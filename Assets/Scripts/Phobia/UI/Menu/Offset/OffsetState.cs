using Phobia.Audio;
using Phobia.Audio.Vis;
using Phobia.Gameplay;
using Phobia.Input;
using UnityEngine;

namespace Phobia.ui.Menu.Offset
{
    public class OffsetMenu : UIBase, IPlayStateInitializable
    {
        private PhobiaVis visualizer;
        private PhobiaSound _mainMusic;

        public override void CreateUI()
        {
            base.CreateUI();
            Debug.Log("[OFFSET] Creating simple offset menu");
        }

        public override void InitUISpecifics()
        {
            base.InitUISpecifics();
            // Check if Controls is ready

            if (!Controls.IsReady)
            {
                Debug.LogError("[OFFSET] Controls not available! Make sure Main.Initialize() was called.");
                return;
            }

            Debug.Log("[OFFSET] Controls system ready");

            // Add offset menu specific actions using Controls with save integration
            AddOrUpdateAction("offsetBars4", "<Keyboard>/1");
            AddOrUpdateAction("offsetBars8", "<Keyboard>/2");
            AddOrUpdateAction("offsetBars16", "<Keyboard>/3");
            AddOrUpdateAction("offsetBars24", "<Keyboard>/4");
            AddOrUpdateAction("offsetBars32", "<Keyboard>/5");
            AddOrUpdateAction("offsetDebug", "<Keyboard>/d");
            AddOrUpdateAction("offsetSpectrum", "<Keyboard>/s");
            AddOrUpdateAction("offsetTestSave", "<Keyboard>/t");

            Debug.Log("[OFFSET] Added offset menu input actions with save integration");

            // Get the music
            _mainMusic = PlayState.Instance != null ? PlayState.Instance.heartBeatMusic : null;

            if (_mainMusic == null)
            {
                Debug.LogWarning("[OFFSET] No main music available; visualizer will not be created.");
                return;
            }

            // Create visualizer - ONE LINE!
            visualizer = PhobiaVis.Create(_mainMusic);
        }

		// USE STANDARD UPDATE:
		public override void Update()
		{
			if (visualizer == null) { return; }

			if (Controls.ACCEPT)
			{
				visualizer.pop();
			}
			
        }

        /// <summary>
        /// Show the OffsetMenu UI.
        /// </summary>
        public override void ShowUI()
        {
            base.ShowUI();
            Debug.Log("[OFFSET] OffsetMenu activated.");
        }

        /// <summary>
        /// Hide the OffsetMenu UI.
        /// </summary>
        public override void HideUI()
        {
            base.HideUI();
            Debug.Log("[OFFSET] OffsetMenu deactivated.");
        }

        /// <summary>
        /// Helper method to add an action to both the Controls system and save it to PlayerControlsSave.
        /// </summary>
        private void AddOrUpdateAction(string actionName, params string[] bindings)
        {
            // Add to the runtime Controls system
            Controls.AddAction(actionName, bindings);
            // Save the binding for persistence
            Controls.SetKeyBindings(actionName, bindings);
            Debug.Log($"[OFFSET] Saved binding for '{actionName}': {string.Join(", ", bindings)}");
        }

        /// <summary>
        /// Test method to demonstrate save/load functionality.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void TestControlsSaveLoad()
        {
            Debug.Log("=== OFFSET CONTROLS SAVE/LOAD TEST ===");

            // Test getting saved bindings
            var bars4Bindings = Controls.GetKeyBindings("offsetBars4");
            Debug.Log($"[OFFSET] Saved bindings for 'offsetBars4': {string.Join(", ", bars4Bindings)}");

            // Test changing a binding
            Debug.Log("[OFFSET] Changing offsetBars4 binding to F1...");
            Controls.SetKeyBindings("offsetBars4", "<Keyboard>/f1");

            // Verify the change
            var newBindings = Controls.GetKeyBindings("offsetBars4");
            Debug.Log($"[OFFSET] New bindings for 'offsetBars4': {string.Join(", ", newBindings)}");

            // Reset to default
            Controls.ResetActionToDefault("offsetBars4");
            var resetBindings = Controls.GetKeyBindings("offsetBars4");
            Debug.Log($"[OFFSET] Reset bindings for 'offsetBars4': {string.Join(", ", resetBindings)}");

            Debug.Log("=== OFFSET CONTROLS SAVE/LOAD TEST COMPLETE ===");
        }
    }
}
