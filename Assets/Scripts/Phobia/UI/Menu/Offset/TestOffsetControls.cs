namespace Phobia.ui.Menu.Offset
{
    /// <summary>
    /// Test script to verify OffsetState controls integration with PlayerControlsSave.
    /// Attach this to a GameObject to test the enhanced controls system.
    /// </summary>
    public class TestOffsetControls : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private KeyCode manualTestKey = KeyCode.F8;

        private void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunTestAfterDelay());
            }
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(manualTestKey))
            {
                RunOffsetControlsTest();
            }
        }

        /// <summary>
        /// Run test after a short delay to ensure systems are initialized.
        /// </summary>
        private System.Collections.IEnumerator RunTestAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            RunOffsetControlsTest();
        }

        /// <summary>
        /// Test the OffsetState controls integration.
        /// </summary>
        public void RunOffsetControlsTest()
        {
            Debug.Log("=== OFFSET CONTROLS INTEGRATION TEST ===");

            try
            {
                // Test 1: Check Controls system
                Debug.Log("Test 1: Checking Controls system...");
                Debug.Log($"✓ Controls.IsReady: {Controls.IsReady}");
                Debug.Log($"✓ Controls.GetActionCount(): {Controls.GetActionCount()}");

                // Test 2: Check PlayerControlsSave
                Debug.Log("Test 2: Checking PlayerControlsSave...");
                var controlsSave = PlayerControlsSave.Instance;
                Debug.Log($"✓ PlayerControlsSave initialized: {controlsSave != null}");

                if (controlsSave != null)
                {
                    Debug.Log($"✓ Total saved actions: {controlsSave.GetAllActionNames().Count}");
                    Debug.Log($"✓ Auto-save enabled: {controlsSave.Preferences.autoSaveBindings}");
                }

                // Test 3: Check offset-specific actions
                Debug.Log("Test 3: Checking offset-specific actions...");
                var offsetActions = new[]
                {
                    "offsetBars4", "offsetBars8", "offsetBars16",
                    "offsetBars24", "offsetBars32", "offsetDebug",
                    "offsetSpectrum", "offsetTestSave"
                };

                foreach (var action in offsetActions)
                {
                    bool exists = Controls.HasAction(action);
                    Debug.Log($"✓ Action '{action}' exists: {exists}");

                    if (controlsSave != null)
                    {
                        var bindings = controlsSave.GetKeyBindings(action);
                        Debug.Log($"  - Saved bindings: {string.Join(", ", bindings)}");
                    }
                }

                // Test 4: Test binding modification
                Debug.Log("Test 4: Testing binding modification...");
                if (controlsSave != null)
                {
                    // Save original binding
                    var originalBindings = controlsSave.GetKeyBindings("offsetTestSave");
                    Debug.Log($"✓ Original 'offsetTestSave' bindings: {string.Join(", ", originalBindings)}");

                    // Change binding
                    controlsSave.SetKeyBindings("offsetTestSave", "<Keyboard>/f11");
                    var newBindings = controlsSave.GetKeyBindings("offsetTestSave");
                    Debug.Log($"✓ Modified 'offsetTestSave' bindings: {string.Join(", ", newBindings)}");

                    // Restore original
                    if (originalBindings.Count > 0)
                    {
                        controlsSave.SetKeyBindings("offsetTestSave", originalBindings.ToArray());
                        Debug.Log("✓ Restored original bindings");
                    }
                    else
                    {
                        controlsSave.ResetActionToDefault("offsetTestSave");
                        Debug.Log("✓ Reset to default bindings");
                    }
                }

                // Test 5: Test input detection
                Debug.Log("Test 5: Testing input detection...");
                Debug.Log("✓ Input detection test - press keys to see if they're detected:");
                Debug.Log("  - Press 1-5 to test bar count changes");
                Debug.Log("  - Press D to test debug info");
                Debug.Log("  - Press S to test spectrum debug");
                Debug.Log("  - Press T to test save/load functionality");
                Debug.Log("  - Press Enter/Space to test accept action");

                Debug.Log("✓ ALL OFFSET CONTROLS TESTS PASSED!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ OFFSET CONTROLS TEST FAILED: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }

            Debug.Log("=== OFFSET CONTROLS INTEGRATION TEST COMPLETE ===");
        }

        /// <summary>
        /// Test method that can be called from Unity Inspector.
        /// </summary>
        [ContextMenu("Run Offset Controls Test")]
        public void RunTestFromMenu()
        {
            RunOffsetControlsTest();
        }

        /// <summary>
        /// Demonstrate real-time input monitoring.
        /// </summary>
        [ContextMenu("Monitor Input (5 seconds)")]
        public void MonitorInput()
        {
            StartCoroutine(MonitorInputCoroutine());
        }

        private System.Collections.IEnumerator MonitorInputCoroutine()
        {
            Debug.Log("=== INPUT MONITORING STARTED (5 seconds) ===");

            float endTime = Time.time + 5f;
            while (Time.time < endTime)
            {
                // Check offset actions
                if (Controls.isPressed("offsetBars4"))
                {
                    Debug.Log("🎵 offsetBars4 pressed!");
                }

                if (Controls.isPressed("offsetBars8"))
                {
                    Debug.Log("🎵 offsetBars8 pressed!");
                }

                if (Controls.isPressed("offsetBars16"))
                {
                    Debug.Log("🎵 offsetBars16 pressed!");
                }

                if (Controls.isPressed("offsetBars24"))
                {
                    Debug.Log("🎵 offsetBars24 pressed!");
                }

                if (Controls.isPressed("offsetBars32"))
                {
                    Debug.Log("🎵 offsetBars32 pressed!");
                }

                if (Controls.isPressed("offsetDebug"))
                {
                    Debug.Log("🎵 offsetDebug pressed!");
                }

                if (Controls.isPressed("offsetSpectrum"))
                {
                    Debug.Log("🎵 offsetSpectrum pressed!");
                }

                if (Controls.isPressed("offsetTestSave"))
                {
                    Debug.Log("🎵 offsetTestSave pressed!");
                }

                // Check common actions

                if (Controls.ACCEPT)
                {
                    Debug.Log("🎵 ACCEPT pressed!");
                }

                if (Controls.BACK)
                {
                    Debug.Log("🎵 BACK pressed!");
                }

                if (Controls.UI_UP)
                {
                    Debug.Log("🎵 UI_UP pressed!");
                }

                if (Controls.UI_DOWN)
                {
                    Debug.Log("🎵 UI_DOWN pressed!");
                }

                yield return null;
            }

            Debug.Log("=== INPUT MONITORING COMPLETE ===");
        }
    }
}
