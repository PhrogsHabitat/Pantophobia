using UnityEngine;
using UnityEngine.UI;
using Phobia.Audio;

namespace Phobia.Audio.Vis
{
	/// <summary>
	/// Specialized audio visualizer for the Offset menu with custom styling and behavior
	/// </summary>
	public class OffsetMenuVis : PhobiaVis
	{
		#region Offset Menu Specific Configuration

		[System.Serializable]
		public class OffsetMenuConfig : VisConfig
		{
			[Header("Offset Menu Styling")]
			public bool useGlowEffect = true;
			public float glowIntensity = 2f;
			public bool usePulseEffect = true;
			public float pulseSpeed = 2f;
			public Color accentColor = Color.cyan;
			public bool showCenterDot = true;
			public float centerDotSize = 10f;

			[Header("Offset Menu Behavior")]
			public bool reactToMenuInteraction = true;
			public float interactionBoost = 1.5f;
			public bool showFrequencyLabels = false;

			/// <summary>
			/// Creates a default configuration optimized for the Offset menu
			/// </summary>
			public static new OffsetMenuConfig CreateDefault()
			{
				var config = new OffsetMenuConfig();

				// Base settings optimized for offset menu
				config.bandCount = 16;
				config.radius = 180f;
				config.maxHeight = 120f;
				config.barWidth = 12f;
				config.rotationSpeed = 15f;
				config.frequencyMultiplier = 120f;
				config.smoothingSpeed = 8f;

				// Offset menu specific settings
				config.useGlowEffect = true;
				config.glowIntensity = 2f;
				config.usePulseEffect = true;
				config.pulseSpeed = 2f;
				config.accentColor = Color.cyan;
				config.showCenterDot = true;
				config.centerDotSize = 10f;

				// Create a more vibrant gradient for the offset menu
				config.colorGradient = new Gradient();
				var colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(new Color(0.1f, 0.1f, 0.8f), 0f),    // Deep blue
                    new GradientColorKey(new Color(0.2f, 0.8f, 1f), 0.25f),   // Cyan
                    new GradientColorKey(new Color(0.8f, 1f, 0.2f), 0.5f),    // Yellow-green
                    new GradientColorKey(new Color(1f, 0.6f, 0.1f), 0.75f),   // Orange
                    new GradientColorKey(new Color(1f, 0.2f, 0.4f), 1f)       // Pink-red
                };
				var alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(0.8f, 0f),
					new GradientAlphaKey(1f, 0.5f),
					new GradientAlphaKey(0.9f, 1f)
				};
				config.colorGradient.SetKeys(colorKeys, alphaKeys);

