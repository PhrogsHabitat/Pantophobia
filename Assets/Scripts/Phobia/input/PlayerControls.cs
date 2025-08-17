using System.Collections.Generic;

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

			// Add all default actions

			CreateDefaultActions();

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

			Debug.Log($"[CONTROLS] Created {GetActionCount()} default actions");
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


		/// <summary>
		/// Check if an action was just pressed this frame.
		/// Usage: Controls.isPressed("action")
		/// This is the main method you'll use everywhere!
		/// </summary>
		public static bool isPressed(string actionName)
		{
			if (!IsReady)
			{
				return false;
			}

			return _input.CheckPressed(actionName);
		}


		/// <summary>
		/// Check if an action is currently being held.
		/// Usage: Controls.isHeld("ui_up")
		/// </summary>
		public static bool isHeld(string actionName)
		{
			if (!IsReady)
			{
				return false;
			}

			return _input.Check(actionName);
		}


		/// <summary>
		/// Check if an action was just released this frame.
		/// Usage: Controls.isReleased("note_left")
		/// </summary>
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

		// Quick access to common actions - use these for cleaner code

		public static bool UI_UP => isPressed("ui_up");
		public static bool UI_DOWN => isPressed("ui_down");
		public static bool UI_LEFT => isPressed("ui_left");
		public static bool UI_RIGHT => isPressed("ui_right");
		public static bool ACCEPT => isPressed("accept");
		public static bool BACK => isPressed("back");

		#endregion

		#region Utility Methods


		/// <summary>
		/// Get the number of registered actions.
		/// </summary>
		public static int GetActionCount()
		{
			return IsReady ? _input.GetActionCount() : 0;
		}


		/// <summary>
		/// Check if an action exists.
		/// </summary>
		public static bool HasAction(string actionName)
		{
			return IsReady && _input.HasAction(actionName);
		}


		/// <summary>
		/// Debug method to list all actions.
		/// </summary>
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


		/// <summary>
		/// Debug method to test if an action is working properly.
		/// Call this in Update() to continuously test an action.
		/// </summary>
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


		/// <summary>
		/// One-time debug check for an action. Call this once to see action status.
		/// </summary>
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


		/// <summary>
		/// Enable debug logging for the input system.
		/// </summary>
		public static void EnableDebugLogging()
		{
			if (IsReady)
			{
				_input.Config.enableDebugLogging = true;
				Debug.Log("[CONTROLS] Debug logging enabled");
			}
		}


		/// <summary>
		/// Disable debug logging for the input system.
		/// </summary>
		public static void DisableDebugLogging()
		{
			if (IsReady)
			{
				_input.Config.enableDebugLogging = false;
				Debug.Log("[CONTROLS] Debug logging disabled");
			}
		}


		/// <summary>
		/// Test method to verify the input system is working.
		/// Call this from your OffsetState to test the fix.
		/// </summary>
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

			// Test adding a simple action

			AddAction("test_action", "<Keyboard>/t");
			Debug.Log("[CONTROLS] Added test action 'test_action' bound to T key");

			// Check if it was added

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

		#region Save System Integration


		/// <summary>
		/// Load saved actions from PlayerControlsSave or create defaults.
		/// </summary>
		private static void LoadSavedActions()
		{
			var actionNames = PlayerControlsSave.Instance.GetAllActionNames();
			if (actionNames.Count == 0)
			{
				Debug.Log("[CONTROLS] No saved actions found, creating defaults");
				CreateDefaultActions();
				return;
			}

			Debug.Log($"[CONTROLS] Loading {actionNames.Count} saved actions");
			foreach (var actionName in actionNames)
			{
				var bindings = PlayerControlsSave.Instance.GetKeyBindings(actionName);
				if (bindings.Count > 0)
				{
					AddAction(actionName, bindings.ToArray());
				}
			}

			Debug.Log($"[CONTROLS] Loaded {GetActionCount()} actions from save data");
		}


		/// <summary>
		/// Save current key bindings to PlayerControlsSave.
		/// </summary>
		public static void SaveKeyBindings()
		{
			if (!IsReady)
			{
				Debug.LogWarning("[CONTROLS] Cannot save - system not ready");
				return;
			}

			// This would require PhobiaInput to expose its action bindings
			// For now, we'll just save the current state

			PlayerControlsSave.Instance.SaveControls();
			Debug.Log("[CONTROLS] Key bindings saved");
		}


		/// <summary>
		/// Set key bindings for an action and save them.
		/// </summary>
		public static void SetKeyBindings(string actionName, params string[] bindings)
		{
			if (!IsReady)
			{
				Debug.LogWarning($"[CONTROLS] Cannot set bindings for '{actionName}' - system not ready");
				return;
			}

			if (PlayerControlsSave.Instance != null)
			{
				PlayerControlsSave.Instance.SetKeyBindings(actionName, bindings);
			}

			// Update the runtime action (this would need PhobiaInput support for rebinding)

			Debug.Log($"[CONTROLS] Set bindings for '{actionName}': {string.Join(", ", bindings)}");
		}


		/// <summary>
		/// Get saved key bindings for an action.
		/// </summary>
		public static List<string> GetKeyBindings(string actionName)
		{
			if (PlayerControlsSave.Instance != null)
			{
				return PlayerControlsSave.Instance.GetKeyBindings(actionName);
			}
			return new List<string>();
		}


		/// <summary>
		/// Reset an action to default bindings.
		/// </summary>
		public static void ResetActionToDefault(string actionName)
		{
			if (PlayerControlsSave.Instance != null)
			{
				PlayerControlsSave.Instance.ResetActionToDefault(actionName);
				Debug.Log($"[CONTROLS] Reset '{actionName}' to default bindings");
			}
		}


		/// <summary>
		/// Reset all actions to default bindings.
		/// </summary>
		public static void ResetAllToDefaults()
		{
			if (PlayerControlsSave.Instance != null)
			{
				PlayerControlsSave.Instance.ResetAllToDefaults();
				Debug.Log("[CONTROLS] Reset all actions to default bindings");
			}
		}


		#endregion
	}
}
