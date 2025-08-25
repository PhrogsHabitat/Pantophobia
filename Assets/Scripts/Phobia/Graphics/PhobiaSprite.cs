using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using System.Xml;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
		public bool IsAnimationPlaying => _animationCoroutine != null;
		public bool IsVisible => _renderer != null && _renderer.enabled;
		public Color BaseColor { get; private set; } = Color.white;

		#endregion

		#region Private Fields

		private SpriteRenderer _renderer;
		private Coroutine _fadeCoroutine;
		private Coroutine _colorTweenCoroutine;
		private Coroutine _animationCoroutine;

		// Sparrow animation support
		private Dictionary<string, List<Sprite>> _sparrowAnimationFrames = new Dictionary<string, List<Sprite>>();
		private Dictionary<string, float> _animationFrameRates = new Dictionary<string, float>();
		private Dictionary<string, bool> _animationLoopSettings = new Dictionary<string, bool>();
		private Dictionary<string, string> _animationAliases = new Dictionary<string, string>(); // alias -> original
		private Dictionary<string, float> _aliasFrameRates = new Dictionary<string, float>(); // alias -> frameRate
		private bool _sparrowSetupDone = false;

		[Header("UI Scaling")]
		[SerializeField] private bool _scaleToFit = false;
		[SerializeField] private Vector2 _targetSize = Vector2.one;

		public bool ScaleToFit
		{
			get => _scaleToFit;
			set
			{
				_scaleToFit = value;
				if (value && _renderer != null && _renderer.sprite != null)
				{
					ApplyScaleToFit();
				}
			}
		}

		public Vector2 TargetSize
		{
			get => _targetSize;
			set
			{
				_targetSize = value;
				if (_scaleToFit && _renderer != null && _renderer.sprite != null)
				{
					ApplyScaleToFit();
				}
			}
		}

		#endregion

		#region Unity Lifecycle

		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
			ApplyConfiguration();
		}

		private void OnDestroy()
		{
			// Stop all coroutines when object is destroyed
			if (_animationCoroutine != null)
			{
				StopCoroutine(_animationCoroutine);
			}
			if (_fadeCoroutine != null)
			{
				StopCoroutine(_fadeCoroutine);
			}
			if (_colorTweenCoroutine != null)
			{
				StopCoroutine(_colorTweenCoroutine);
			}
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

			sprite.LoadSparrowXML(atlasPath, spriteName);
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
		/// Load sprite from Sparrow atlas and auto-setup animations from XML
		/// </summary>
		public void LoadSparrowXML(string resourcePath, string spriteName)
		{
			if (string.IsNullOrEmpty(resourcePath) || string.IsNullOrEmpty(spriteName))
			{
				Debug.LogError("[PhobiaSprite] Resource path or sprite name is null/empty");
				return;
			}

			// Load the texture
			Texture2D texture = Resources.Load<Texture2D>(resourcePath);
			if (texture == null)
			{
				Debug.LogError($"[PhobiaSprite] Texture not found: {resourcePath}");
				return;
			}

			// Load the XML - assuming it has the same name as the texture but with .xml extension
			TextAsset xmlAsset = Resources.Load<TextAsset>(resourcePath);
			if (xmlAsset == null)
			{
				Debug.LogWarning($"[PhobiaSprite] Sparrow XML not found: {resourcePath}");
				return;
			}

			ParseSparrowXML(xmlAsset.text, texture, spriteName);
		}

		/// <summary>
		/// Parse Sparrow XML and auto-create AnimationClips for each animation
		/// </summary>
		private void ParseSparrowXML(string xmlText, Texture2D texture, string defaultSpriteName)
		{
			if (string.IsNullOrEmpty(xmlText) || texture == null)
			{
				Debug.LogError("[PhobiaSprite] Invalid XML or texture for Sparrow parsing");
				return;
			}

			try
			{
				// Parse XML
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(xmlText);

				XmlNode atlasNode = xmlDoc.SelectSingleNode("//TextureAtlas");
				if (atlasNode == null)
				{
					Debug.LogError("[PhobiaSprite] TextureAtlas node not found in XML.");
					return;
				}

				// Get the original texture size from XML if available
				int originalWidth = 4096; // Default assumption
				int originalHeight = 4096; // Default assumption
				if (atlasNode.Attributes["width"] != null && atlasNode.Attributes["height"] != null)
				{
					originalWidth = int.Parse(atlasNode.Attributes["width"].Value);
					originalHeight = int.Parse(atlasNode.Attributes["height"].Value);
				}

				// Calculate scale factors
				float scaleX = (float)texture.width / originalWidth;
				float scaleY = (float)texture.height / originalHeight;

				// Parse all sub-textures
				var subTextures = new Dictionary<string, Sprite>();
				foreach (XmlNode node in atlasNode.SelectNodes("SubTexture"))
				{
					string name = node.Attributes["name"]?.Value;
					if (string.IsNullOrEmpty(name)) { continue; }

					int x = int.Parse(node.Attributes["x"]?.Value ?? "0");
					int y = int.Parse(node.Attributes["y"]?.Value ?? "0");
					int width = int.Parse(node.Attributes["width"]?.Value ?? "0");
					int height = int.Parse(node.Attributes["height"]?.Value ?? "0");

					// Scale coordinates to fit the loaded texture
					int scaledX = Mathf.RoundToInt(x * scaleX);
					int scaledY = Mathf.RoundToInt(y * scaleY);
					int scaledWidth = Mathf.RoundToInt(width * scaleX);
					int scaledHeight = Mathf.RoundToInt(height * scaleY);

					// Check if the scaled coordinates are within texture bounds
					if (scaledX + scaledWidth > texture.width || scaledY + scaledHeight > texture.height)
					{
						Debug.LogWarning($"[PhobiaSprite] Scaled coordinates for {name} are outside texture bounds. Skipping.");
						continue;
					}

					// Create sprite (adjust y coordinate for Unity's coordinate system)
					Rect rect = new Rect(scaledX, texture.height - scaledY - scaledHeight, scaledWidth, scaledHeight);
					Vector2 pivot = new Vector2(0.5f, 0.5f);

					Sprite sprite = Sprite.Create(texture, rect, pivot, 100);
					sprite.name = name;
					subTextures[name] = sprite;
				}

				// Set default sprite
				if (subTextures.ContainsKey(defaultSpriteName))
				{
					_renderer.sprite = subTextures[defaultSpriteName];
				}
				else if (subTextures.Count > 0)
				{
					// Use first sprite if default not found
					using (var enumerator = subTextures.Values.GetEnumerator())
					{
						enumerator.MoveNext();
						_renderer.sprite = enumerator.Current;
					}
				}

				// Group frames by animation name (remove numbers from end)
				var animGroups = new Dictionary<string, List<Sprite>>();
				foreach (var kvp in subTextures)
				{
					// Extract base name without frame numbers
					string baseName = System.Text.RegularExpressions.Regex.Replace(kvp.Key, @"\d+$", "");

					if (!animGroups.ContainsKey(baseName))
					{
						animGroups[baseName] = new List<Sprite>();
					}
					animGroups[baseName].Add(kvp.Value);
				}

				// Sort each animation's frames by name
				foreach (var anim in animGroups)
				{
					anim.Value.Sort((a, b) => string.Compare(a.name, b.name));
					_sparrowAnimationFrames[anim.Key] = anim.Value;
					_animationFrameRates[anim.Key] = 24f; // Default frame rate
				}

				_sparrowSetupDone = true;

				// Log available animations for debugging
				Debug.Log($"[PhobiaSprite] Loaded {_sparrowAnimationFrames.Count} animations: {string.Join(", ", _sparrowAnimationFrames.Keys)}");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[PhobiaSprite] Error parsing Sparrow XML: {ex.Message}");
				_sparrowSetupDone = false;
			}
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
		/// Play animation by name
		/// </summary>
		public void PlayAnimation(string animationName)
		{
			if (!Config.enableAnimations)
			{
				Debug.LogWarning("[PhobiaSprite] Animations disabled");
				return;
			}

			// Check if animations are available
			if (!_sparrowSetupDone || _sparrowAnimationFrames == null || _sparrowAnimationFrames.Count == 0)
			{
				Debug.LogWarning($"[PhobiaSprite] No animations loaded. Cannot play '{animationName}'");
				return;
			}

			// Check if the requested animation exists
			if (_sparrowAnimationFrames.ContainsKey(animationName))
			{
				// Stop any running animation
				if (_animationCoroutine != null)
				{
					StopCoroutine(_animationCoroutine);
				}

				// Start new animation
				_animationCoroutine = StartCoroutine(PlayAnimationRoutine(animationName));
				CurrentAnimation = animationName;
				return;
			}

			Debug.LogWarning($"[PhobiaSprite] Animation '{animationName}' not found! Available animations: {string.Join(", ", _sparrowAnimationFrames.Keys)}");

			// Try to play the first available animation as a fallback
			if (_sparrowAnimationFrames.Count > 0)
			{
				using (var enumerator = _sparrowAnimationFrames.Keys.GetEnumerator())
				{
					enumerator.MoveNext();
					string fallbackAnimation = enumerator.Current;
					Debug.LogWarning($"[PhobiaSprite] Playing fallback animation: {fallbackAnimation}");

					if (_animationCoroutine != null)
					{
						StopCoroutine(_animationCoroutine);
					}

					_animationCoroutine = StartCoroutine(PlayAnimationRoutine(fallbackAnimation));
					CurrentAnimation = fallbackAnimation;
				}
			}
		}

		/// <summary>
		/// Animation coroutine that cycles through frames
		/// </summary>
		private IEnumerator PlayAnimationRoutine(string animationName)
		{
			var frames = _sparrowAnimationFrames[animationName];
			int currentFrame = 0;

			// Get frame rate for this animation (default to 24 FPS if not specified)
			float frameRate = _animationFrameRates.ContainsKey(animationName) ?
				_animationFrameRates[animationName] * _config.animationSpeed : 24f * _config.animationSpeed;

			float frameDelay = 1f / frameRate;

			// Use per-animation loop setting if available
			bool shouldLoop = GetAnimationLoop(animationName);

			Debug.Log($"[PhobiaSprite] Playing animation '{animationName}' at {frameRate} FPS with {frames.Count} frames, loop={shouldLoop}");

			while (frames.Count > 0)
			{
				_renderer.sprite = frames[currentFrame];
				currentFrame = (currentFrame + 1) % frames.Count;

				// If not looping and we're at the last frame, break
				if (!shouldLoop && currentFrame == 0)
				{
					break;
				}

				yield return new WaitForSeconds(frameDelay);
			}

			_animationCoroutine = null;
			Debug.Log($"[PhobiaSprite] Animation '{animationName}' finished playing");
		}

		/// <summary>
		/// Set custom frame rate for an animation
		/// </summary>
		public void SetAnimationFrameRate(string animationName, float frameRate)
		{
			if (_sparrowAnimationFrames.ContainsKey(animationName))
			{
				_animationFrameRates[animationName] = frameRate;
			}
			else
			{
				Debug.LogWarning($"[PhobiaSprite] Cannot set frame rate for unknown animation: {animationName}");
			}
		}

		/// <summary>
		/// Set whether a specific animation should loop.
		/// </summary>
		public void SetAnimationLoop(string animationName, bool shouldLoop)
		{
			_animationLoopSettings[animationName] = shouldLoop;
		}

		/// <summary>
		/// Get whether a specific animation should loop. Returns config default if not set.
		/// </summary>
		public bool GetAnimationLoop(string animationName)
		{
			if (_animationLoopSettings.TryGetValue(animationName, out bool loop))
			{
				return loop;
			}

			return Config.loopAnimations;
		}

		/// <summary>
		/// Stop current animation
		/// </summary>
		public void StopAnimation()
		{
			if (_animationCoroutine != null)
			{
				StopCoroutine(_animationCoroutine);
				_animationCoroutine = null;
			}
		}

		/// <summary>
		/// Register an animation alias with a custom frame rate.
		/// </summary>
		public void AddAnim(string originalAnim, string alias, float frameRate)
		{
			if (!_sparrowAnimationFrames.ContainsKey(originalAnim))
			{
				Debug.LogWarning($"[PhobiaSprite] Cannot alias unknown animation: {originalAnim}");
				return;
			}
			_animationAliases[alias] = originalAnim;
			_aliasFrameRates[alias] = frameRate;
		}

		/// <summary>
		/// Play an animation by alias or original name, with explicit looping and force options.
		/// </summary>
		public void PlayAnim(string aliasOrName, bool shouldLoop = true, bool force = false)
		{
			string animName = aliasOrName;
			if (_animationAliases.TryGetValue(aliasOrName, out var mapped))
			{
				animName = mapped;
			}

			if (!_sparrowAnimationFrames.ContainsKey(animName))
			{
				Debug.LogWarning($"[PhobiaSprite] Animation '{aliasOrName}' (resolved as '{animName}') not found.");
				return;
			}

			// Set loop for this play only
			_animationLoopSettings[animName] = shouldLoop;

			// Set frame rate if alias has one
			if (_aliasFrameRates.TryGetValue(aliasOrName, out float frameRate))
			{
				_animationFrameRates[animName] = frameRate;
			}

			if (force || CurrentAnimation != animName || !IsAnimationPlaying)
			{
				PlayAnimation(animName);
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

		/// <summary>
		/// Scale the sprite to fit the target size while maintaining aspect ratio
		/// </summary>
		public void ApplyScaleToFit()
		{
			if (_renderer == null || _renderer.sprite == null)
			{
				Debug.LogWarning("[PhobiaSprite] Cannot scale to fit - no sprite or renderer");
				return;
			}

			// Get the sprite's native size
			Vector2 spriteSize = _renderer.sprite.bounds.size;

			// Calculate the scale needed to fit the target size
			float scaleX = _targetSize.x / spriteSize.x;
			float scaleY = _targetSize.y / spriteSize.y;

			// Use the smaller scale to maintain aspect ratio
			float uniformScale = Mathf.Min(scaleX, scaleY);

			// Apply the scale
			transform.localScale = new Vector3(uniformScale, uniformScale, 1f);

			Debug.Log($"[PhobiaSprite] Scaled to fit: spriteSize={spriteSize}, targetSize={_targetSize}, scale={uniformScale}");
		}

		/// <summary>
		/// Scale the sprite to fill the target size (may crop)
		/// </summary>
		public void ApplyScaleToFill()
		{
			if (_renderer == null || _renderer.sprite == null)
			{
				Debug.LogWarning("[PhobiaSprite] Cannot scale to fill - no sprite or renderer");
				return;
			}

			// Get the sprite's native size
			Vector2 spriteSize = _renderer.sprite.bounds.size;

			// Calculate the scale needed to fill the target size
			float scaleX = _targetSize.x / spriteSize.x;
			float scaleY = _targetSize.y / spriteSize.y;

			// Use the larger scale to fill (may crop)
			float uniformScale = Mathf.Max(scaleX, scaleY);

			// Apply the scale
			transform.localScale = new Vector3(uniformScale, uniformScale, 1f);

			Debug.Log($"[PhobiaSprite] Scaled to fill: spriteSize={spriteSize}, targetSize={_targetSize}, scale={uniformScale}");
		}

		/// <summary>
		/// Stretch the sprite to exactly fit the target size (may distort aspect ratio)
		/// </summary>
		public void ApplyStretchToFit()
		{
			if (_renderer == null || _renderer.sprite != null)
			{
				Debug.LogWarning("[PhobiaSprite] Cannot stretch to fit - no sprite or renderer");
				return;
			}

			// Get the sprite's native size
			Vector2 spriteSize = _renderer.sprite.bounds.size;

			// Calculate the scale needed to stretch to the target size
			float scaleX = _targetSize.x / spriteSize.x;
			float scaleY = _targetSize.y / spriteSize.y;

			// Apply non-uniform scale (will distort if aspect ratios differ)
			transform.localScale = new Vector3(scaleX, scaleY, 1f);

			Debug.Log($"[PhobiaSprite] Stretched to fit: spriteSize={spriteSize}, targetSize={_targetSize}, scale=({scaleX}, {scaleY})");
		}

        #endregion
	}
}
