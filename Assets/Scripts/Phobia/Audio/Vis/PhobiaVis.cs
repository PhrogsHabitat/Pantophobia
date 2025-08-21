using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Phobia.Audio.Vis
{
	/// <summary>
	/// Comprehensive audio visualizer system for the Phobia framework.
	/// Provides a single, runtime-configurable audio visualizer with integrated management and performance optimization.
	/// This class consolidates all audio visualization functionality into one comprehensive solution.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class PhobiaVis : MonoBehaviour
	{
		#region Configuration Data

		/// <summary>
		/// Configuration data for the visualizer that can be set at runtime
		/// </summary>
		[System.Serializable]
		public class VisConfig
		{
			[Header("Audio Settings")]
			public PhobiaSound targetSound;
			[Range(64, 8192)] public int sampleCount = 512;

			[Header("Visual Settings")]
			public float maxHeight = 150f;
			public float radius = 200f;
			public float rotationSpeed = 10f;

			[Header("Frequency Analysis")]
			[Range(4, 32)] public int bandCount = 8;
			public float frequencyMultiplier = 2000f;  // VERY high sensitivity - make all bars dance!
			public float smoothingSpeed = 10f;

			[Header("Appearance")]
			public Gradient colorGradient;
			public float barWidth = 15f;
			public bool enableRotation = true;
			public bool enableSmoothing = true;

			[Header("Performance")]
			public bool useObjectPooling = true;
			public int maxPoolSize = 64;
			public bool enableCaching = true;
			public int maxCacheSize = 50;
			public bool adaptiveFrameRate = true;
			public float maxUpdateInterval = 1f / 60f;

			[Header("Quality")]
			public bool enableAntiAliasing = false;
			public bool enableLOD = false;

			/// <summary>
			/// Creates a default configuration
			/// </summary>
			public static VisConfig CreateDefault()
			{
				var config = new VisConfig();

				// Set default gradient
				config.colorGradient = new Gradient();
				var colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(Color.blue, 0f),
					new GradientColorKey(Color.cyan, 0.3f),
					new GradientColorKey(Color.yellow, 0.7f),
					new GradientColorKey(Color.red, 1f)
				};
				var alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(1f, 0f),
					new GradientAlphaKey(1f, 1f)
				};
				config.colorGradient.SetKeys(colorKeys, alphaKeys);

				return config;
			}

			/// <summary>
			/// Create config optimized for performance
			/// </summary>
			public static VisConfig CreatePerformanceConfig()
			{
				var config = CreateDefault();
				config.bandCount = 6;
				config.sampleCount = 256;
				config.enableSmoothing = false;
				config.useObjectPooling = true;
				config.maxPoolSize = 32;
				config.adaptiveFrameRate = true;
				return config;
			}

			/// <summary>
			/// Create config optimized for quality
			/// </summary>
			public static VisConfig CreateHighQualityConfig()
			{
				var config = CreateDefault();
				config.bandCount = 16;
				config.sampleCount = 1024;
				config.enableSmoothing = true;
				config.smoothingSpeed = 15f;
				config.maxPoolSize = 128;
				config.enableAntiAliasing = true;
				return config;
			}

			/// <summary>
			/// Create compact config for small displays
			/// </summary>
			public static VisConfig CreateCompactConfig()
			{
				var config = CreateDefault();
				config.radius = 100f;
				config.bandCount = 6;
				config.maxHeight = 80f;
				config.barWidth = 10f;
				return config;
			}
		}

		#endregion

		#region Public Properties

		[SerializeField] private VisConfig _config;
		public VisConfig Config
		{
			get => _config ?? (_config = VisConfig.CreateDefault());
			set => _config = value;
		}

		/// <summary>
		/// Whether the visualizer is currently active and processing audio
		/// </summary>
		public bool IsActive { get; private set; }

		/// <summary>
		/// Current audio source being visualized
		/// </summary>
		public AudioSource CurrentAudioSource { get; private set; }

		#endregion

		#region Protected Fields (Accessible to Derived Classes)

		// Audio processing
		protected float[] _samples;
		protected float[] _frequencyBands;
		protected float[] _bandBuffer;
		protected float[] _bufferDecrease;

		// UI Components
		protected List<RectTransform> _visualizerBars = new List<RectTransform>();
		protected List<Image> _barImages = new List<Image>();
		protected RectTransform _container;
		protected Canvas _parentCanvas;

		// Object pooling
		protected Queue<GameObject> _barPool = new Queue<GameObject>();
		protected List<GameObject> _activeObjects = new List<GameObject>();

		#endregion

		#region Private Fields

		// Caching
		private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();
		private static readonly Dictionary<int, Material> _materialCache = new Dictionary<int, Material>();

		// Performance tracking
		private float _lastUpdateTime;
		private const float MIN_UPDATE_INTERVAL = 1f / 60f; // 60 FPS max

		#endregion

		#region Simple Creation & Auto-Setup

		/// <summary>
		/// Simple static create method - auto-setup everything!
		/// Usage: var vis = PhobiaVis.Create(sound);
		/// </summary>
		public static PhobiaVis Create(PhobiaSound sound = null)
		{
			// Create GameObject with auto-setup
			GameObject visObject = new GameObject("PhobiaVis");
			PhobiaVis vis = visObject.AddComponent<PhobiaVis>();

			if (sound != null)
			{
				vis.Config.targetSound = sound;
			}

			return vis;
		}

		/// <summary>
		/// Set the sound for this visualizer
		/// </summary>
		public void SetSound(PhobiaSound sound)
		{
			SetTargetSound(sound);
		}

		/// <summary>
		/// Set the number of bars in the visualizer
		/// </summary>
		public void SetBarCount(int count)
		{
			count = Mathf.Clamp(count, 4, 32);
			if (Config.bandCount != count)
			{
				Config.bandCount = count;

				// Adjust frequency multiplier based on bar count - make ALL bars excited!
				if (count > 16)
				{
					Config.frequencyMultiplier = 3000f; // SUPER high sensitivity for 24-32 bars
				}
				else if (count > 8)
				{
					Config.frequencyMultiplier = 2500f; // Very high sensitivity for 12-16 bars
				}
				else
				{
					Config.frequencyMultiplier = 2000f; // High sensitivity for 4-8 bars
				}

				if (IsActive)
				{
					// Recreate visualization with new bar count
					InitializeVisualization();
				}

				UnityEngine.Debug.Log($"[PhobiaVis] Set to {count} bars with multiplier {Config.frequencyMultiplier}");
			}
		}

		/// <summary>
		/// Automatic setup - handles everything!
		/// </summary>
		private void AutoSetup()
		{
			// Auto-attach to smart parent
			if (transform.parent == null)
			{
				Transform smartParent = GetSmartParent();
				if (smartParent != null)
				{
					transform.SetParent(smartParent, false);
				}
			}

			// Auto-setup RectTransform
			RectTransform rectTransform = GetComponent<RectTransform>();
			if (rectTransform == null)
			{
				rectTransform = gameObject.AddComponent<RectTransform>();
			}

			// Set sensible defaults
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.sizeDelta = new Vector2(Config.radius * 2, Config.radius * 2);
		}

		#endregion

		#region Unity Lifecycle

		protected virtual void Awake()
		{
			// Ensure we have a config
			if (_config == null)
			{
				_config = VisConfig.CreateDefault();
			}

			// Auto-setup everything!

			AutoSetup();

			// Add to tracking list
			_activeVisualizers.Add(this);

			// Find or create parent canvas
			_parentCanvas = GetComponentInParent<Canvas>();
			if (_parentCanvas == null)
			{
				_parentCanvas = FindFirstObjectByType<Canvas>();
			}
		}

		protected virtual void Start()
		{
			// Initialize with current config
			if (_config.targetSound != null)
			{
				Initialize(_config);
			}
		}

		protected virtual void Update()
		{
			if (!IsActive || CurrentAudioSource == null || !CurrentAudioSource.isPlaying)
			{
				return;
			}

			// Update performance metrics

			UpdatePerformanceMetrics();

			// Performance throttling
			float updateInterval = Config.adaptiveFrameRate ? Config.maxUpdateInterval : MIN_UPDATE_INTERVAL;
			if (Time.time - _lastUpdateTime < updateInterval)
			{
				return;
			}

			_lastUpdateTime = Time.time;

			// Optimize configuration if needed
			OptimizeConfigForPerformance();

			ProcessAudioData();
			UpdateVisualization();
		}

		protected virtual void OnDestroy()
		{
			// Remove from active visualizers list
			_activeVisualizers.Remove(this);

			Cleanup();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Initialize the visualizer with the given configuration
		/// </summary>
		public virtual void Initialize(VisConfig config)
		{
			if (config == null)
			{
				Debug.LogError("[PhobiaVis] Cannot initialize with null config");
				return;
			}

			_config = config;

			// Clean up existing visualization
			Cleanup();

			// Set up audio source
			if (config.targetSound != null)
			{
				SetTargetSound(config.targetSound);
			}

			// Initialize visualization
			InitializeVisualization();

			IsActive = true;
		}

		/// <summary>
		/// Set the target audio source for visualization
		/// </summary>
		public virtual void SetTargetSound(PhobiaSound sound)
		{
			if (sound == null)
			{
				Debug.LogWarning("[PhobiaVis] Setting null target sound");
				CurrentAudioSource = null;
				IsActive = false;
				return;
			}

			_config.targetSound = sound;
			CurrentAudioSource = sound.GetComponent<AudioSource>();

			if (CurrentAudioSource == null)
			{
				Debug.LogError("[PhobiaVis] PhobiaSound does not have an AudioSource component");
				IsActive = false;
				return;
			}

			IsActive = true;
		}

		/// <summary>
		/// Update the visualizer configuration at runtime
		/// </summary>
		public virtual void UpdateConfig(VisConfig newConfig)
		{
			if (newConfig == null)
			{
				return;
			}

			bool needsRebuild =
				_config.bandCount != newConfig.bandCount ||
				_config.sampleCount != newConfig.sampleCount ||
				_config.radius != newConfig.radius ||
				_config.barWidth != newConfig.barWidth;

			_config = newConfig;

			if (needsRebuild)
			{
				Initialize(_config);
			}
		}

		/// <summary>
		/// Enable or disable the visualizer
		/// </summary>
		public virtual void SetActive(bool active)
		{
			IsActive = active;
			if (_container != null)
			{
				_container.gameObject.SetActive(active);
			}
		}

		#endregion


		#region Protected Virtual Methods

		/// <summary>
		/// Initialize the visualization components. Override for custom visualization types.
		/// </summary>
		protected virtual void InitializeVisualization()
		{
			// Ensure sample count is valid
			_config.sampleCount = Mathf.ClosestPowerOfTwo(_config.sampleCount);
			_config.sampleCount = Mathf.Clamp(_config.sampleCount, 64, 8192);

			// Create container
			CreateContainer();

			// Create visualization elements
			CreateVisualizationElements();

			// Initialize audio processing arrays
			InitializeAudioArrays();
		}

		/// <summary>
		/// Process audio data from the current audio source
		/// </summary>
		protected virtual void ProcessAudioData()
		{
			if (_samples == null || CurrentAudioSource == null)
			{
				return;
			}

			// Get spectrum data

			CurrentAudioSource.GetSpectrumData(_samples, 0, FFTWindow.BlackmanHarris);

			// Process frequency bands
			CalculateFrequencyBands();
			ProcessBandBuffers();
		}

		/// <summary>
		/// Update the visual representation. Override for custom visualization behavior.
		/// </summary>
		protected virtual void UpdateVisualization()
		{
			if (_config.enableRotation && _container != null)
			{
				_container.Rotate(0, 0, _config.rotationSpeed * Time.deltaTime);
			}

			UpdateVisualizationElements();
		}

		/// <summary>
		/// Create the main container for visualization elements
		/// </summary>
		protected virtual void CreateContainer()
		{
			if (_container != null)
			{
				return;
			}

			GameObject containerGO = new GameObject("VisualizerContainer");
			containerGO.transform.SetParent(transform);
			_container = containerGO.AddComponent<RectTransform>();

			// Configure container
			_container.anchorMin = new Vector2(0.5f, 0.5f);
			_container.anchorMax = new Vector2(0.5f, 0.5f);
			_container.pivot = new Vector2(0.5f, 0.5f);
			_container.anchoredPosition = Vector2.zero;
			_container.sizeDelta = new Vector2(_config.radius * 2, _config.radius * 2);
		}

		/// <summary>
		/// Create the individual visualization elements (bars, particles, etc.)
		/// Override for different visualization types.
		/// </summary>
		protected virtual void CreateVisualizationElements()
		{
			// Clear existing elements
			ClearVisualizationElements();

			// Create bars in circular formation
			for (int i = 0; i < _config.bandCount; i++)
			{
				GameObject bar = CreateVisualizationBar(i);
				if (bar != null)
				{
					_activeObjects.Add(bar);
				}
			}
		}

		/// <summary>
		/// Create a single visualization bar. Override for custom bar appearance.
		/// </summary>
		protected virtual GameObject CreateVisualizationBar(int index)
		{
			GameObject bar = GetPooledObject();
			if (bar == null)
			{
				bar = new GameObject($"VisBar_{index}", typeof(Image));
			}

			bar.transform.SetParent(_container);
			RectTransform barRect = bar.GetComponent<RectTransform>();
			Image barImage = bar.GetComponent<Image>();

			// Configure bar appearance
			ConfigureBarAppearance(barImage, barRect, index);

			// Position bar in circle
			PositionBarInCircle(barRect, index);

			_visualizerBars.Add(barRect);
			_barImages.Add(barImage);

			return bar;
		}

		/// <summary>
		/// Configure the appearance of a visualization bar
		/// </summary>
		protected virtual void ConfigureBarAppearance(Image barImage, RectTransform barRect, int index)
		{
			barImage.color = Color.white;
			barRect.sizeDelta = new Vector2(_config.barWidth, 10f);
			barRect.pivot = new Vector2(0.5f, 0f); // Grow outward from center
		}

		/// <summary>
		/// Position a bar in the circular formation
		/// </summary>
		protected virtual void PositionBarInCircle(RectTransform barRect, int index)
		{
			float angle = index * Mathf.PI * 2 / _config.bandCount;
			Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _config.radius;
			barRect.anchoredPosition = pos;

			// Rotate bar to point OUTWARD from center (add 90 degrees to make it face outward)
			// Without +90, bars point toward center. WITH +90, they point outward like spokes!
			barRect.localEulerAngles = new Vector3(0, 0, angle * Mathf.Rad2Deg + -90f);

			// Set anchor to bottom center so bar grows outward from the circle
			barRect.anchorMin = new Vector2(0.5f, 0f);
			barRect.anchorMax = new Vector2(0.5f, 0f);
			barRect.pivot = new Vector2(0.5f, 0f);
		}

		#endregion

		#region Private Implementation Methods

		/// <summary>
		/// Initialize audio processing arrays
		/// </summary>
		private void InitializeAudioArrays()
		{
			_samples = new float[_config.sampleCount];
			_frequencyBands = new float[_config.bandCount];
			_bandBuffer = new float[_config.bandCount];
			_bufferDecrease = new float[_config.bandCount];
		}

		/// <summary>
		/// Calculate frequency bands with balanced distribution - make ALL bars equally excited!
		/// </summary>
		private void CalculateFrequencyBands()
		{
			// Clear all bands first
			for (int i = 0; i < _config.bandCount; i++)
			{
				_frequencyBands[i] = 0;
			}

			// Use a more balanced approach - give each bar a fair share of excitement!
			int usableSamples = _config.sampleCount / 2; // Only use first half (Nyquist frequency)

			for (int band = 0; band < _config.bandCount; band++)
			{
				// More balanced frequency mapping - not too logarithmic
				float bandRatio = (float)band / _config.bandCount;

				// Gentler curve - not as aggressive as pure logarithmic
				float startRatio = Mathf.Pow(bandRatio, 0.8f);
				float endRatio = Mathf.Pow((float)(band + 1) / _config.bandCount, 0.8f);

				int startSample = (int)(startRatio * usableSamples);
				int endSample = (int)(endRatio * usableSamples);

				// Ensure minimum samples per band so everyone gets some data!
				int minSamplesPerBand = Mathf.Max(1, usableSamples / (_config.bandCount * 4));
				if (endSample - startSample < minSamplesPerBand)
				{
					endSample = startSample + minSamplesPerBand;
				}

				// Clamp to valid range
				startSample = Mathf.Clamp(startSample, 0, usableSamples - 1);
				endSample = Mathf.Clamp(endSample, startSample + 1, usableSamples);

				// Calculate average for this frequency range
				float sum = 0;
				int sampleCount = endSample - startSample;

				for (int i = startSample; i < endSample && i < _samples.Length; i++)
				{
					sum += _samples[i];
				}

				if (sampleCount > 0)
				{
					float average = sum / sampleCount;

					// BALANCED sensitivity - make everyone equally excited!
					float sensitivity = _config.frequencyMultiplier;

					if (band < _config.bandCount * 0.25f)
					{
						// Bass frequencies - good sensitivity but not overwhelming
						sensitivity *= 2.5f;
					}
					else if (band < _config.bandCount * 0.75f)
					{
						// Mid frequencies - strong sensitivity
						sensitivity *= 3.0f;
					}
					else
					{
						// High frequencies - EXTRA sensitivity to make them dance!
						sensitivity *= 5.0f;
					}

					_frequencyBands[band] = average * sensitivity;
				}
			}
		}

		/// <summary>
		/// Process band buffers for smoothing
		/// </summary>
		private void ProcessBandBuffers()
		{
			if (!_config.enableSmoothing)
			{
				// Direct copy without smoothing
				Array.Copy(_frequencyBands, _bandBuffer, _config.bandCount);
				return;
			}

			for (int i = 0; i < _config.bandCount; i++)
			{
				// Use proper lerp-based smoothing for both rise and fall
				float targetValue = _frequencyBands[i];
				float currentValue = _bandBuffer[i];

				// Different speeds for rise vs fall to make it more natural
				float lerpSpeed = targetValue > currentValue ?
					_config.smoothingSpeed * 2f : // Faster rise
					_config.smoothingSpeed * 0.5f; // Slower fall

				_bandBuffer[i] = Mathf.Lerp(currentValue, targetValue, Time.deltaTime * lerpSpeed);
			}
		}

		/// <summary>
		/// Update all visualization elements
		/// </summary>
		private void UpdateVisualizationElements()
		{
			for (int i = 0; i < Mathf.Min(_visualizerBars.Count, _config.bandCount); i++)
			{
				UpdateVisualizationBar(i);
			}
		}

		/// <summary>
		/// Update a single visualization bar
		/// </summary>
		private void UpdateVisualizationBar(int index)
		{
			if (index >= _visualizerBars.Count || index >= _barImages.Count)
			{
				return;
			}

			RectTransform bar = _visualizerBars[index];
			Image image = _barImages[index];

			if (bar == null || image == null)
			{
				return;
			}

			// Scale the bar height

			float targetHeight = Mathf.Clamp(_bandBuffer[index], 0.1f, _config.maxHeight);
			Vector2 size = bar.sizeDelta;

			if (_config.enableSmoothing)
			{
				size.y = Mathf.Lerp(size.y, targetHeight, Time.deltaTime * _config.smoothingSpeed);
			}
			else
			{
				size.y = targetHeight;
			}

			bar.sizeDelta = size;

			// Update color
			float intensity = Mathf.Clamp01(_bandBuffer[index] / (_config.maxHeight * 0.5f));
			image.color = _config.colorGradient.Evaluate(intensity);
		}

		/// <summary>
		/// Clear all visualization elements
		/// </summary>
		private void ClearVisualizationElements()
		{
			// Return active objects to pool
			foreach (var obj in _activeObjects)
			{
				if (obj != null)
				{
					ReturnToPool(obj);
				}
			}

			_activeObjects.Clear();
			_visualizerBars.Clear();
			_barImages.Clear();
		}

		/// <summary>
		/// Clean up all resources
		/// </summary>
		private void Cleanup()
		{
			IsActive = false;
			ClearVisualizationElements();

			if (_container != null)
			{
				DestroyImmediate(_container.gameObject);
				_container = null;
			}
		}

		#endregion

		#region Object Pooling

		/// <summary>
		/// Get a pooled object or create a new one
		/// </summary>
		private GameObject GetPooledObject()
		{
			if (!_config.useObjectPooling || _barPool.Count == 0)
			{
				return null;
			}

			GameObject obj = _barPool.Dequeue();
			obj.SetActive(true);
			return obj;
		}

		/// <summary>
		/// Return an object to the pool
		/// </summary>
		private void ReturnToPool(GameObject obj)
		{
			if (!_config.useObjectPooling || obj == null)
			{
				if (obj != null)
				{
					DestroyImmediate(obj);
				}

				return;
			}

			if (_barPool.Count < _config.maxPoolSize)
			{
				obj.SetActive(false);
				obj.transform.SetParent(null);
				_barPool.Enqueue(obj);
			}
			else
			{
				DestroyImmediate(obj);
			}
		}

		#endregion

		#region Static Utility Methods

		/// <summary>
		/// Create a PhobiaVis instance with the specified configuration
		/// </summary>
		public static PhobiaVis Create(Transform parent, VisConfig config = null)
		{
			GameObject visObject = new GameObject("PhobiaVis");
			if (parent != null)
			{
				visObject.transform.SetParent(parent, false);
			}

			// Add RectTransform for UI positioning
			RectTransform rectTransform = visObject.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = Vector2.zero;

			PhobiaVis visualizer = visObject.AddComponent<PhobiaVis>();

			// Try to find a Canvas in the parent chain
			Canvas parentCanvas = null;
			Transform search = parent;
			while (search != null)
			{
				parentCanvas = search.GetComponent<Canvas>();
				if (parentCanvas != null)
				{
					break;
				}


				search = search.parent;
			}
			if (parentCanvas != null)
			{
				visualizer._parentCanvas = parentCanvas;
			}

			if (config != null)
			{
				visualizer.Initialize(config);
			}

			return visualizer;
		}

		/// <summary>
		/// Create a PhobiaVis instance attached to a Canvas
		/// </summary>
		public static PhobiaVis CreateOnCanvas(Canvas canvas, VisConfig config = null)
		{
			if (canvas == null)
			{
				Debug.LogError("[PhobiaVis] Cannot create visualizer on null canvas");
				return null;
			}

			return Create(canvas.transform, config);
		}

		/// <summary>
		/// Find or create a canvas for UI elements
		/// </summary>
		public static Canvas FindOrCreateCanvas()
		{
			Canvas canvas = FindFirstObjectByType<Canvas>();
			if (canvas != null)
			{
				return canvas;
			}

			// Create new canvas

			GameObject canvasGO = new GameObject("PhobiaVisCanvas");
			canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvasGO.AddComponent<CanvasScaler>();
			canvasGO.AddComponent<GraphicRaycaster>();

			return canvas;
		}

		/// <summary>
		/// Auto-detect PhobiaCamera in scene
		/// </summary>
		public static Phobia.Camera.PhobiaCamera FindPhobiaCamera()
		{
			return FindFirstObjectByType<Phobia.Camera.PhobiaCamera>();
		}

		/// <summary>
		/// Get smart parent for visualizer (PhobiaCamera UI or canvas)
		/// </summary>
		public static Transform GetSmartParent()
		{
			// Try to find PhobiaCamera first
			var phobiaCamera = FindPhobiaCamera();
			if (phobiaCamera != null)
			{
				// Look for UI canvas attached to camera
				Canvas cameraCanvas = phobiaCamera.GetComponentInChildren<Canvas>();
				if (cameraCanvas != null)
				{
					return cameraCanvas.transform;
				}
			}

			// Fall back to finding or creating a canvas
			return FindOrCreateCanvas().transform;
		}

		#endregion

		#region Simple Methods

		/// <summary>
		/// Update config (short name)
		/// </summary>
		public void set(VisConfig config)
		{
			UpdateConfig(config);
		}

		/// <summary>
		/// Trigger boost/pop effect
		/// </summary>
		public virtual void pop()
		{
			// Base implementation - can be overridden
			if (Config.enableSmoothing)
			{
				Config.smoothingSpeed *= 1.5f;
			}
		}

		#endregion

		#region Simple Manager System

		// Simple tracking for cleanup
		private static List<PhobiaVis> _activeVisualizers = new List<PhobiaVis>();

		/// <summary>
		/// Get all active visualizers (for cleanup)
		/// </summary>
		public static List<PhobiaVis> GetActiveVisualizers()
		{
			_activeVisualizers.RemoveAll(v => v == null);
			return new List<PhobiaVis>(_activeVisualizers);
		}



		#endregion

		#region Integrated Performance Optimization

		// Performance tracking (additional fields for integrated optimization)
		private float _averageFrameTime;
		private int _currentFrameRate;
		private List<float> _frameTimeHistory = new List<float>();

		// Additional caching
		private static Dictionary<string, Gradient> _gradientCache = new Dictionary<string, Gradient>();

		/// <summary>
		/// Update performance metrics
		/// </summary>
		private void UpdatePerformanceMetrics()
		{
			if (!Config.adaptiveFrameRate)
			{
				return;
			}

			float currentFrameTime = Time.unscaledDeltaTime;
			_frameTimeHistory.Add(currentFrameTime);

			if (_frameTimeHistory.Count > 60) // Keep last 60 frames
			{
				_frameTimeHistory.RemoveAt(0);
			}

			// Calculate average
			float total = 0f;
			foreach (float time in _frameTimeHistory)
			{
				total += time;
			}
			_averageFrameTime = total / _frameTimeHistory.Count;
			_currentFrameRate = Mathf.RoundToInt(1f / _averageFrameTime);
		}

		/// <summary>
		/// Check if performance allows for the given operation
		/// </summary>
		private bool CanPerformOperation(float estimatedCost)
		{
			if (!Config.adaptiveFrameRate)
			{
				return true;
			}

			float availableTime = Config.maxUpdateInterval - _averageFrameTime;
			return availableTime > estimatedCost;
		}

		/// <summary>
		/// Optimize configuration based on current performance
		/// </summary>
		private void OptimizeConfigForPerformance()
		{
			if (!Config.adaptiveFrameRate)
			{
				return;
			}

			int targetFrameRate = 60;
			if (_currentFrameRate < targetFrameRate * 0.6f)
			{
				// Severe performance issues - reduce quality significantly
				Config.bandCount = Mathf.Max(4, Config.bandCount / 2);
				Config.sampleCount = Mathf.Max(64, Config.sampleCount / 2);
				Config.enableSmoothing = false;
				Config.useObjectPooling = true;
			}
			else if (_currentFrameRate < targetFrameRate * 0.8f)
			{
				// Moderate performance issues - reduce quality moderately
				Config.bandCount = Mathf.Max(6, (int)(Config.bandCount * 0.75f));
				Config.smoothingSpeed *= 0.5f;
			}
		}

		/// <summary>
		/// Get cached sprite or load and cache it
		/// </summary>
		public static Sprite GetCachedSprite(string path)
		{
			if (_spriteCache.TryGetValue(path, out Sprite cachedSprite))
			{
				return cachedSprite;
			}

			Sprite sprite = Resources.Load<Sprite>(path);
			if (sprite != null && _spriteCache.Count < 50)
			{
				_spriteCache[path] = sprite;
			}

			return sprite;
		}

		/// <summary>
		/// Get cached material or create and cache it
		/// </summary>
		public static Material GetCachedMaterial(int materialId, System.Func<Material> materialFactory)
		{
			if (_materialCache.TryGetValue(materialId, out Material cachedMaterial))
			{
				return cachedMaterial;
			}

			Material material = materialFactory();
			if (material != null && _materialCache.Count < 50)
			{
				_materialCache[materialId] = material;
			}

			return material;
		}

		/// <summary>
		/// Clear all caches
		/// </summary>
		public static void ClearCaches()
		{
			_spriteCache.Clear();
			_materialCache.Clear();
			_gradientCache.Clear();
		}

		#endregion
	}
}

