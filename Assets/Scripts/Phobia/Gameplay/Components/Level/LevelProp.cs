using System;
using System.Collections;
using Phobia.Graphics;
using UnityEngine;

namespace Phobia.Gameplay.Components.Level
{
    /// <summary>
    /// Enhanced level prop system with runtime configuration and advanced features
    /// Handles sprite-based props with animations, effects, and state management
    /// </summary>
    public class LevelProp : MonoBehaviour
    {
        #region Configuration Data

        /// <summary>
        /// Configuration for LevelProp behavior and appearance
        /// </summary>
        [System.Serializable]
        public class PropConfig
        {
            [Header("Basic Settings")]
            public string propName = "";
            public bool autoInitialize = true;
            public bool enableInteraction = false;

            [Header("Visual Settings")]
            public bool enableAnimations = true;
            public bool enableEffects = true;
            public bool enableFading = true;

            [Header("State Management")]
            public bool enableStateTracking = true;
            public bool autoReviveOnEnable = false;
            public float defaultAlpha = 1f;

            [Header("Performance")]
            public bool enableCaching = true;
            public bool useObjectPooling = false;

            /// <summary>
            /// Create default prop config
            /// </summary>
            public static PropConfig CreateDefault()
            {
                return new PropConfig();
            }

            /// <summary>
            /// Create config for interactive props
            /// </summary>
            public static PropConfig CreateInteractiveConfig()
            {
                return new PropConfig
                {
                    enableInteraction = true,
                    enableAnimations = true,
                    enableEffects = true,
                    enableStateTracking = true
                };
            }

            /// <summary>
            /// Create config for background props
            /// </summary>
            public static PropConfig CreateBackgroundConfig()
            {
                return new PropConfig
                {
                    enableInteraction = false,
                    enableAnimations = false,
                    enableEffects = false,
                    useObjectPooling = true
                };
            }
        }

        #endregion

        #region Public Properties

        [SerializeField] private PropConfig _config;
        public PropConfig Config
        {
            get => _config ?? (_config = PropConfig.CreateDefault());
            set => _config = value;
        }

        public PhobiaSprite sprite { get; private set; }
        public string propName
        {
            get => Config.propName;
            set => Config.propName = value;
        }
        public bool isReady = true;
        public bool isDying = false;

        // Enhanced state tracking
        public bool IsVisible => sprite != null && sprite.IsVisible;
        public bool IsAnimating => sprite != null && sprite.IsAnimationPlaying;
        public string CurrentAnimation => sprite?.CurrentAnimation ?? "";

        #endregion

        #region Events

        public event Action<LevelProp> OnPropReady;
        public event Action<LevelProp> OnPropDestroyed;
        public event Action<LevelProp> OnPropInteracted;
        public event Action<LevelProp, string> OnAnimationComplete;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSprite();
        }

        private void Start()
        {
            if (Config.autoInitialize)
            {
                ApplyConfiguration();
            }
        }

        private void OnEnable()
        {
            if (Config.autoReviveOnEnable && isDying)
            {
                Revive();
            }
        }

        #endregion


        #region Initialization

        /// <summary>
        /// Initialize the sprite component
        /// </summary>
        private void InitializeSprite()
        {
            if (sprite == null)
            {
                sprite = gameObject.AddComponent<PhobiaSprite>();
            }
        }

        /// <summary>
        /// Apply current configuration to the prop
        /// </summary>
        public void ApplyConfiguration()
        {
            if (sprite != null && Config != null)
            {
                // Create sprite config based on prop config
                var spriteConfig = new PhobiaSprite.SpriteConfig
                {
                    enableAnimations = Config.enableAnimations,
                    enableFading = Config.enableEffects,
                    enableCaching = Config.enableCaching,
                    defaultAlpha = Config.defaultAlpha
                };

                sprite.UpdateConfig(spriteConfig);
            }
        }

        #endregion

        #region Sprite Management

        /// <summary>
        /// Load texture with enhanced error handling
        /// </summary>
        public void LoadTexture(string texturePath)
        {
            if (sprite == null)
            {
                Debug.LogError("[LevelProp] Sprite not initialized");
                return;
            }

            sprite.LoadTexture(texturePath);
        }

        /// <summary>
        /// Load Sparrow atlas with validation
        /// </summary>
        public void LoadSparrowAtlas(string atlasPath, string spriteName)
        {
            if (sprite == null)
            {
                Debug.LogError("[LevelProp] Sprite not initialized");
                return;
            }

            sprite.LoadSparrowAtlas(atlasPath, spriteName);
        }

        /// <summary>
        /// Create solid color sprite
        /// </summary>
        public void MakeSolidColor(Vector2 size, Color color)
        {
            if (sprite == null)
            {
                Debug.LogError("[LevelProp] Sprite not initialized");
                return;
            }

            sprite.MakeSolidColor(size, color);
        }

