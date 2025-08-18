using UnityEngine;

namespace Phobia.Core
{
	/// <summary>
	/// Minimal bootstrap component. Attach to an empty GameObject in any scene.
	/// - Sets basic app settings (quality/framerate, vsync, screen settings)
	/// - Uses minimal Phobia code (no heavy caches)
	/// - Invokes Main.Initialize() which performs full initialization and state load
	/// </summary>
	[DefaultExecutionOrder(-1000)]
	public class PhobiaApplication : MonoBehaviour
	{
		[Header("Application Settings")]
		public bool disableVSync = true;
		public int targetFramerate = Constants.FRAMERATE;
		public bool swagDebug = !Constants.EDITOR_DEBUG;
		public bool dontDestroyOnLoad = !Constants.DESTROY_ON_LOAD;

		[Header("Screen Settings")]
		public bool fullScreen = false;
		public int screenWidth = Constants.GAME_WIDTH;
		public int screenHeight = Constants.GAME_HEIGHT;
		public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;

		private void Awake()
		{
			// Set VSync

			if (disableVSync)
			{
				QualitySettings.vSyncCount = 0;
			}

			// Set target framerate

			if (targetFramerate > 0)
			{
				Application.targetFrameRate = targetFramerate;
			}

			// Set DontDestroyOnLoad

			if (dontDestroyOnLoad)
			{
				DontDestroyOnLoad(gameObject);
			}

			// Configure screen settings

			Screen.SetResolution(screenWidth, screenHeight, fullScreenMode);
			Screen.fullScreen = fullScreen;

			#if UNITY_EDITOR
			Debug.unityLogger.logEnabled = true;
			#endif

		}

		private void Start()
		{
			try
			{
				Debug.Log("[PhobiaApplication] Starting initialization...");

				// Defer heavy lifting to Main
				Main.Initialize();

				Debug.Log("[PhobiaApplication] Initialization completed successfully.");
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[PhobiaApplication] Initialization failed: {ex.Message}\n{ex.StackTrace}");
			}
		}
	}
}

