using System.Collections.Generic;
using Phobia.Audio;
using Phobia.Gameplay.Components.Level;
using Phobia.Gameplay.Components.Level.Levels;
using Phobia.Gameplay.Components.Music;
using Phobia.RegistryShit;
using Phobia.ui;
using UnityEngine;

namespace Phobia.Gameplay
{
    public class PlayState : MonoBehaviour
    {
        public static PlayState Instance { get; private set; }

        public UnityEngine.Camera mainCamera;
        public Transform cameraFollowPoint;
        public float defaultCamZoom = 1.05f;

        public string CurrentSceneId { get; private set; }
        private MonoBehaviour _activeScene;

        public string currentLevel = "cumLevel";

        private PhobiaSound _heartBeatMusic;
        public PhobiaSound heartBeatMusic => _heartBeatMusic;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                mainCamera = FindOrCreatePhobiaCamera();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadScene(string sceneId)
        {
            // Cleanup previous scene
            if (_activeScene != null)
            {
                Destroy(_activeScene.gameObject);
                _activeScene = null;
            }

            // Create new scene
            if (SceneRegistry.TryCreateScene(sceneId, this, out var newScene))
            {
                _activeScene = newScene;
                CurrentSceneId = sceneId;

                // Handle scene-specific initialization
                if (newScene is LevelBase level)
                {
                    InitializeLevel(level);
                    currentLevel = sceneId;
                }
                else if (newScene is UIBase ui)
                {
                    InitializeUI(ui);
                }
            }
        }

        private void InitializeLevel(LevelBase level)
        {
            CleanupPreviousLevel();

            LevelData levelData = LevelData.LoadLevelMetadata(CurrentSceneId);
            if (levelData == null)
            {
                Debug.LogError("Failed to load level metadata");
                levelData = CreateFallbackLevelData();
            }

            defaultCamZoom = levelData.defaultZoom;
            mainCamera.orthographicSize = defaultCamZoom;

            SetupMusic(levelData);

            level.Create();
            level.InitLevelSpecifics();
        }

        private void InitializeUI(UIBase ui)
        {
            // UI-specific initialization
            ui.CreateUI();
            ui.InitUISpecifics();
            ui.ShowUI();
        }

        private void CleanupPreviousLevel()
        {
            if (_activeScene != null)
            {
                Destroy(_activeScene.gameObject);
                _activeScene = null;
            }

            if (_heartBeatMusic != null)
            {
                _heartBeatMusic.Stop();
                _heartBeatMusic.ReturnToPool();
                _heartBeatMusic = null;
            }

            if (Conductor.Instance != null)
            {
                Conductor.Instance.OnBeatHit -= HandleBeat;
                Conductor.Instance.OnStepHit -= HandleStep;
                Conductor.Instance.OnMeasureHit -= HandleMeasure;
            }
        }

        private void EnsureConductor()
        {
            if (Conductor.Instance == null)
            {
                Debug.LogWarning("[PlayState] Conductor.Instance is null. Creating a new Conductor.");
                var conductorGO = new GameObject("Conductor");
                conductorGO.AddComponent<Conductor>();
            }
        }

        private void SetupMusic(LevelData levelData)
        {
            EnsureConductor();

            string musicPath = Paths.levelMusic(CurrentSceneId, levelData.songId);
            Debug.Log($"[SetupMusic] Loading music from path: {musicPath}");

            AudioClip clip = Resources.Load<AudioClip>(musicPath);

            if (clip != null)
            {
                Debug.Log("[SetupMusic] Music loaded successfully.");
                _heartBeatMusic = PhobiaSound.Create(clip, 1f, true, false);
                _heartBeatMusic.persistent = true;
                _heartBeatMusic.Play();

                if (Conductor.Instance != null)
                {
                    Debug.Log("[SetupMusic] Conductor instance found. Mapping song.");
                    Conductor.Instance.MapSong(_heartBeatMusic, levelData.bpm, levelData.timeChanges);
                    Conductor.Instance.timeSignatureNumerator = levelData.songSigNum;
                    Conductor.Instance.timeSignatureDenominator = levelData.songSigDenum;

                    Conductor.Instance.OnBeatHit += HandleBeat;
                    Conductor.Instance.OnStepHit += HandleStep;
                    Conductor.Instance.OnMeasureHit += HandleMeasure;
                }
                else
                {
                    Debug.LogError("[SetupMusic] Conductor.Instance is still null after fallback.");
                }
            }
            else
            {
                Debug.LogError($"[SetupMusic] Music not found: {musicPath}");
            }
        }

        private void CreateBackground(Color color)
        {
            GameObject bg = new GameObject("Background");
            SpriteRenderer renderer = bg.AddComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sprite = Sprite.Create(
                new Texture2D(1, 1),
                new Rect(0, 0, 1, 1),
                Vector2.zero
            );
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(20, 20);
        }

        private void HandleBeat()
        {
            Debug.Log("Beat hit");
        }

        private void HandleStep()
        {
            Debug.Log("Step hit");
        }

        private void HandleMeasure()
        {
            Debug.Log("Measure hit");
        }

        public static UnityEngine.Camera FindOrCreatePhobiaCamera()
        {
            var phobiaCamera = FindFirstObjectByType<Phobia.Camera.PhobiaCamera>();
            if (phobiaCamera != null)
            {
                Debug.Log("[PlayState] Found existing PhobiaCamera");
                return phobiaCamera.GetComponent<UnityEngine.Camera>();
            }

            var mainCam = UnityEngine.Camera.main;
            if (mainCam != null)
            {
                var existingPhobiaCam = mainCam.GetComponent<Phobia.Camera.PhobiaCamera>();
                if (existingPhobiaCam == null)
                {
                    mainCam.gameObject.AddComponent<Phobia.Camera.PhobiaCamera>();
                    Debug.Log("[PlayState] Added PhobiaCamera component to existing main camera");
                }
                return mainCam;
            }

            var camGO = new GameObject("PhobiaCamera");
            var camera = camGO.AddComponent<UnityEngine.Camera>();
            var phobiaCam = camGO.AddComponent<Phobia.Camera.PhobiaCamera>();
            camera.orthographic = true;
            camGO.tag = "MainCamera";
            Debug.Log("[PlayState] Created new PhobiaCamera");

            return camera;
        }

        private LevelData CreateFallbackLevelData()
        {
            return new LevelData
            {
                defaultZoom = 1.05f,
                bgColor = new Color(0.1f, 0.1f, 0.2f, 1f),
                songId = "default",
                bpm = 120,
                songSigNum = 4,
                songSigDenum = 4,
                timeChanges = new List<Conductor.TimeChange>
                {
                    new Conductor.TimeChange
                    {
                        timeMs = 0,
                        bpm = 120,
                        numerator = 4,
                        denominator = 4
                    }
                }
            };
        }
    }
}
