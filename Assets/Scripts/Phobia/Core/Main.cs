using UnityEngine;
using Phobia.Input;
using Phobia.Gameplay;
using Phobia.RegistryShit;

namespace Phobia.Core
{
    public static class Main
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            Debug.Log("[MAIN] Starting initialization...");

            // Registries auto-initialize via attribute

            Controls.Initialize();
            Debug.Log($"[MAIN] Controls.IsReady: {Controls.IsReady}");

            EnsurePlayState();
            Debug.Log("[MAIN] Initialization complete");
        }

    private static void EnsurePlayState()
        {
            if (Object.FindFirstObjectByType<PlayState>() != null)
            {
                return;
            }

            var go = new GameObject("PlayState");
            var playState = go.AddComponent<PlayState>();

            // Load initial scene
            var initialState = Constants.INIT_STATE;
            playState.LoadScene(initialState);
        }
    }
}
