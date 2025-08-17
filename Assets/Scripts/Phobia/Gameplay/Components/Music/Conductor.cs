using System;
using System.Collections.Generic;

using Phobia.Audio;

using UnityEngine;

namespace Phobia.Gameplay.Components.Music
{
    /// <summary>
    /// Enhanced music conductor with runtime configuration and precise timing
    /// Handles beat detection, time signatures, and musical events with swag precision
    /// </summary>
    public class Conductor : MonoBehaviour
    {
        #region Configuration Data

        /// <summary>
        /// Configuration for Conductor timing and behavior
        /// </summary>
        [System.Serializable]
        public class ConductorConfig
        {
            [Header("Basic Timing")]
            public float defaultBpm = 120f;
            public int timeSignatureNumerator = 4;
            public int timeSignatureDenominator = 4;

            [Header("Offsets")]
            public float instrumentalOffset = 0f;
            public float formatOffset = 0f;
            public float inputOffset = 0f;
            public float audioVisualOffset = 0f;

            [Header("Precision")]
            public bool enableHighPrecision = true;
            public float precisionThreshold = 1f; // ms
            public bool enableEventSmoothing = false;

            [Header("Performance")]
            public bool enableEventCaching = true;
            public int maxCachedEvents = 1000;

            /// <summary>
            /// Create default conductor config
            /// </summary>
            public static ConductorConfig CreateDefault()
            {
                return new ConductorConfig();
            }

            /// <summary>
            /// Create high-precision config for rhythm games
            /// </summary>
            public static ConductorConfig CreateHighPrecisionConfig()
            {
                return new ConductorConfig
                {
                    enableHighPrecision = true,
                    precisionThreshold = 0.5f,
                    enableEventSmoothing = true,
                    enableEventCaching = true
                };
            }

            /// <summary>
            /// Create performance-optimized config
            /// </summary>
            public static ConductorConfig CreatePerformanceConfig()
            {
                return new ConductorConfig
                {
                    enableHighPrecision = false,
                    precisionThreshold = 5f,
                    enableEventSmoothing = false,
                    enableEventCaching = false
                };
            }
        }

        #endregion

        #region Singleton and Events

        public static Conductor Instance { get; private set; }

        // Enhanced event callbacks with timing data
        public event Action OnMeasureHit;
        public event Action OnBeatHit;
        public event Action OnStepHit;
        public event Action<float> OnBpmChanged;

        #endregion

        #region Public Properties

        [SerializeField] private ConductorConfig _config;
        public ConductorConfig Config
        {
            get => _config ?? (_config = ConductorConfig.CreateDefault());
            set => _config = value;
        }

        // Core timing properties
        public PhobiaSound activeSong;
        public float songPosition; // Current position in milliseconds
        public float bpm = 120;
        public float crochet; // ms per beat
        public float stepCrochet; // ms per step (16th note)

        // Time signature (default 4/4)
        public int timeSignatureNumerator
        {
            get => Config.timeSignatureNumerator;
            set => Config.timeSignatureNumerator = value;
        }
        public int timeSignatureDenominator
        {
            get => Config.timeSignatureDenominator;
            set => Config.timeSignatureDenominator = value;
        }

        // Position tracking
        public int currentMeasure { get; private set; }
        public int currentBeat { get; private set; }
        public int currentStep { get; private set; }

        // Enhanced offset management
        public float instrumentalOffset
        {
            get => Config.instrumentalOffset;
            set => Config.instrumentalOffset = value;
        }
        public float formatOffset
        {
            get => Config.formatOffset;
            set => Config.formatOffset = value;
        }
        public float inputOffset
        {
            get => Config.inputOffset;
            set => Config.inputOffset = value;
        }
        public float audioVisualOffset
        {
            get => Config.audioVisualOffset;
            set => Config.audioVisualOffset = value;
        }
        public float combinedOffset => instrumentalOffset + formatOffset + inputOffset + audioVisualOffset;

        #endregion

        #region Time Management

