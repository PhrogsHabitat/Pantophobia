using System;
using System.Collections;
using System.Collections.Generic;

namespace Phobia.Audio
{
    /// <summary>
    /// Enhanced PhobiaSound with improved modularity and runtime configuration
    /// Provides pooled audio management with spatial audio support and advanced features
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PhobiaSound : MonoBehaviour
    {
        #region Configuration Data

        /// <summary>
        /// Configuration for PhobiaSound instances
        /// </summary>
        [System.Serializable]
        public class SoundConfig
        {
            [Header("Basic Settings")]
            public float volume = 1.0f;
            public bool loop = false;
            public bool important = false;
            public bool persistent = false;
            public bool autoDestroy = true;

            [Header("Spatial Audio")]
            public bool spatialEnabled = false;
            public float maxDistance = 2000f;
            public float minVolume = 0.1f;
            public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
            public float dopplerLevel = 1f;

            [Header("Effects")]
            public bool enableFadeIn = false;
            public float fadeInDuration = 1f;
            public bool enableFadeOut = false;
            public float fadeOutDuration = 1f;

            [Header("Metadata")]
            public string world = "default";
            public string key = "";
            public string label = "unknown";

            /// <summary>
            /// Create a default sound configuration
            /// </summary>
            public static SoundConfig CreateDefault()
            {
                return new SoundConfig();
            }

            /// <summary>
            /// Create a configuration for music
            /// </summary>
            public static SoundConfig CreateMusicConfig()
            {
                return new SoundConfig
                {
                    volume = 0.8f,
                    loop = true,
                    persistent = true,
                    autoDestroy = false,
                    enableFadeIn = true,
                    fadeInDuration = 2f
                };
            }

            /// <summary>
            /// Create a configuration for spatial sound effects
            /// </summary>
            public static SoundConfig CreateSpatialConfig(float maxDist = 50f, float minVol = 0.1f)
            {
                return new SoundConfig
                {
                    spatialEnabled = true,
                    maxDistance = maxDist,
                    minVolume = minVol,
                    rolloffMode = AudioRolloffMode.Custom,
                    dopplerLevel = 0.5f
                };
            }
        }

        #endregion

        #region Static Pool Management

        private static Stack<PhobiaSound> _pool = new Stack<PhobiaSound>();
        private static Dictionary<string, PhobiaSound> _namedSounds = new Dictionary<string, PhobiaSound>();
        private static List<PhobiaSound> _activeSounds = new List<PhobiaSound>();

        #endregion

        #region Public Properties

        [SerializeField] private SoundConfig _config;
        public SoundConfig Config
        {
            get => _config ?? (_config = SoundConfig.CreateDefault());
            set => _config = value;
        }

        public float songPosition = 0;
        public Transform tiedObject = null;
        public bool muted = false;

        // Legacy properties for backward compatibility
        public bool important
        {
            get => Config.important;
            set => Config.important = value;
        }
        public bool persistent
        {
            get => Config.persistent;
            set => Config.persistent = value;
        }
        public string world
        {
            get => Config.world;
            set => Config.world = value;
        }
        public string key
        {
            get => Config.key;
            set => Config.key = value;
        }
        public float maxDistance
        {
            get => Config.maxDistance;
            set => Config.maxDistance = value;
        }
        public float minVolume
        {
            get => Config.minVolume;
            set => Config.minVolume = value;
        }
        public bool spatialEnabled
        {
            get => Config.spatialEnabled;
            set => Config.spatialEnabled = value;
        }

        #endregion

        #region Private Fields

        private bool _shouldPlay = false;
        private float _baseVolume = 1.0f;
        private float _lastCalculatedVolume = -1f;
        private Coroutine _fadeCoroutine;

        // Components
        private AudioSource _audioSource;
        public AudioSource audioSource => _audioSource;
        private Transform _transform;

        #endregion

        #region Events

        public static event Action<float> onVolumeChanged;
        public static event Action<PhobiaSound> onSoundCreated;
        public static event Action<PhobiaSound> onSoundDestroyed;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            _transform = transform;
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            // Initialize with default config if none exists
            if (_config == null)
            {
                _config = SoundConfig.CreateDefault();
            }

            ApplyConfiguration();
        }

