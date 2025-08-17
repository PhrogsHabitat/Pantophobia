using UnityEngine;
using Controls = Phobia.Input.Controls;

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
            var save = PhobiaSave.Instance;
            if (save == null)
            {
                Debug.LogError("[MAIN] PhobiaSave.Instance is null. Attempting to load save data.");
                save = PhobiaSave.LoadSaveData();
            }
            Debug.Log($"[MAIN] PhobiaSave.Instance: {(save != null ? "Valid" : "Null")}");

            if (save.Player == null)
            {
                Debug.LogError("[MAIN] SaveData.Player is null. Initializing default player data.");
                save.Data.player = new PhobiaSave.PlayerData();
            }
            Debug.Log($"[MAIN] SaveData.Player: {(save.Player != null ? "Valid" : "Null")}");

            Controls.Initialize();
            Debug.Log($"[MAIN] Controls.IsReady: {Controls.IsReady}");

            EnsurePlayState(save);
            Debug.Log("[MAIN] Initialization complete");
        }

        private static void EnsurePlayState(PhobiaSave save)
        {
            if (Object.FindFirstObjectByType<PlayState>() != null)
            {
                return;
            }

            var go = new GameObject("PlayState");
            var playState = go.AddComponent<PlayState>();

            // Load initial scene
            var initialState = !string.IsNullOrEmpty(save.Player.lastPlayedLevel)
                ? save.Player.lastPlayedLevel
                : Constants.INIT_STATE;

            playState.LoadScene(initialState);
        }
    }
}