        // Time change management
        public List<TimeChange> timeChanges = new List<TimeChange>();
        public TimeChange currentTimeChange { get; private set; }

        // Previous positions for event detection
        private int _lastStep = -1;
        private int _lastBeat = -1;
        private int _lastMeasure = -1;

        // Performance tracking
        private float _lastUpdateTime;
        private Queue<float> _timingHistory = new Queue<float>();

        /// <summary>
        /// Time change data structure with enhanced functionality
        /// </summary>
        [Serializable]
        public struct TimeChange
        {
            public float timeMs;     // Timestamp in ms (matches JSON field "timeMs")
            public float bpm;
            public int numerator;
            public int denominator;
            [NonSerialized] public float beatTime; // Cumulative beats (calculated at runtime)

            /// <summary>
            /// Create a time change at the specified time
            /// </summary>
            public static TimeChange Create(float timeMs, float bpm, int numerator = 4, int denominator = 4)
            {
                return new TimeChange
                {
                    timeMs = timeMs,
                    bpm = bpm,
                    numerator = numerator,
                    denominator = denominator,
                    beatTime = 0
                };
            }
        }

        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Initialize with a default time change
                currentTimeChange = new TimeChange
                {
                    timeMs = 0,
                    bpm = bpm,
                    numerator = timeSignatureNumerator,
                    denominator = timeSignatureDenominator,
                    beatTime = 0
                };
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            RecalculateTimings();
        }

        #region Public Methods

        /// <summary>
        /// Map a song with enhanced validation and configuration support
        /// </summary>
        public void MapSong(PhobiaSound sound, float initialBpm, List<TimeChange> changes = null)
        {
            if (sound == null)
            {
                Debug.LogError("[Conductor] Cannot map null sound");
                return;
            }

            activeSong = sound;
            bpm = initialBpm;
            timeChanges = changes ?? new List<TimeChange>();

            // Initialize with a default time change if empty
            if (timeChanges.Count == 0)
            {
                timeChanges.Add(new TimeChange
                {
                    timeMs = 0,
                    bpm = bpm,
                    numerator = timeSignatureNumerator,
                    denominator = timeSignatureDenominator,
                    beatTime = 0
                });
            }

            MapTimeChanges();
            RecalculateTimings();
            OnBpmChanged?.Invoke(bpm);
        }

        /// <summary>
        /// Map song with configuration
        /// </summary>
        public void MapSong(PhobiaSound sound, float initialBpm, ConductorConfig config, List<TimeChange> changes = null)
        {
            Config = config;
            MapSong(sound, initialBpm, changes);
        }

        /// <summary>
        /// Update configuration at runtime
        /// </summary>
        public void UpdateConfig(ConductorConfig newConfig)
        {
            Config = newConfig;
            RecalculateTimings();
        }

        /// <summary>
        /// Add a time change at runtime
        /// </summary>
        public void AddTimeChange(float timeMs, float newBpm, int numerator = 4, int denominator = 4)
        {
            var timeChange = TimeChange.Create(timeMs, newBpm, numerator, denominator);
            timeChanges.Add(timeChange);
            timeChanges.Sort((a, b) => a.timeMs.CompareTo(b.timeMs));
            MapTimeChanges();
        }

        /// <summary>
        /// Get timing accuracy for performance monitoring
        /// </summary>
        public float GetTimingAccuracy()
        {
            if (_timingHistory.Count == 0)
            {
                return 1f;
            }

            float sum = 0f;
            foreach (float timing in _timingHistory)
            {
                sum += timing;
            }
            return sum / _timingHistory.Count;
        }

        #endregion