        void Update()
        {
            // Update volume if needed
            float calculatedVolume = GetCalculatedVolume();
            if (calculatedVolume != _lastCalculatedVolume)
            {
                _audioSource.volume = calculatedVolume;
                _lastCalculatedVolume = calculatedVolume;
            }

            // Handle delayed playback
            if (_shouldPlay && songPosition < 0)
            {
                songPosition += Time.deltaTime * 1000;
                if (songPosition >= 0)
                {
                    PlayNow();
                }
            }

            // Update song position
            if (_audioSource.isPlaying)
            {
                songPosition = _audioSource.time * 1000; // in milliseconds
            }

            // Update spatial properties
            if (Config.spatialEnabled && tiedObject != null)
            {
                _transform.position = tiedObject.position;
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            _activeSounds.Remove(this);
            onSoundDestroyed?.Invoke(this);
        }

        #endregion

        #region Static Pool Management

        /// <summary>
        /// Clear the sound pool
        /// </summary>
        public static void ClearPool()
        {
            _pool.Clear();
            _namedSounds.Clear();
        }

        /// <summary>
        /// Get a sound from the pool or create a new one
        /// </summary>
        public static PhobiaSound GetFromPool()
        {
            // Ensure there is at least one AudioListener in the scene
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                GameObject listenerGO = new GameObject("PhobiaAudioListener");
                listenerGO.AddComponent<AudioListener>();
            }

            PhobiaSound sound;
            if (_pool.Count > 0)
            {
                sound = _pool.Pop();
                sound.gameObject.SetActive(true);
            }
            else
            {
                GameObject go = new GameObject("PhobiaSound");
                sound = go.AddComponent<PhobiaSound>();
            }

            _activeSounds.Add(sound);
            return sound;
        }

        /// <summary>
        /// Get all active sounds
        /// </summary>
        public static List<PhobiaSound> GetActiveSounds()
        {
            _activeSounds.RemoveAll(s => s == null);
            return new List<PhobiaSound>(_activeSounds);
        }

        /// <summary>
        /// Get a named sound if it exists
        /// </summary>
        public static PhobiaSound GetNamedSound(string name)
        {
            return _namedSounds.TryGetValue(name, out PhobiaSound sound) ? sound : null;
        }

        /// <summary>
        /// Stop all active sounds
        /// </summary>
        public static void StopAllSounds()
        {
            foreach (var sound in GetActiveSounds())
            {
                sound.Stop();
            }
        }

