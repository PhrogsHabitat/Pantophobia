using System;
using System.Collections.Generic;

namespace Phobia.Save
{
    /// <summary>
    /// Save system for player controls and input settings.
    /// Handles saving/loading of key bindings, input preferences, and control state.
    /// Extends PhobiaSaveBase for core functionality while providing controls-specific features.
    /// </summary>
    public class PlayerControlsSave : PhobiaSaveBase<PlayerControlsSave.ControlsData>
    {
        #region Constants & Static Fields

        private const string SAVE_FILE_NAME = "PlayerControls.json";

        // Singleton instance
        private static PlayerControlsSave _instance;
        public static PlayerControlsSave Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PlayerControlsSave();
                    _instance.LoadShit();
                }
                return _instance;
            }
        }

        #endregion

        #region Data Structures

        /// <summary>
        /// Data structure for controls save data.
        /// Contains key bindings, input settings, and control preferences.
        /// </summary>
        [Serializable]
        public class ControlsData
        {
            public string version = "1.0.0";
            public long lastSaved = 0;
            public Dictionary<string, List<string>> keyBindings = new Dictionary<string, List<string>>();
            public InputSettings inputSettings = new InputSettings();
            public ControlPreferences preferences = new ControlPreferences();

            /// <summary>
            /// Create default controls data with standard key bindings.
            /// </summary>
            public static ControlsData CreateDefault()
            {
                var data = new ControlsData();
                data.lastSaved = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Set up default key bindings
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

        /// <summary>
        /// Input system settings.
        /// </summary>
        [Serializable]
        public class InputSettings
        {
            public string controlScheme = "PhobiaKeyboard";
            public bool autoEnable = true;
            public bool dontDestroyOnLoad = true;
            public bool enableBuffering = true;
            public float bufferSeconds = 0.15f;
            public bool enableDeadzone = true;
            public float deadzoneThreshold = 0.2f;
            public bool lockAndHideCursor = false;
            public bool enableDebugLogging = false;
            public bool enableCaching = true;
            public int maxCacheSize = 100;
        }

        /// <summary>
        /// Control preferences and user settings.
        /// </summary>
        [Serializable]
        public class ControlPreferences
        {
            public bool autoSaveBindings = true;
            public bool showInputHints = true;
            public float inputSensitivity = 1.0f;
            public bool enableHapticFeedback = true;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private PlayerControlsSave() : base(SAVE_FILE_NAME)
        {
            Debug.Log("[CONTROLS_SAVE] PlayerControlsSave initialized");
        }

        #endregion

        #region PhobiaSaveBase Implementation

        /// <summary>
        /// Provide default data when no save exists.
        /// </summary>
        protected override ControlsData GetDefaultData()
        {
            return ControlsData.CreateDefault();
        }

        /// <summary>
        /// Validate loaded data.
        /// </summary>
        protected override bool ValidateData(ControlsData data)
        {
            return data != null && data.keyBindings != null && data.inputSettings != null;
        }

        /// <summary>
        /// Load data from disk (PlayerPrefs in this case).
        /// </summary>
        protected override void LoadShit()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_FILE_NAME))
                {
                    string jsonData = PlayerPrefs.GetString(SAVE_FILE_NAME);
                    _data = JsonUtility.FromJson<ControlsData>(jsonData);
                }
                else
                {
                    _data = GetDefaultData();
                }

                if (!ValidateData(_data))
                {
                    Debug.LogWarning("[CONTROLS_SAVE] Invalid data loaded. Resetting to defaults.");
                    _data = GetDefaultData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CONTROLS_SAVE] Error loading data: {e.Message}");
                _data = GetDefaultData();
            }
        }

        /// <summary>
        /// Flush data to disk (PlayerPrefs in this case).
        /// </summary>
        public override void SaveShit()
        {
            try
            {
                string jsonData = JsonUtility.ToJson(_data, true);
                PlayerPrefs.SetString(SAVE_FILE_NAME, jsonData);
                PlayerPrefs.Save();
                _isDirty = false;
                Debug.Log($"[CONTROLS_SAVE] Data saved to PlayerPrefs with key: {SAVE_FILE_NAME}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[CONTROLS_SAVE] Failed to save data: {e.Message}");
            }
        }

        /// <summary>
        /// Clear the save file (remove from PlayerPrefs).
        /// </summary>
        public void ClearSaveFile()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_FILE_NAME))
                {
                    PlayerPrefs.DeleteKey(SAVE_FILE_NAME);
                    PlayerPrefs.Save();
                    Debug.Log($"[CONTROLS_SAVE] Cleared save data from PlayerPrefs with key: {SAVE_FILE_NAME}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CONTROLS_SAVE] Error clearing save data: {e.Message}");
            }
        }

        /// <summary>
        /// Check if the save file exists.
        /// </summary>
        public bool SaveFileExists()
        {
            return PlayerPrefs.HasKey(SAVE_FILE_NAME);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get key bindings for an action.
        /// </summary>
        public List<string> GetKeyBindings(string actionName)
        {
            if (Data.keyBindings.ContainsKey(actionName))
            {
                return new List<string>(Data.keyBindings[actionName]);
            }
            return new List<string>();
        }

        /// <summary>
        /// Set key bindings for an action.
        /// </summary>
        public void SetKeyBindings(string actionName, params string[] bindings)
        {
            Data.keyBindings[actionName] = new List<string>(bindings);
            MarkDirty();

            if (Data.preferences.autoSaveBindings)
            {
                SaveShit();
            }

            Debug.Log($"[CONTROLS_SAVE] Set bindings for '{actionName}': {string.Join(", ", bindings)}");
        }

        /// <summary>
        /// Add a key binding to an existing action.
        /// </summary>
        public void AddKeyBinding(string actionName, string binding)
        {
            if (!Data.keyBindings.ContainsKey(actionName))
            {
                Data.keyBindings[actionName] = new List<string>();
            }

            if (!Data.keyBindings[actionName].Contains(binding))
            {
                Data.keyBindings[actionName].Add(binding);
                MarkDirty();

                if (Data.preferences.autoSaveBindings)
                {
                    SaveShit();
                }

                Debug.Log($"[CONTROLS_SAVE] Added binding '{binding}' to action '{actionName}'");
            }
        }

        /// <summary>
        /// Remove a key binding from an action.
        /// </summary>
        public void RemoveKeyBinding(string actionName, string binding)
        {
            if (Data.keyBindings.ContainsKey(actionName))
            {
                if (Data.keyBindings[actionName].Remove(binding))
                {
                    MarkDirty();

                    if (Data.preferences.autoSaveBindings)
                    {
                        SaveShit();
                    }

                    Debug.Log($"[CONTROLS_SAVE] Removed binding '{binding}' from action '{actionName}'");
                }
            }
        }

        /// <summary>
        /// Check if an action has any key bindings.
        /// </summary>
        public bool HasKeyBindings(string actionName)
        {
            return Data.keyBindings.ContainsKey(actionName) && Data.keyBindings[actionName].Count > 0;
        }

        /// <summary>
        /// Get all action names that have bindings.
        /// </summary>
        public List<string> GetAllActionNames()
        {
            return new List<string>(Data.keyBindings.Keys);
        }

        /// <summary>
        /// Reset an action to default bindings.
        /// </summary>
        public void ResetActionToDefault(string actionName)
        {
            var defaultData = ControlsData.CreateDefault();
            if (defaultData.keyBindings.ContainsKey(actionName))
            {
                Data.keyBindings[actionName] = new List<string>(defaultData.keyBindings[actionName]);
                MarkDirty();

                if (Data.preferences.autoSaveBindings)
                {
                    SaveShit();
                }

                Debug.Log($"[CONTROLS_SAVE] Reset action '{actionName}' to default bindings");
            }
        }

        /// <summary>
        /// Reset all bindings to defaults.
        /// </summary>
        public void ResetAllToDefaults()
        {
            var defaultData = ControlsData.CreateDefault();
            Data.keyBindings = defaultData.keyBindings;
            Data.inputSettings = defaultData.inputSettings;
            Data.preferences = defaultData.preferences;
            MarkDirty();
            SaveShit();

            Debug.Log("[CONTROLS_SAVE] Reset all controls to defaults");
        }

        /// <summary>
        /// Save controls data to disk.
        /// </summary>
        public void SaveControls()
        {
            SaveShit();
        }

        /// <summary>
        /// Ensure key bindings are properly managed for an action.
        /// </summary>
        public void EnsureKeyBindings(string actionName, params string[] bindings)
        {
            if (!Data.keyBindings.ContainsKey(actionName))
            {
                SetKeyBindings(actionName, bindings);
            }
        }

        #endregion

        #region Quick Access Properties

        /// <summary>
        /// Quick access to input settings.
        /// </summary>
        public InputSettings Settings => Data.inputSettings;

        /// <summary>
        /// Quick access to control preferences.
        /// </summary>
        public ControlPreferences Preferences => Data.preferences;

        #endregion
    }
}
