using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

namespace Phobia.Graphics
{
    /// <summary>
    /// Enhanced sprite management system with runtime configuration and caching
    /// Handles texture loading, animations, and visual effects with swag efficiency
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PhobiaSprite : MonoBehaviour
    {
        #region Configuration Data

        /// <summary>
        /// Configuration for PhobiaSprite instances - runtime configurable like a boss
        /// </summary>
        [System.Serializable]
        public class SpriteConfig
        {
            [Header("Basic Settings")]
            public bool enableCaching = true;
            public bool autoDestroy = false;
            public float defaultAlpha = 1f;
            public Color tintColor = Color.white;

            [Header("Animation Settings")]
            public bool enableAnimations = true;
            public float animationSpeed = 1f;
            public bool loopAnimations = true;

            [Header("Effects")]
            public bool enableFading = true;
            public float fadeSpeed = 1f;
            public bool enableColorTweening = true;

            [Header("Performance")]
            public int maxCacheSize = 100;
            public bool useObjectPooling = false;

            /// <summary>
            /// Creates default config that doesn't suck
            /// </summary>
            public static SpriteConfig CreateDefault()
            {
                return new SpriteConfig();
            }

            /// <summary>
            /// Creates config optimized for UI elements
            /// </summary>
            public static SpriteConfig CreateUIConfig()
            {
                return new SpriteConfig
                {
                    enableAnimations = false,
                    enableFading = true,
                    fadeSpeed = 2f,
                    useObjectPooling = true
                };
            }

            /// <summary>
            /// Creates config for animated sprites that need to be smooth
            /// </summary>
            public static SpriteConfig CreateAnimatedConfig()
            {
                return new SpriteConfig
                {
                    enableAnimations = true,
                    animationSpeed = 1f,
                    loopAnimations = true,
                    enableCaching = true
                };
            }
        }

        #endregion

        #region Static Cache System

        // Cached textures and atlases - Unity handles memory but we handle the smart shit
        private static Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();
        private static Dictionary<string, SpriteAtlas> _cachedAtlases = new Dictionary<string, SpriteAtlas>();
        private static Dictionary<Color, Sprite> _solidColorSprites = new Dictionary<Color, Sprite>();
        private static Dictionary<string, bool> _animationStates = new Dictionary<string, bool>();

        // Performance tracking
        private static int _cacheHits = 0;
        private static int _cacheMisses = 0;

        /// <summary>
        /// Clear all cached sprites - useful for memory management
        /// </summary>
        public static void ClearCache()
        {
            _cachedSprites.Clear();
            _cachedAtlases.Clear();
            // Keep solid colors cuz they're tiny and useful
            Debug.Log($"[PhobiaSprite] Cache cleared. Stats - Hits: {_cacheHits}, Misses: {_cacheMisses}");
        }

        /// <summary>
        /// Get cache performance stats because we're nerds
        /// </summary>
        public static (int hits, int misses, float hitRate) GetCacheStats()
        {
            float hitRate = _cacheHits + _cacheMisses > 0 ? (float)_cacheHits / (_cacheHits + _cacheMisses) : 0f;
            return (_cacheHits, _cacheMisses, hitRate);
        }

        #endregion


        #region Public Properties

        [SerializeField] private SpriteConfig _config;
        public SpriteConfig Config
        {
            get => _config ?? (_config = SpriteConfig.CreateDefault());
            set => _config = value;
        }

        public string CurrentAnimation { get; private set; }
        public bool IsAnimationDynamic => _animator != null && _animator.runtimeAnimatorController != null;
        public bool IsVisible => _renderer != null && _renderer.enabled;
        public Color BaseColor { get; private set; } = Color.white;

        #endregion

        #region Private Fields

        private SpriteRenderer _renderer;
        private Animator _animator;
        private Coroutine _fadeCoroutine;
        private Coroutine _colorTweenCoroutine;

		#endregion

		#region Unity Lifecycle

		private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            // Lazy initialize animator if needed and config allows
            if (_animator == null && Config.enableAnimations)
            {
                _animator = gameObject.AddComponent<Animator>();
            }

            // Apply initial config
            ApplyConfiguration();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create a PhobiaSprite with texture loading
        /// </summary>
        public static PhobiaSprite Create(Vector3 position, string spritePath, SpriteConfig config = null)
        {
            GameObject go = new GameObject($"PhobiaSprite_{System.IO.Path.GetFileName(spritePath)}");
            go.transform.position = position;
            PhobiaSprite sprite = go.AddComponent<PhobiaSprite>();

            if (config != null)
            {
                sprite.Config = config;
            }

            sprite.LoadTexture(spritePath);
            return sprite;
        }

        /// <summary>
        /// Create a PhobiaSprite with Sparrow atlas - fancy shit
        /// </summary>
        public static PhobiaSprite CreateSparrow(Vector3 position, string atlasPath, string spriteName, SpriteConfig config = null)
        {
            GameObject go = new GameObject($"PhobiaSprite_{spriteName}");
            go.transform.position = position;
            PhobiaSprite sprite = go.AddComponent<PhobiaSprite>();

            if (config != null)
            {
                sprite.Config = config;
            }

            sprite.LoadSparrowAtlas(atlasPath, spriteName);
            return sprite;
        }

        /// <summary>
        /// Create a solid color sprite - simple but effective
        /// </summary>
        public static PhobiaSprite CreateSolidColor(Vector3 position, Vector2 size, Color color, SpriteConfig config = null)
        {
            GameObject go = new GameObject($"SolidSprite_{color}");
            go.transform.position = position;
            PhobiaSprite sprite = go.AddComponent<PhobiaSprite>();

            if (config != null)
            {
                sprite.Config = config;
            }

            sprite.MakeSolidColor(size, color);
            return sprite;
        }

        #endregion


        #region Configuration Management

        /// <summary>
        /// Apply current configuration to the sprite
        /// </summary>
        public void ApplyConfiguration()
        {
            if (_config == null)
            {
                return;
            }

            if (_renderer != null)
            {
                Color color = _config.tintColor;
                color.a = _config.defaultAlpha;
                _renderer.color = color;
                BaseColor = color;
            }

            if (_animator != null && _config.enableAnimations)
            {
                _animator.speed = _config.animationSpeed;
            }
        }

        /// <summary>
        /// Update configuration at runtime - because flexibility is king
        /// </summary>
        public void UpdateConfig(SpriteConfig newConfig)
        {
            _config = newConfig;
            ApplyConfiguration();
        }

        #endregion

        #region Texture Loading

        /// <summary>
        /// Load texture from Resources with smart caching
        /// </summary>
        public void LoadTexture(string spritePath)
        {
            if (string.IsNullOrEmpty(spritePath))
            {
                Debug.LogError("[PhobiaSprite] Sprite path is null or empty");
                return;
            }

            if (_config.enableCaching && _cachedSprites.TryGetValue(spritePath, out Sprite cachedSprite))
            {
                _renderer.sprite = cachedSprite;
                _cacheHits++;
                return;
            }

            Sprite loadedSprite = Resources.Load<Sprite>(spritePath);
            if (loadedSprite != null)
            {
                // Cache the sprite if caching is enabled
                if (_config.enableCaching && _cachedSprites.Count < _config.maxCacheSize)
                {
                    _cachedSprites[spritePath] = loadedSprite;
                }

                _renderer.sprite = loadedSprite;
                _cacheMisses++;
            }
            else
            {
                Debug.LogError($"[PhobiaSprite] Sprite not found: {spritePath}");
            }
        }

        /// <summary>
        /// Load sprite from Sparrow atlas - for the fancy animated stuff
        /// </summary>
        public void LoadSparrowAtlas(string atlasPath, string spriteName)
        {
            if (string.IsNullOrEmpty(atlasPath) || string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError("[PhobiaSprite] Atlas path or sprite name is null/empty");
                return;
            }

            if (!_config.enableCaching || !_cachedAtlases.TryGetValue(atlasPath, out SpriteAtlas atlas))
            {
                atlas = Resources.Load<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    Debug.LogError($"[PhobiaSprite] Atlas not found: {atlasPath}");
                    return;
                }

                if (_config.enableCaching)
                {
                    _cachedAtlases[atlasPath] = atlas;
                }
            }

            Sprite sprite = atlas.GetSprite(spriteName);
            if (sprite == null)
            {
                Debug.LogError($"[PhobiaSprite] Sprite '{spriteName}' not found in atlas '{atlasPath}'");
                return;
            }

            _renderer.sprite = sprite;
        }