				return config;
			}
		}

		#endregion

		#region Private Fields

		private OffsetMenuConfig _offsetConfig;
		private Image _centerDot;
		private float _pulseTimer;
		private float _interactionMultiplier = 1f;

		#endregion

		#region Properties

		/// <summary>
		/// Get the offset menu specific configuration
		/// </summary>
		public OffsetMenuConfig OffsetConfig
		{
			get => _offsetConfig ?? (_offsetConfig = OffsetMenuConfig.CreateDefault());
			set => _offsetConfig = value;
		}

		#endregion

		#region Initialization

		protected override void Awake()
		{
			// Initialize with offset menu config if none exists
			if (Config == null)
			{
				Config = OffsetMenuConfig.CreateDefault();
			}

			_offsetConfig = Config as OffsetMenuConfig ?? OffsetMenuConfig.CreateDefault();

			base.Awake();
		}

		/// <summary>
		/// Initialize with offset menu specific configuration
		/// </summary>
		public void Initialize(OffsetMenuConfig config)
		{
			_offsetConfig = config;
			base.Initialize(config);
		}

		#endregion

		#region Override Methods

		protected override void InitializeVisualization()
		{
			base.InitializeVisualization();

			// Create center dot if enabled
			if (_offsetConfig.showCenterDot)
			{
				CreateCenterDot();
			}
		}

		protected override void UpdateVisualization()
		{
			// Update pulse timer
			_pulseTimer += Time.deltaTime * _offsetConfig.pulseSpeed;

			// Apply interaction multiplier decay
			_interactionMultiplier = Mathf.Lerp(_interactionMultiplier, 1f, Time.deltaTime * 2f);

			base.UpdateVisualization();

			// Update center dot
			UpdateCenterDot();
		}

		protected override void ConfigureBarAppearance(Image barImage, RectTransform barRect, int index)
		{
			base.ConfigureBarAppearance(barImage, barRect, index);

			// Apply glow effect if enabled
			if (_offsetConfig.useGlowEffect)
			{
				// This would typically involve a custom shader or material
				// For now, we'll simulate with increased alpha and size variation
				var color = barImage.color;
				color.a *= _offsetConfig.glowIntensity;
				barImage.color = color;
			}
		}

		protected override GameObject CreateVisualizationBar(int index)
		{
			GameObject bar = base.CreateVisualizationBar(index);

			// Add offset menu specific styling
			if (bar != null && _offsetConfig.useGlowEffect)
			{
				// Add outline or glow component here if available
				// This is where you'd add custom visual effects
			}

			return bar;
		}

		#endregion

		#region Offset Menu Specific Methods

		/// <summary>
		/// Create the center dot visualization element
		/// </summary>
		private void CreateCenterDot()
		{
			if (_container == null)
			{
				return;
			}

			GameObject centerDotGO = new GameObject("CenterDot", typeof(Image));
			centerDotGO.transform.SetParent(_container);

			_centerDot = centerDotGO.GetComponent<Image>();
			RectTransform dotRect = centerDotGO.GetComponent<RectTransform>();

			// Configure center dot
			_centerDot.color = _offsetConfig.accentColor;
			dotRect.sizeDelta = new Vector2(_offsetConfig.centerDotSize, _offsetConfig.centerDotSize);
			dotRect.anchoredPosition = Vector2.zero;
			dotRect.anchorMin = new Vector2(0.5f, 0.5f);
			dotRect.anchorMax = new Vector2(0.5f, 0.5f);
			dotRect.pivot = new Vector2(0.5f, 0.5f);
		}

		/// <summary>
		/// Update the center dot with pulse effects
		/// </summary>
		private void UpdateCenterDot()
		{
			if (_centerDot == null || !_offsetConfig.usePulseEffect)
			{
				return;
			}

			// Calculate pulse scale

			float pulseScale = 1f + Mathf.Sin(_pulseTimer) * 0.3f;
			pulseScale *= _interactionMultiplier;

			// Apply scale
			_centerDot.transform.localScale = Vector3.one * pulseScale;

			// Update color intensity based on audio
			if (_bandBuffer != null && _bandBuffer.Length > 0)
			{
				float averageIntensity = 0f;
				for (int i = 0; i < _bandBuffer.Length; i++)
				{
					averageIntensity += _bandBuffer[i];
				}
				averageIntensity /= _bandBuffer.Length;

				Color dotColor = _offsetConfig.accentColor;
				dotColor.a = Mathf.Clamp01(0.5f + averageIntensity / _offsetConfig.maxHeight);
				_centerDot.color = dotColor;
			}
		}

		/// <summary>
		/// Trigger an interaction boost effect
		/// </summary>
		public void TriggerInteractionBoost()
		{
			if (_offsetConfig.reactToMenuInteraction)
			{
				_interactionMultiplier = _offsetConfig.interactionBoost;
			}
		}

		/// <summary>
		/// Short name for TriggerInteractionBoost (overrides base)
		/// </summary>
		public override void pop()
		{
			TriggerInteractionBoost();
		}

		/// <summary>
		/// Short name for TriggerInteractionBoost
		/// </summary>
		public void boost()
		{
			TriggerInteractionBoost();
		}

		/// <summary>
		/// Set the accent color for offset menu specific elements
		/// </summary>
		public void SetAccentColor(Color color)
		{
			_offsetConfig.accentColor = color;
			if (_centerDot != null)
			{
				_centerDot.color = color;
			}
		}

		#endregion

		#region Static Factory Methods

		/// <summary>
		/// Create an OffsetMenuVis instance with default offset menu configuration
		/// </summary>
		public static OffsetMenuVis CreateForOffsetMenu(Transform parent, PhobiaSound targetSound = null)
		{
			GameObject visObject = new GameObject("OffsetMenuVis");
			if (parent != null)
			{
				visObject.transform.SetParent(parent);
			}

			// Add RectTransform for UI positioning
			RectTransform rectTransform = visObject.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = Vector2.zero;

			OffsetMenuVis visualizer = visObject.AddComponent<OffsetMenuVis>();

			var config = OffsetMenuConfig.CreateDefault();
			if (targetSound != null)
			{
				config.targetSound = targetSound;
			}

			visualizer.Initialize(config);

			return visualizer;
		}

		/// <summary>
		/// Simple create method - auto-setup everything!
		/// </summary>
		public static new OffsetMenuVis Create(PhobiaSound sound = null)
		{
			GameObject visObject = new GameObject("OffsetMenuVis");
			OffsetMenuVis vis = visObject.AddComponent<OffsetMenuVis>();

			if (sound != null)
			{
				vis.SetSound(sound);
			}

			return vis;
		}

		#endregion
	}
}