        public void MapTimeChanges()
        {
            if (timeChanges.Count == 0)
            {
                return;
            }

            // Sort by time
            timeChanges.Sort((a, b) => a.timeMs.CompareTo(b.timeMs));

            // Calculate cumulative beat times
            TimeChange first = timeChanges[0];
            first.beatTime = 0f;
            timeChanges[0] = first;

            for (int i = 1; i < timeChanges.Count; i++)
            {
                TimeChange current = timeChanges[i];
                TimeChange previous = timeChanges[i - 1];

                float crochet = (60f / previous.bpm) * 1000f;
                float elapsedBeats = (current.timeMs - previous.timeMs) / crochet;
                current.beatTime = previous.beatTime + elapsedBeats;

                timeChanges[i] = current;
            }
        }

        private void Update()
        {
            if (activeSong == null || !activeSong.IsPlaying())
            {
                return;
            }

            // Update song position from PhobiaSound
            songPosition = activeSong.songPosition + combinedOffset;
            UpdateTimeChange();
            RecalculateTimings();
            UpdateMusicalPosition();
        }

        private void UpdateTimeChange()
        {
            if (timeChanges.Count == 0)
            {
                return;
            }

            // Start with the first time change
            currentTimeChange = timeChanges[0];

            // Find the latest applicable time change
            for (int i = 0; i < timeChanges.Count; i++)
            {
                if (songPosition >= timeChanges[i].timeMs)
                {
                    currentTimeChange = timeChanges[i];
                }
                else
                {
                    break;
                }
            }
        }

        private void UpdateMusicalPosition()
        {
            // Get time since current time change
            float timeSinceChange = songPosition - currentTimeChange.timeMs;

            // Calculate steps since change (16th notes)
            float stepsSinceChange = timeSinceChange / stepCrochet;
            float totalSteps = (currentTimeChange.beatTime * 4f) + stepsSinceChange;

            // Update positions
            currentStep = Mathf.FloorToInt(totalSteps);
            currentBeat = Mathf.FloorToInt(totalSteps / 4f);
            currentMeasure = Mathf.FloorToInt(currentBeat / timeSignatureNumerator);

            // Dispatch events
            if (currentStep != _lastStep)
            {
                OnStepHit?.Invoke();
                _lastStep = currentStep;

                if (currentStep % 4 == 0)
                {
                    OnBeatHit?.Invoke();
                    _lastBeat = currentBeat;

                    if (currentBeat % timeSignatureNumerator == 0)
                    {
                        OnMeasureHit?.Invoke();
                        _lastMeasure = currentMeasure;
                    }
                }
            }
        }

        public void RecalculateTimings()
        {
            bpm = currentTimeChange.bpm;
            crochet = (60f / bpm) * 1000f; // ms per beat
            stepCrochet = crochet / 4f;     // ms per step (16th note)
        }

        // ======== Utility Methods ========
        public float GetStep(float timeMs)
        {
            for (int i = 0; i < timeChanges.Count - 1; i++)
            {
                var change = timeChanges[i];
                var next = timeChanges[i + 1];

                if (timeMs >= change.timeMs && timeMs < next.timeMs)
                {
                    float timeInSection = timeMs - change.timeMs;
                    return (change.beatTime * 4f) + (timeInSection / stepCrochet);
                }
            }

            // Handle last section
            var last = timeChanges[timeChanges.Count - 1];
            float lastSectionTime = timeMs - last.timeMs;
            return (last.beatTime * 4f) + (lastSectionTime / stepCrochet);
        }

		public float BeatToSeconds(float beat)
		{
			return GetStepTimeInMs(beat * 4f) / 1000f;
		}

		public float GetStepTimeInMs(float step)
        {
            for (int i = 0; i < timeChanges.Count - 1; i++)
            {
                var change = timeChanges[i];
                var next = timeChanges[i + 1];
                float stepsAtChange = change.beatTime * 4f;

                if (step >= stepsAtChange && step < (next.beatTime * 4f))
                {
                    float stepsInSection = step - stepsAtChange;
                    return change.timeMs + (stepsInSection * stepCrochet);
                }
            }

            // Handle last section
            var last = timeChanges[timeChanges.Count - 1];
            float stepsInLast = step - (last.beatTime * 4f);
            return last.timeMs + (stepsInLast * stepCrochet);
        }
    }
}