        /// <summary>
        /// Create a solid color sprite - simple but effective as hell
        /// </summary>
        public void MakeSolidColor(Vector2 size, Color color)
        {
            if (!_solidColorSprites.TryGetValue(color, out Sprite solidSprite))
            {
                // Create tiny 2x2 texture (efficient for scaling)
                Texture2D tex = new Texture2D(2, 2);
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }

                tex.Apply();
                solidSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
                _solidColorSprites[color] = solidSprite;
            }

            _renderer.sprite = solidSprite;
            transform.localScale = new Vector3(size.x / 2, size.y / 2, 1);
            BaseColor = color;
        }

        #endregion


        #region Animation System

        /// <summary>
        /// Play animation if animator is available and config allows
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (!_config.enableAnimations || _animator == null)
            {
                Debug.LogWarning("[PhobiaSprite] Animations disabled or no animator available");
                return;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[PhobiaSprite] No animator controller assigned!");
                return;
            }

            if (!_animationStates.TryGetValue(animationName, out bool _))
            {
                // Check if animation exists - assume it does for now
                _animationStates[animationName] = true;
            }

            _animator.Play(animationName);
            CurrentAnimation = animationName;
        }

        /// <summary>
        /// Stop current animation
        /// </summary>
        public void StopAnimation()
        {
            if (_animator != null)
            {
                _animator.enabled = false;
            }
        }

        /// <summary>
        /// Set animation speed
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (_animator != null)
            {
                _animator.speed = speed;
            }
        }

		#endregion

		#region Visual Effects

		/// <summary>
		/// Fade out sprite
		/// </summary>
		public void FadeOut(float duration)
		{
			StartFade(0, duration);
		}

		/// <summary>
		/// Fade in sprite
		/// </summary>
		public void FadeIn(float duration)
		{
			StartFade(1, duration);
		}

		/// <summary>
		/// Fade to specific alpha value
		/// </summary>
		public void FadeTo(float targetAlpha, float duration)
		{
			StartFade(targetAlpha, duration);
		}

		/// <summary>
		/// Start fade operation with proper coroutine management
		/// </summary>
		private void StartFade(float targetAlpha, float duration)
        {
            if (!_config.enableFading)
            {
                SetAlpha(targetAlpha);
                return;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
        }

        /// <summary>
        /// Fade coroutine that actually does the work
        /// </summary>
        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            float startAlpha = _renderer.color.a;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                SetAlpha(newAlpha);
                yield return null;
            }

            SetAlpha(targetAlpha);
            _fadeCoroutine = null;
        }


        #endregion

        #region Color and Visual Management

        /// <summary>
        /// Set sprite alpha value
        /// </summary>
        public void SetAlpha(float alpha)
        {
            Color c = _renderer.color;
            c.a = Mathf.Clamp01(alpha);
            _renderer.color = c;
        }

        /// <summary>
        /// Set sprite color
        /// </summary>
        public void SetColor(Color color)
        {
            _renderer.color = color;
        }

        /// <summary>
        /// Reset to base color
        /// </summary>
        public void ResetColor()
        {
            _renderer.color = BaseColor;
        }

        /// <summary>
        /// Tween to target color over time
        /// </summary>
        public void TweenToColor(Color targetColor, float duration)
        {
            if (!_config.enableColorTweening)
            {
                SetColor(targetColor);
                return;
            }

            if (_colorTweenCoroutine != null)
            {
                StopCoroutine(_colorTweenCoroutine);
            }

            _colorTweenCoroutine = StartCoroutine(ColorTweenRoutine(targetColor, duration));
        }

        /// <summary>
        /// Color tween coroutine
        /// </summary>
        private IEnumerator ColorTweenRoutine(Color targetColor, float duration)
        {
            Color startColor = _renderer.color;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Color newColor = Color.Lerp(startColor, targetColor, elapsed / duration);
                _renderer.color = newColor;
                yield return null;
            }

            _renderer.color = targetColor;
            _colorTweenCoroutine = null;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get screen position of sprite
        /// </summary>
        public Vector2 GetScreenPosition(UnityEngine.Camera camera = null)
        {
            if (camera == null)
            {
                camera = UnityEngine.Camera.main;
            }

            return camera.WorldToScreenPoint(transform.position);
        }

        /// <summary>
        /// Clone this sprite with same configuration
        /// </summary>
        public PhobiaSprite Clone()
        {
            GameObject clone = Instantiate(gameObject);
            clone.name = gameObject.name + "_Clone";
            PhobiaSprite clonedSprite = clone.GetComponent<PhobiaSprite>();
            clonedSprite.Config = _config; // Copy config
            return clonedSprite;
        }

        /// <summary>
        /// Check if animation is currently playing
        /// </summary>
        public bool IsAnimationPlaying => _animator != null &&
                                     _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1;

        /// <summary>
        /// Add callback for when animation completes
        /// </summary>
        public void AddAnimationCompleteCallback(string animName, System.Action callback)
        {
            if (!_config.enableAnimations)
            {
                callback?.Invoke();
                return;
            }

            StartCoroutine(WaitForAnimationComplete(animName, callback));
        }

        /// <summary>
        /// Coroutine to wait for animation completion
        /// </summary>
        private IEnumerator WaitForAnimationComplete(string animName, System.Action callback)
        {
            // Wait for the specified animation to start
            while (CurrentAnimation != animName || !IsAnimationPlaying)
            {
                yield return null;
            }

            // Wait for it to finish
            while (IsAnimationPlaying && CurrentAnimation == animName)
            {
                yield return null;
            }

            callback?.Invoke();
        }

        #endregion
    }
}
