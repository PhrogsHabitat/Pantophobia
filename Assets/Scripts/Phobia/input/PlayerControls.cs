using System.Collections.Generic;
using UnityEngine;

namespace Phobia.Input
{
	/// <summary>
	/// Unified controls system for Pantophobia.
	/// This is the main interface for all input handling in the game.
	/// Provides simple static access: Controls.isPressed("action")
	/// Manages PhobiaInput and all control logic.
	/// </summary>
	public static class Controls
	{
		#region Core System

		private static PhobiaInput _input;
		private static bool _initialized = false;

		/// <summary>
		/// Initialize the controls system. Called from Main.cs.
		/// This is the ONLY initialization you need!
		/// </summary>
		public static void Initialize()
		{
			if (_initialized)
			{
				return;
			}

			Debug.Log("[CONTROLS] Initializing unified Controls system...");

			// Ensure PhobiaInput is initialized
			if (PhobiaInput.Instance == null)
			{
				PhobiaInput.EnsureInstance();
			}
			_input = PhobiaInput.Instance;

			// Load saved actions from PlayerControlsSave
			LoadSavedActions();

			_initialized = true;
			Debug.Log("[CONTROLS] Controls system initialized successfully - ready to use!");
		}

		/// <summary>
		/// Check if the system is ready to use.
		/// </summary>
		public static bool IsReady => _initialized && _input != null;

		#endregion

		#region Action Creation & Management

		/// <summary>
		/// Create all actions from saved key bindings.
		/// </summary>
		private static void LoadSavedActions()
		{
			var actionNames = GetAllActionNames();
			if (actionNames.Count == 0)
			{
				Debug.Log("[CONTROLS] No saved actions found, creating defaults");
				CreateDefaultActions();
				return;
			}

			Debug.Log($"[CONTROLS] Loading {actionNames.Count} saved actions");
			foreach (var actionName in actionNames)
			{
				var bindings = GetKeyBindings(actionName);
				if (bindings.Count > 0)
				{
					AddAction(actionName, bindings.ToArray());
				}
			}

			Debug.Log($"[CONTROLS] Loaded {GetActionCount()} actions from save data");
		}

		/// <summary>
		/// Create all default actions that the game needs.
		/// </summary>
		private static void CreateDefaultActions()
		{
			// UI Navigation
			AddAction("ui_up", "<Keyboard>/upArrow", "<Keyboard>/w");
			AddAction("ui_down", "<Keyboard>/downArrow", "<Keyboard>/s");
			AddAction("ui_left", "<Keyboard>/leftArrow", "<Keyboard>/a");
			AddAction("ui_right", "<Keyboard>/rightArrow", "<Keyboard>/d");

			// Game Actions
			AddAction("accept", "<Keyboard>/enter", "<Keyboard>/space", "<Keyboard>/z");
			AddAction("back", "<Keyboard>/escape", "<Keyboard>/x");
			AddAction("pause", "<Keyboard>/escape", "<Keyboard>/p");
			AddAction("reset", "<Keyboard>/r");

			// Note/Rhythm Actions
			AddAction("note_left", "<Keyboard>/leftArrow", "<Keyboard>/a");
			AddAction("note_down", "<Keyboard>/downArrow", "<Keyboard>/s");
			AddAction("note_up", "<Keyboard>/upArrow", "<Keyboard>/w");
			AddAction("note_right", "<Keyboard>/rightArrow", "<Keyboard>/d");

			// Debug Actions
			AddAction("debug_info", "<Keyboard>/f1");
			AddAction("debug_reload", "<Keyboard>/f5");

			SaveControls();
			Debug.Log($"[CONTROLS] Created {GetActionCount()} default actions and saved to disk");
		}

		/// <summary>
		/// Add a new action at runtime. Can be called from any script.
		/// Usage: Controls.AddAction("swagAction", "<Keyboard>/q");
		/// </summary>
		public static void AddAction(string actionName, params string[] bindings)
		{
			if (!IsReady)
			{
				Debug.LogWarning($"[CONTROLS] Cannot add action '{actionName}' - system not initialized");
				return;
			}

			Debug.Log($"[CONTROLS] Adding action '{actionName}' with bindings: {string.Join(", ", bindings)}");
			var action = _input.AddAction(actionName, bindings);

			if (action != null)
			{
				Debug.Log($"[CONTROLS] Successfully added action '{actionName}'. Total actions: {GetActionCount()}");
			}
			else
			{
				Debug.LogError($"[CONTROLS] Failed to add action '{actionName}'");
			}
		}

		#endregion

		#region Main API

		public static bool isPressed(string actionName)
		{
			if (!IsReady)
			{
				return false;
			}
			return _input.CheckPressed(actionName);
		}

		public static bool isHeld(string actionName)
		{
			if (!IsReady)
			{
				return false;
			}
			return _input.Check(actionName);
		}

