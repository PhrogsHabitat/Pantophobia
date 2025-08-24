using System.Collections.Generic;
using Phobia.Audio;
using Phobia.Gameplay.Components.Level;
using Phobia.Gameplay.Components.Level.Levels;
using Phobia.Gameplay.Components.Music;
using Phobia.Input;
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

		// NOTE: currentLevel is not used for scene loading. Consider removing or updating its usage.

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
			// Set the current scene id before loading
			CurrentSceneId = sceneId;
			Debug.Log($"[PlayState] LoadScene called with sceneId: {sceneId}");
			try
			{
				CleanupPreviousScene();
				Debug.Log($"[PlayState] After CleanupPreviousScene, _activeScene: {_activeScene?.GetType().Name ?? "null"}");
				if (SceneRegistry.TryCreateScene(sceneId, this, out var newScene))
				{
					Debug.Log($"[PlayState] SceneRegistry returned: {newScene?.GetType().Name}, enabled: {newScene?.enabled}, GameObject active: {newScene?.gameObject.activeSelf}");
					InitializeScene(newScene);
					_activeScene = newScene;
					Debug.Log($"[PlayState] _activeScene set: {_activeScene?.GetType().Name ?? "null"}");
					Debug.Log($"[PlayState] After InitializeScene, _activeScene: {_activeScene?.GetType().Name ?? "null"}");
				}
				else
				{
					Debug.LogError($"[PlayState] SceneRegistry could not create scene for id: {sceneId}");
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError($"Failed to load scene {sceneId}: {e}");
			}
		}
		private void InitializeScene(MonoBehaviour scene)
		{
			// Ensure the scene's GameObject is active
			scene.gameObject.SetActive(true);

			_activeScene = scene;

			LevelData levelData = LevelData.LoadLevelMetadata(CurrentSceneId);
			if (levelData == null)
			{
				Debug.LogError("Failed to load level metadata");
				levelData = CreateFallbackLevelData();
			}

			// Set PlayState properties from LevelData
			defaultCamZoom = levelData.defaultZoom;
			mainCamera.orthographicSize = defaultCamZoom;

			// Example: set PlayState flags from LevelData
			bool forceNoMusic = levelData.forceNoMusic;
			bool isLevel = levelData.isLevel;

			// Optionally, expose these as PlayState properties if needed
			// this.ForceNoMusic = forceNoMusic;
			// this.IsLevel = isLevel;

			// Setup music only if not forced off
			if (!forceNoMusic)
			{
				SetupMusic(levelData);
			}

			// Call Create directly
			if (_activeScene is LevelBase level)
			{
				level.Create();
			}
			else if (_activeScene is UIBase ui)
			{
				ui.Create();
			}
		}

		private void CleanupPreviousScene()
		{
			// ADDED: Full scene cleanup
			Debug.Log($"[PlayState] CleanupPreviousScene called, _activeScene: {_activeScene?.GetType().Name ?? "null"}");
			if (_activeScene != null)
			{
				Destroy(_activeScene.gameObject);
				Debug.Log($"[PlayState] Destroyed _activeScene GameObject: {_activeScene?.GetType().Name ?? "null"}");
				_activeScene = null;
				Debug.Log($"[PlayState] _activeScene set to null in CleanupPreviousScene");
			}

			// Music cleanup
			if (_heartBeatMusic != null)
			{
				_heartBeatMusic.Stop();
				_heartBeatMusic.ReturnToPool();
				_heartBeatMusic = null;
			}

			// Conductor cleanup
			if (Conductor.Instance != null)
			{
				Conductor.Instance.OnBeatHit -= HandleBeat;
				Conductor.Instance.OnStepHit -= HandleStep;
				Conductor.Instance.OnMeasureHit -= HandleMeasure;
			}
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
			GameObject camGO = null;
			UnityEngine.Camera camera = null;
			if (phobiaCamera != null)
			{
				Debug.Log("[PlayState] Found existing PhobiaCamera");
				camGO = phobiaCamera.gameObject;
				camera = phobiaCamera.GetComponent<UnityEngine.Camera>();
			}
			else
			{
				var mainCam = UnityEngine.Camera.main;
				if (mainCam != null)
				{
					var existingPhobiaCam = mainCam.GetComponent<Phobia.Camera.PhobiaCamera>();
					if (existingPhobiaCam == null)
					{
						mainCam.gameObject.AddComponent<Phobia.Camera.PhobiaCamera>();
						Debug.Log("[PlayState] Added PhobiaCamera component to existing main camera");
					}
					camGO = mainCam.gameObject;
					camera = mainCam;
				}
				else
				{
					camGO = new GameObject("PhobiaCamera");
					camera = camGO.AddComponent<UnityEngine.Camera>();
					camGO.AddComponent<Phobia.Camera.PhobiaCamera>();
					camera.orthographic = true;
					camGO.tag = "MainCamera";
					Debug.Log("[PlayState] Created new PhobiaCamera");
				}
			}

			// Ensure a FollowPoint exists as a child of the camera
			var followPoint = camGO.GetComponentInChildren<FollowPoint>();
			if (followPoint == null)
			{
				var followGO = new GameObject("FollowPoint");
				followGO.transform.SetParent(camGO.transform, false);
				followPoint = followGO.AddComponent<FollowPoint>();
			}
			// Assign to PlayState.cameraFollowPoint if possible
			if (PlayState.Instance != null)
			{
				PlayState.Instance.cameraFollowPoint = followPoint.transform;
			}

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

		protected void Update()
		{

		}
    }
}
