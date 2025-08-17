using UnityEngine;

namespace Phobia.ui.Menu.Offset
{
    public class OffsetMenu : UIBase, IPlayStateInitializable
    {
        private PhobiaVis visualizer;
        private PhobiaSound _mainMusic;
        private PlayerControlsSave _controlsSave;

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

            // Initialize the save system
            _controlsSave = PlayerControlsSave.Instance;
            Debug.Log("[OFFSET] PlayerControlsSave initialized");

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

        private void Update()
        {
            if (visualizer == null)
            {
                return;
            }

            // Simple input handling using Controls

            if (Controls.ACCEPT)
            {
                visualizer.pop();
            }



            // Test bar count changes using Controls actions
            if (Controls.isPressed("offsetBars4"))
            {
                visualizer.SetBarCount(4);
                Debug.Log("[OFFSET] Set to 4 bars");
            }
            if (Controls.isPressed("offsetBars8"))
            {
                visualizer.SetBarCount(8);
                Debug.Log("[OFFSET] Set to 8 bars");
            }
            if (Controls.isPressed("offsetBars16"))
            {
                visualizer.SetBarCount(16);
                Debug.Log("[OFFSET] Set to 16 bars");
            }
            if (Controls.isPressed("offsetBars24"))
            {
                visualizer.SetBarCount(24);
                Debug.Log("[OFFSET] Set to 24 bars");
            }
            if (Controls.isPressed("offsetBars32"))
            {
                visualizer.SetBarCount(32);
                Debug.Log("[OFFSET] Set to 32 bars - classic audio visualizer style! 🎵");
            }

            // Debug key to check Unity standard visualizer setup using Controls
            if (Controls.isPressed("offsetDebug"))
            {
                Debug.Log("[OFFSET] Enhanced Unity Audio Visualizer Debug Info:");
                Debug.Log($"- Bar Count: {visualizer.Config.bandCount}");
                Debug.Log($"- Frequency Multiplier: {visualizer.Config.frequencyMultiplier}");
                Debug.Log($"- Sample Count: {visualizer.Config.sampleCount}");
                Debug.Log($"- Sample Rate: {AudioSettings.outputSampleRate} Hz");
                Debug.Log($"- Smoothing Speed: {visualizer.Config.smoothingSpeed}");

                Debug.Log("Enhanced features:");
                Debug.Log("- Peak detection with falloff for dynamic response");
                Debug.Log("- Logarithmic scaling for natural visual response");
                Debug.Log("- Subtle bounce effects for more life");
                Debug.Log("- Enhanced color blending with peak intensity");
                Debug.Log("- Improved frequency distribution for all bars");
                Debug.Log("- Now using unified Controls system with save support! 🎮");

                // Show save system integration info
                if (_controlsSave != null)
                {
                    Debug.Log("\nControls Save System Info:");
                    Debug.Log($"- PlayerControlsSave initialized: {_controlsSave != null}");
                    Debug.Log($"- Auto-save bindings: {_controlsSave.Preferences.autoSaveBindings}");
                    Debug.Log($"- Total saved actions: {_controlsSave.GetAllActionNames().Count}");

                    // Show current bindings for offset actions
                    var offsetActions = new[] { "offsetBars4", "offsetBars8", "offsetDebug", "offsetSpectrum" };
                    foreach (var action in offsetActions)
                    {
                        var bindings = _controlsSave.GetKeyBindings(action);
                        Debug.Log($"- {action}: {string.Join(", ", bindings)}");
                    }
                }
            }

            // Spectrum debug key - shows real-time values using Controls
            if (Controls.isPressed("offsetSpectrum"))
            {
                var debug = "[OFFSET] Real-time Spectrum Values:\n";
                // Note: This would need access to visualizer's internal arrays
                // For now, just show that the feature is available
                debug += "Press S to see real-time spectrum data\n";
                debug += "All bars should show activity during music playback!\n";
                debug += "Controls system managing all input! 🎵";
                Debug.Log(debug);
            }

            // Test save/load functionality
            if (Controls.isPressed("offsetTestSave"))
            {
                TestControlsSaveLoad();
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

            // Save the binding to PlayerControlsSave for persistence
            if (_controlsSave != null)
            {
                _controlsSave.SetKeyBindings(actionName, bindings);
                Debug.Log($"[OFFSET] Saved binding for '{actionName}': {string.Join(", ", bindings)}");
            }
        }

        /// <summary>
        /// Test method to demonstrate save/load functionality.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void TestControlsSaveLoad()
        {
            Debug.Log("=== OFFSET CONTROLS SAVE/LOAD TEST ===");

            if (_controlsSave == null)
            {
                Debug.LogError("[OFFSET] PlayerControlsSave not initialized");
                return;
            }

            // Test getting saved bindings
            var bars4Bindings = _controlsSave.GetKeyBindings("offsetBars4");
            Debug.Log($"[OFFSET] Saved bindings for 'offsetBars4': {string.Join(", ", bars4Bindings)}");

            // Test changing a binding
            Debug.Log("[OFFSET] Changing offsetBars4 binding to F1...");
            _controlsSave.SetKeyBindings("offsetBars4", "<Keyboard>/f1");

            // Verify the change
            var newBindings = _controlsSave.GetKeyBindings("offsetBars4");
            Debug.Log($"[OFFSET] New bindings for 'offsetBars4': {string.Join(", ", newBindings)}");

            // Reset to default
            Debug.Log("[OFFSET] Resetting to default...");
            _controlsSave.ResetActionToDefault("offsetBars4");

            var resetBindings = _controlsSave.GetKeyBindings("offsetBars4");
            Debug.Log($"[OFFSET] Reset bindings for 'offsetBars4': {string.Join(", ", resetBindings)}");

            Debug.Log("=== OFFSET CONTROLS SAVE/LOAD TEST COMPLETE ===");
        }
    }
}