		public static bool isReleased(string actionName)
		{
			if (!IsReady)
			{
				return false;
			}
			return _input.CheckReleased(actionName);
		}

		#endregion

		#region Convenience Properties

		public static bool UI_UP => isPressed("ui_up");
		public static bool UI_DOWN => isPressed("ui_down");
		public static bool UI_LEFT => isPressed("ui_left");
		public static bool UI_RIGHT => isPressed("ui_right");
		public static bool ACCEPT => isPressed("accept");
		public static bool BACK => isPressed("back");

		#endregion

		#region Utility Methods

		public static int GetActionCount()
		{
			return IsReady ? _input.GetActionCount() : 0;
		}

		public static bool HasAction(string actionName)
		{
			return IsReady && _input.HasAction(actionName);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void DebugListActions()
		{
			if (Application.isEditor)
			{
				if (!IsReady)
				{
					Debug.Log("[CONTROLS] System not initialized");
					return;
				}

				Debug.Log($"[CONTROLS] Registered Actions ({GetActionCount()}):");
				var actionNames = _input.GetActionNames();
				foreach (var actionName in actionNames)
				{
					Debug.Log($"  - {actionName}");
				}
			}
			else
			{
				Debug.LogWarning("[CONTROLS DEBUG] DebugListActions is only available in the Unity Editor.");
			}
		}

		public static void DebugTestAction(string actionName)
		{
			if (!IsReady)
			{
				Debug.Log("[CONTROLS] System not initialized");
				return;
			}

			bool exists = HasAction(actionName);
			bool pressed = isPressed(actionName);
			bool held = isHeld(actionName);

			if (pressed || held)
			{
				Debug.Log($"[CONTROLS] Action '{actionName}' - Exists: {exists}, Pressed: {pressed}, Held: {held}");
			}

			if (!exists)
			{
				Debug.LogWarning($"[CONTROLS] Action '{actionName}' does not exist! Available actions:");
				DebugListActions();
			}
		}

		public static void DebugCheckAction(string actionName)
		{
			if (!IsReady)
			{
				Debug.Log("[CONTROLS] System not initialized");
				return;
			}

			bool exists = HasAction(actionName);
			Debug.Log($"[CONTROLS] Action '{actionName}' exists: {exists}");

			if (!exists)
			{
				Debug.LogWarning($"[CONTROLS] Action '{actionName}' does not exist! Available actions:");
				DebugListActions();
			}
			else
			{
				Debug.Log($"[CONTROLS] Action '{actionName}' is ready for input testing");
			}
		}

		public static void EnableDebugLogging()
		{
			if (IsReady)
			{
				_input.Config.enableDebugLogging = true;
				Debug.Log("[CONTROLS] Debug logging enabled");
			}
		}

		public static void DisableDebugLogging()
		{
			if (IsReady)
			{
				_input.Config.enableDebugLogging = false;
				Debug.Log("[CONTROLS] Debug logging disabled");
			}
		}

		public static void TestInputSystem()
		{
			if (!IsReady)
			{
				Debug.LogError("[CONTROLS] System not ready!");
				return;
			}

			Debug.Log("[CONTROLS] === INPUT SYSTEM TEST ===");
			Debug.Log($"[CONTROLS] System ready: {IsReady}");
			Debug.Log($"[CONTROLS] Total actions: {GetActionCount()}");

			AddAction("test_action", "<Keyboard>/t");
			Debug.Log("[CONTROLS] Added test action 'test_action' bound to T key");

			bool exists = HasAction("test_action");
			Debug.Log($"[CONTROLS] Test action exists: {exists}");

			if (exists)
			{
				Debug.Log("[CONTROLS] SUCCESS: Action system is working!");
				Debug.Log("[CONTROLS] Press T to test the action, then check console");
			}
			else
			{
				Debug.LogError("[CONTROLS] FAILED: Action was not added properly");
			}

			Debug.Log("[CONTROLS] === TEST COMPLETE ===");
		}

		#endregion

		#region Controls Save System

		[System.Serializable]
		public class ControlsData
		{
			public Dictionary<string, List<string>> keyBindings = new Dictionary<string, List<string>>();
			public InputSettings inputSettings = new InputSettings();
			public ControlPreferences preferences = new ControlPreferences();
			public long lastSaved = 0;

			public static ControlsData CreateDefault()
			{
				var data = new ControlsData();
				data.lastSaved = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				// Default key bindings
				data.keyBindings["ui_up"] = new List<string> { "<Keyboard>/upArrow", "<Keyboard>/w" };
				data.keyBindings["ui_down"] = new List<string> { "<Keyboard>/downArrow", "<Keyboard>/s" };
				data.keyBindings["ui_left"] = new List<string> { "<Keyboard>/leftArrow", "<Keyboard>/a" };
				data.keyBindings["ui_right"] = new List<string> { "<Keyboard>/rightArrow", "<Keyboard>/d" };
				data.keyBindings["accept"] = new List<string> { "<Keyboard>/enter", "<Keyboard>/space", "<Keyboard>/z" };
				data.keyBindings["back"] = new List<string> { "<Keyboard>/escape", "<Keyboard>/x" };
				data.keyBindings["pause"] = new List<string> { "<Keyboard>/escape", "<Keyboard>/p" };
				data.keyBindings["reset"] = new List<string> { "<Keyboard>/r" };
				data.keyBindings["note_left"] = new List<string> { "<Keyboard>/leftArrow", "<Keyboard>/a" };
				data.keyBindings["note_down"] = new List<string> { "<Keyboard>/downArrow", "<Keyboard>/s" };
				data.keyBindings["note_up"] = new List<string> { "<Keyboard>/upArrow", "<Keyboard>/w" };
				data.keyBindings["note_right"] = new List<string> { "<Keyboard>/rightArrow", "<Keyboard>/d" };
				data.keyBindings["debug_info"] = new List<string> { "<Keyboard>/f1" };
				data.keyBindings["debug_reload"] = new List<string> { "<Keyboard>/f5" };
				return data;
			}
		}

		[System.Serializable]
		public class InputSettings
		{
			public string controlScheme = "PhobiaKeyboard";
			public bool enableBuffering = true;
			public float bufferSeconds = 0.15f;
			public bool enableDeadzone = true;
			public float deadzoneThreshold = 0.2f;
			public bool lockAndHideCursor = false;
			public bool enableDebugLogging = false;
		}

		[System.Serializable]
		public class ControlPreferences
		{
			public bool autoSaveBindings = true;
			public bool showInputHints = true;
			public float inputSensitivity = 1.0f;
			public bool enableHapticFeedback = true;
		}

		private static ControlsData _controlsData;
		private const string SAVE_FILE_NAME = "PlayerControls.json";

		private static string GetSavePath()
		{
			return System.IO.Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
		}

		private static void LoadControlsData()
		{
			if (System.IO.File.Exists(GetSavePath()))
			{
				string json = System.IO.File.ReadAllText(GetSavePath());
				_controlsData = JsonUtility.FromJson<ControlsData>(json);
				if (_controlsData == null || _controlsData.keyBindings == null)
				{
					Debug.LogWarning("[CONTROLS] Invalid controls data loaded. Resetting to defaults.");
					_controlsData = ControlsData.CreateDefault();
				}
			}
			else
			{
				_controlsData = ControlsData.CreateDefault();
			}
		}

		private static void SaveControlsData()
		{
			if (_controlsData == null) { return; }
			string json = JsonUtility.ToJson(_controlsData, true);
			System.IO.File.WriteAllText(GetSavePath(), json);
			_controlsData.lastSaved = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			Debug.Log($"[CONTROLS] Controls data saved to {SAVE_FILE_NAME}");
		}

		public static List<string> GetKeyBindings(string actionName)
		{
			if (_controlsData == null || !_controlsData.keyBindings.ContainsKey(actionName)) { return new List<string>(); }
			return new List<string>(_controlsData.keyBindings[actionName]);
		}

		public static void SetKeyBindings(string actionName, params string[] bindings)
		{
			if (_controlsData == null) { LoadControlsData(); }
			_controlsData.keyBindings[actionName] = new List<string>(bindings);
			SaveControlsData();
			AddAction(actionName, bindings);
			Debug.Log($"[CONTROLS] Set bindings for '{actionName}': {string.Join(", ", bindings)}");
		}

		public static void ResetActionToDefault(string actionName)
		{
			var defaultData = ControlsData.CreateDefault();
			if (defaultData.keyBindings.ContainsKey(actionName))
			{
				_controlsData.keyBindings[actionName] = new List<string>(defaultData.keyBindings[actionName]);
				SaveControlsData();
				AddAction(actionName, _controlsData.keyBindings[actionName].ToArray());
				Debug.Log($"[CONTROLS] Reset '{actionName}' to default bindings");
			}
		}

		public static void ResetAllToDefaults()
		{
			_controlsData = ControlsData.CreateDefault();
			SaveControlsData();
			LoadSavedActions();
			Debug.Log("[CONTROLS] Reset all actions to default bindings");
		}

		public static void SaveControls()
		{
			SaveControlsData();
		}

		public static List<string> GetAllActionNames()
		{
			if (_controlsData == null) { LoadControlsData(); }
			return new List<string>(_controlsData.keyBindings.Keys);
		}

		public static ControlPreferences Preferences => _controlsData?.preferences;
		public static InputSettings Settings => _controlsData?.inputSettings;

		#endregion
	}
}