        /// <summary>
        /// Set global volume for all sounds
        /// </summary>
        public static void SetGlobalVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            onVolumeChanged?.Invoke(volume);
        }

        #endregion


        #region Public Methods - Configuration

        /// <summary>
        /// Initialize the sound with a configuration
        /// </summary>
        public void Initialize(SoundConfig config, AudioClip clip = null)
        {
            _config = config ?? SoundConfig.CreateDefault();

            if (clip != null)
            {
                _audioSource.clip = clip;
            }

            ApplyConfiguration();

            // Register as named sound if key is provided
            if (!string.IsNullOrEmpty(_config.key))
            {
                _namedSounds[_config.key] = this;
            }

            onSoundCreated?.Invoke(this);
        }

        /// <summary>
        /// Apply the current configuration to the audio source
        /// </summary>
        public void ApplyConfiguration()
        {
            if (_config == null)
            {
                return;
            }

            _baseVolume = _config.volume;
            _audioSource.loop = _config.loop;
            _audioSource.rolloffMode = _config.rolloffMode;
            _audioSource.dopplerLevel = _config.dopplerLevel;

            SetupSpatialAudio();
        }

        /// <summary>
        /// Update configuration at runtime
        /// </summary>
        public void UpdateConfig(SoundConfig newConfig)
        {
            _config = newConfig;
            ApplyConfiguration();
        }

        /// <summary>
        /// Set up spatial audio based on current configuration
        /// </summary>
        public void SetupSpatialAudio()
        {
            if (_config.spatialEnabled)
            {
                // Configure Unity's built-in spatial audio
                _audioSource.spatialBlend = 1f; // Full 3D audio
                _audioSource.rolloffMode = _config.rolloffMode;

                if (_config.rolloffMode == AudioRolloffMode.Custom)
                {
                    // Custom volume curve (linear from max to min volume)
                    AnimationCurve volumeCurve = new AnimationCurve(
                        new Keyframe(0, 1f),
                        new Keyframe(_config.maxDistance, _config.minVolume)
                    );
                    _audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, volumeCurve);
                }

                // Configure distance parameters
                _audioSource.minDistance = 1f;
                _audioSource.maxDistance = _config.maxDistance;
                _audioSource.dopplerLevel = _config.dopplerLevel;
            }
            else
            {
                // Reset to 2D audio
                _audioSource.spatialBlend = 0f;
                _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            }
        }

        #endregion

        #region Public Methods - Playback Control

        /// <summary>
        /// Play the sound with optional parameters
        /// </summary>
        public void Play(bool forceRestart = false, float startTime = 0)
        {
            if (forceRestart)
            {
                Stop();
            }
            else if (IsPlaying())
            {
                return;
            }

            // Handle fade in if enabled
            if (_config.enableFadeIn && !forceRestart)
            {
                PlayWithFadeIn(startTime);
                return;
            }

            if (startTime < 0)
            {
                _shouldPlay = true;
                songPosition = startTime;
                return;
            }

            PlayNow(startTime);
        }

        /// <summary>
        /// Stop the sound with optional fade out
        /// </summary>
        public void Stop(bool useFadeOut = false)
        {
            if (useFadeOut && _config.enableFadeOut && IsPlaying())
            {
                StopWithFadeOut();
                return;
            }

            _audioSource.Stop();
            _shouldPlay = false;
            songPosition = 0;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        /// <summary>
        /// Check if the sound is currently playing
        /// </summary>
        public bool IsPlaying()
        {
            return _audioSource.isPlaying || _shouldPlay;
        }

        /// <summary>
        /// Pause the sound
        /// </summary>
        public void Pause()
        {
            _audioSource.Pause();
        }

        /// <summary>
        /// Resume the sound
        /// </summary>
        public void Resume()
        {
            _audioSource.UnPause();
        }

        #endregion


        #region Public Methods - Volume and Effects

        /// <summary>
        /// Set the volume of this sound
        /// </summary>
        public void SetVolume(float volume)
        {
            _baseVolume = Mathf.Clamp01(volume);
            _config.volume = _baseVolume;
            onVolumeChanged?.Invoke(GetCalculatedVolume());
        }

        /// <summary>
        /// Get the base volume of this sound
        /// </summary>
        public float GetVolume() => _baseVolume;

        /// <summary>
        /// Fade to a target volume over time
        /// </summary>
        public void FadeTo(float targetVolume, float duration, Action onComplete = null)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _fadeCoroutine = StartCoroutine(FadeRoutine(targetVolume, duration, onComplete));
        }

        /// <summary>
        /// Tie this sound to a transform for spatial audio
        /// </summary>
        public void TieTo(Transform target, bool enableSpatial = true)
        {
            tiedObject = target;
            _config.spatialEnabled = enableSpatial;
            SetupSpatialAudio();

            if (tiedObject != null)
            {
                _transform.position = tiedObject.position;
            }
        }

        /// <summary>
        /// Set spatial audio distance parameters
        /// </summary>
        public void SetDistanceParams(float minVol, float maxDist)
        {
            _config.minVolume = minVol;
            _config.maxDistance = maxDist;
            SetupSpatialAudio();
        }

        #endregion

        #region Public Methods - Utility

        /// <summary>
        /// Return this sound to the pool
        /// </summary>
        public void ReturnToPool()
        {
            if (!_config.persistent)
            {
                Stop();

                // Remove from named sounds if applicable
                if (!string.IsNullOrEmpty(_config.key) && _namedSounds.ContainsKey(_config.key))
                {
                    _namedSounds.Remove(_config.key);
                }

                gameObject.SetActive(false);
                _pool.Push(this);
                _activeSounds.Remove(this);
            }
        }

        /// <summary>
        /// Clone this sound with the same configuration
        /// </summary>
        public PhobiaSound Clone()
        {
            PhobiaSound clone = Create(_audioSource.clip, _config);
            return clone;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the calculated volume considering mute state and global volume
        /// </summary>
        private float GetCalculatedVolume()
        {
            return muted ? 0 : _baseVolume * AudioListener.volume;
        }

        /// <summary>
        /// Play immediately with the given start time
        /// </summary>
        private void PlayNow(float startTime = 0)
        {
            _audioSource.time = Mathf.Max(0, startTime);
            _audioSource.Play();
            _shouldPlay = false;
        }

        /// <summary>
        /// Play with fade in effect
        /// </summary>
        private void PlayWithFadeIn(float startTime = 0)
        {
            float originalVolume = _baseVolume;
            _baseVolume = 0;
            PlayNow(startTime);
            FadeTo(originalVolume, _config.fadeInDuration);
        }

        /// <summary>
        /// Stop with fade out effect
        /// </summary>
        private void StopWithFadeOut()
        {
            FadeTo(0, _config.fadeOutDuration, () =>
            {
                _audioSource.Stop();
                _shouldPlay = false;
                songPosition = 0;
            });
        }

        /// <summary>
        /// Coroutine for fading volume
        /// </summary>
        private IEnumerator FadeRoutine(float targetVolume, float duration, Action onComplete)
        {
            float startVolume = _baseVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _baseVolume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            _baseVolume = targetVolume;
            _fadeCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Auto-destroy coroutine for non-looping sounds
        /// </summary>
        private IEnumerator AutoDestroyAfterPlayback()
        {
            yield return new WaitWhile(() => _audioSource.isPlaying);
            ReturnToPool();
        }

        #endregion


        #region Static Factory Methods

        /// <summary>
        /// Create a PhobiaSound with basic parameters (legacy compatibility)
        /// </summary>
        public static PhobiaSound Create(AudioClip clip, float volume = 1f, bool loop = false, bool autoDestroy = true)
        {
            var config = SoundConfig.CreateDefault();
            config.volume = volume;
            config.loop = loop;
            config.autoDestroy = autoDestroy;

            return Create(clip, config);
        }

        /// <summary>
        /// Create a PhobiaSound with full configuration
        /// </summary>
        public static PhobiaSound Create(AudioClip clip, SoundConfig config)
        {
            if (clip == null)
            {
                Debug.LogError("[PhobiaSound] Cannot create sound with null AudioClip");
                return null;
            }

            PhobiaSound sound = GetFromPool();
            sound.Initialize(config, clip);

            if (config.autoDestroy && !config.loop)
            {
                sound.StartCoroutine(sound.AutoDestroyAfterPlayback());
            }

            return sound;
        }

        /// <summary>
        /// Create and immediately play a one-shot sound
        /// </summary>
        public static PhobiaSound PlayOnce(AudioClip clip, float volume = 1f)
        {
            PhobiaSound sound = Create(clip, volume, false, true);
            sound.Play();
            return sound;
        }

        /// <summary>
        /// Create a music sound with appropriate settings
        /// </summary>
        public static PhobiaSound CreateMusic(AudioClip clip, float volume = 0.8f, string key = null)
        {
            var config = SoundConfig.CreateMusicConfig();
            config.volume = volume;
            if (!string.IsNullOrEmpty(key))
            {
                config.key = key;
            }

            return Create(clip, config);
        }

        /// <summary>
        /// Create a spatial sound effect
        /// </summary>
        public static PhobiaSound CreateSpatial(AudioClip clip, Transform target, float maxDistance = 50f, float volume = 1f)
        {
            var config = SoundConfig.CreateSpatialConfig(maxDistance);
            config.volume = volume;

            PhobiaSound sound = Create(clip, config);
            if (sound != null)
            {
                sound.TieTo(target, true);
            }

            return sound;
        }

        /// <summary>
        /// Load and create a sound from Resources
        /// </summary>
        public static PhobiaSound LoadAndCreate(string resourcePath, SoundConfig config = null)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogError($"[PhobiaSound] Could not load audio clip from path: {resourcePath}");
                return null;
            }

            config = config ?? SoundConfig.CreateDefault();
            config.label = clip.name;

            return Create(clip, config);
        }

        #endregion
    }
}
