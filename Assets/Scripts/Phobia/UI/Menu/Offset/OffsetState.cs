using Phobia.Audio;
using Phobia.Audio.Vis;
using Phobia.Gameplay;
using Phobia.Input;
using UnityEngine;

namespace Phobia.ui.Menu.Offset
{
    public class OffsetMenu : UIBase
    {
        private PhobiaVis visualizer;
        private PhobiaSound _mainMusic;
        private bool _visualizerInitialized = false;

        public override void Initialize(PlayState playStateRef = null)
        {
            base.Initialize(playStateRef);
        }

        public override void Create()
        {
            // Ensure initialization (canvas setup) before creating UI
            if (!isInitialized)
            {
                Initialize(PlayState.Instance);
            }
            base.Create();
            Debug.Log("[OFFSET] Creating simple offset menu");

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

            // Create visualizer with proper parent (the UI canvas)
            CreateVisualizer();
        }

        private void CreateVisualizer()
        {
            // Create visualizer as a child of the UIBase's Canvas
            if (uiCanvas != null)
            {
                visualizer = PhobiaVis.CreateOnCanvas(uiCanvas, null);
            }
            else
            {
				Debug.Log("canvas null");
            }

            if (visualizer != null)
            {
                // Configure the visualizer
                visualizer.SetSound(_mainMusic);

                // Initialize the visualizer with its config
                visualizer.Initialize(visualizer.Config);

                // Set up RectTransform for proper UI placement

                _visualizerInitialized = true;
                Debug.Log("[OFFSET] Visualizer created and initialized successfully");
            }
            else
            {
                Debug.LogError("[OFFSET] Failed to create visualizer");
            }
        }

        // USE STANDARD UPDATE:
        public override void Update()
        {
            base.Update();

            // Ensure we have the music reference
            if (_mainMusic == null && PlayState.Instance != null)
            {
                _mainMusic = PlayState.Instance.heartBeatMusic;

                // If we now have music but no visualizer, create it
                if (_mainMusic != null && !_visualizerInitialized)
                {
                    CreateVisualizer();
                }
            }

            if (visualizer == null || !_visualizerInitialized) { return; }

            if (Controls.ACCEPT)
            {
                visualizer.pop();
            }
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Clean up visualizer
            if (visualizer != null)
            {
                Destroy(visualizer.gameObject);
            }
        }
    }
}