        /// <summary>
        /// Play animation with callback support
        /// </summary>
        public void PlayAnimation(string animName, Action onComplete = null)
        {
            if (sprite == null)
            {
                Debug.LogError("[LevelProp] Sprite not initialized");
                onComplete?.Invoke();
                return;
            }

            if (!Config.enableAnimations)
            {
                Debug.LogWarning("[LevelProp] Animations disabled in config");
                onComplete?.Invoke();
                return;
            }

            sprite.PlayAnimation(animName);

            if (onComplete != null)
            {
                sprite.AddAnimationCompleteCallback(animName, () =>
                {
                    OnAnimationComplete?.Invoke(this, animName);
                    onComplete.Invoke();
                });
            }
        }

        /// <summary>
        /// Set sprite color
        /// </summary>
        public void SetColor(Color color)
        {
            if (sprite != null)
            {
                sprite.SetColor(color);
            }
        }

        /// <summary>
        /// Fade effects with config validation
        /// </summary>
        public void FadeOut(float duration, Action onComplete = null)
        {
            if (!Config.enableEffects)
            {
                SetAlpha(0f);
                onComplete?.Invoke();
                return;
            }

            if (sprite != null)
            {
                sprite.FadeOut(duration);
            }

            if (onComplete != null)
            {
                StartCoroutine(DelayedCallback(duration, onComplete));
            }
        }

        public void FadeIn(float duration, Action onComplete = null)
        {
            if (!Config.enableEffects)
            {
                SetAlpha(1f);
                onComplete?.Invoke();
                return;
            }

            if (sprite != null)
            {
                sprite.FadeIn(duration);
            }

            if (onComplete != null)
            {
                StartCoroutine(DelayedCallback(duration, onComplete));
            }
        }

        /// <summary>
        /// Set alpha directly
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (sprite != null)
            {
                sprite.SetAlpha(alpha);
            }
        }

        /// <summary>
        /// Delayed callback coroutine
        /// </summary>
        private IEnumerator DelayedCallback(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }

        #endregion

        #region Transform Properties

        /// <summary>
        /// Prop position in 2D space
        /// </summary>
        public Vector2 Position
        {
            get => transform.position;
            set => transform.position = new Vector3(value.x, value.y, transform.position.z);
        }

        /// <summary>
        /// Prop scale in 2D space
        /// </summary>
        public Vector2 Scale
        {
            get => transform.localScale;
            set => transform.localScale = new Vector3(value.x, value.y, 1);
        }

        /// <summary>
        /// Prop rotation in degrees
        /// </summary>
        public float Rotation
        {
            get => transform.eulerAngles.z;
            set => transform.rotation = Quaternion.Euler(0, 0, value);
        }

        #endregion

        #region State Management

        /// <summary>
        /// Revive the prop with enhanced state management
        /// </summary>
        public void Revive()
        {
            gameObject.SetActive(true);
            isDying = false;
            isReady = true;

            if (sprite != null)
            {
                sprite.SetAlpha(Config.defaultAlpha);
            }

            OnPropReady?.Invoke(this);
        }

        /// <summary>
        /// Kill the prop with cleanup
        /// </summary>
        public void Kill()
        {
            isDying = true;
            isReady = false;

            OnPropDestroyed?.Invoke(this);

            if (Config.useObjectPooling)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Interact with the prop
        /// </summary>
        public void Interact()
        {
            if (!Config.enableInteraction || !isReady)
            {
                return;
            }

            OnPropInteracted?.Invoke(this);
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create a basic level prop
        /// </summary>
        public static LevelProp Create(string name, Vector2 position, PropConfig config = null)
        {
            GameObject go = new GameObject(name);
            LevelProp prop = go.AddComponent<LevelProp>();

            if (config != null)
            {
                prop.Config = config;
            }

            prop.propName = name;
            prop.Position = position;
            return prop;
        }

        /// <summary>
        /// Create an interactive prop
        /// </summary>
        public static LevelProp CreateInteractive(string name, Vector2 position)
        {
            var config = PropConfig.CreateInteractiveConfig();
            return Create(name, position, config);
        }

        /// <summary>
        /// Create a background prop
        /// </summary>
        public static LevelProp CreateBackground(string name, Vector2 position)
        {
            var config = PropConfig.CreateBackgroundConfig();
            return Create(name, position, config);
        }

        /// <summary>
        /// Create prop with texture
        /// </summary>
        public static LevelProp CreateWithTexture(string name, Vector2 position, string texturePath, PropConfig config = null)
        {
            var prop = Create(name, position, config);
            prop.LoadTexture(texturePath);
            return prop;
        }

        /// <summary>
        /// Create prop with solid color
        /// </summary>
        public static LevelProp CreateSolidColor(string name, Vector2 position, Vector2 size, Color color, PropConfig config = null)
        {
            var prop = Create(name, position, config);
            prop.MakeSolidColor(size, color);
            return prop;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Update configuration at runtime
        /// </summary>
        public void UpdateConfig(PropConfig newConfig)
        {
            Config = newConfig;
            ApplyConfiguration();
        }

        /// <summary>
        /// Get screen position
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
        /// Clone this prop
        /// </summary>
        public LevelProp Clone()
        {
            var clone = Create(propName + "_Clone", Position, Config);
            clone.Scale = Scale;
            clone.Rotation = Rotation;
            return clone;
        }

        #endregion
    }
}
