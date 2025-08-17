using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Phobia.Save
{
    /// <summary>
    /// Base class for Pantophobia save systems - Unity-native implementation.
    /// Provides core save/load functionality that can be extended for specialized save types.
    /// Uses Unity's JsonUtility for maximum compatibility and simplicity.
    /// </summary>
    public abstract class PhobiaSaveBase<T> where T : class, new()
    {
        #region Core Save Infrastructure

        protected string fileName;
        protected string saveVersion;

        protected PhobiaSaveBase(string fileName, string version = "1.0.0")
        {
            this.fileName = fileName;
            this.saveVersion = version;
        }

        /// <summary>
        /// Get the full path where save data should be stored.
        /// </summary>
        protected string GetSavePath()
        {
            return System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }

        /// <summary>
        /// Load JSON data from disk. Override this in derived classes.
        /// </summary>
        protected abstract object LoadDataFromDisk();

        /// <summary>
        /// Save data to disk using Unity's JsonUtility. Flush method for immediate saving.
        /// </summary>
        protected virtual void SaveShit()
        {
            try
            {
                var data = GetDataForSaving();
                string jsonData = JsonUtility.ToJson(data, true);
                System.IO.File.WriteAllText(GetSavePath(), jsonData);
                Debug.Log($"[SAVE] Data flushed to file: {fileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SAVE] Failed to flush data: {e.Message}");
            }
        }

        /// <summary>
        /// Get the data object that should be saved. Override in derived classes.
        /// </summary>
        protected abstract object GetDataForSaving();

        /// <summary>
        /// Clear save file from disk. Nuclear option!
        /// </summary>
        public virtual void DeleteSave()
        {
            try
            {
                if (System.IO.File.Exists(GetSavePath()))
                {
                    System.IO.File.Delete(GetSavePath());
                    Debug.Log($"[SAVE] Cleared save data from file: {fileName}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SAVE] Error clearing save data: {e.Message}");
            }
        }

        /// <summary>
        /// Check if save file exists on disk.
        /// </summary>
        public bool SaveFileExists()
        {
            return System.IO.File.Exists(GetSavePath());
        }

        /// <summary>
        /// Load the save data from disk or create new if none exists.
        /// </summary>
        public T Load()
        {
            if (SaveFileExists())
            {
                try
                {
                    string jsonData = System.IO.File.ReadAllText(GetSavePath());
                    T data = JsonUtility.FromJson<T>(jsonData);
                    if (ValidateData(data))
                    {
                        return data;
                    }
                    else
                    {
                        Debug.LogWarning("[SAVE] Loaded data is invalid, creating new data.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SAVE] Failed to load save data: {e.Message}");
                }
            }

            // Return new instance if no valid save data is found
            return new T();
        }

        /// <summary>
        /// Override this method to provide default data for the save file.
        /// </summary>
        protected abstract T GetDefaultData();

        /// <summary>
        /// Override this method to validate loaded data.
        /// </summary>
        protected abstract bool ValidateData(T data);

        /// <summary>
        /// Save the current instance to disk.
        /// </summary>
        public void Save()
        {
            SaveShit();
        }

        #endregion
    }

    /// <summary>
    /// Main Pantophobia save system - handles game progress, settings, and player data.
    /// Extends PhobiaSaveBase for core functionality while providing game-specific features.
    /// </summary>
    public class PhobiaSave : PhobiaSaveBase<PhobiaSave.SaveData>
    {
        #region Constants & Static Fields

        private const string SAVE_FILE_NAME = "PhobiaSave.json";
        private const string SAVE_VERSION = "1.0.0";

        // Singleton instance
        private static PhobiaSave _instance;
        /// <summary>
        /// Singleton instance for accessing save data.
        /// </summary>
        public static PhobiaSave Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PhobiaSave();
                }
                return _instance;
            }
        }

        #endregion

        #region Data Structures

        [Serializable]
        public class SaveData
        {
            public string version = SAVE_VERSION;
            public long lastSaved = 0;
            public ProgressData progress = new ProgressData();
            public OptionsData options = new OptionsData();
            public PlayerData player = new PlayerData();

            public static SaveData CreateDefault()
            {
                var saveData = new SaveData();
                saveData.lastSaved = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return saveData;
            }
        }

        [Serializable]
        public class ProgressData
        {
            public List<string> completedLevels = new List<string>();
            public List<string> unlockedLevels = new List<string> { "testLevel" };
            public string currentLevel = "testLevel";
            public Dictionary<string, LevelScore> levelScores = new Dictionary<string, LevelScore>();
            public int totalPlayTime = 0;
            public int totalDeaths = 0;
            public int totalLevelsCompleted = 0;

            public bool IsLevelCompleted(string levelId) => completedLevels.Contains(levelId);
            public bool IsLevelUnlocked(string levelId) => unlockedLevels.Contains(levelId);

            public void CompleteLevel(string levelId)
            {
                if (!completedLevels.Contains(levelId))
                {
                    completedLevels.Add(levelId);
                    totalLevelsCompleted++;
                }
            }

            public void UnlockLevel(string levelId)
            {
                if (!unlockedLevels.Contains(levelId))
                {
                    unlockedLevels.Add(levelId);
                }
            }
        }

        [Serializable]
        public class LevelScore
        {
            public int score = 0;
            public float completionTime = 0f;
            public int deaths = 0;
            public bool perfectRun = false;
            public long achievedAt = 0;

            public LevelScore() { }

            public LevelScore(int score, float time, int deaths, bool perfect = false)
            {
                this.score = score;
                this.completionTime = time;
                this.deaths = deaths;
                this.perfectRun = perfect;
                this.achievedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        [Serializable]
        public class OptionsData
        {
            public float masterVolume = 1.0f;
            public float musicVolume = 0.8f;
            public float sfxVolume = 0.9f;
            public bool muteAudio = false;
            public bool fullscreen = true;
            public int targetFramerate = 60;
            public bool vsync = true;
            public int qualityLevel = 2;
            public InputSettings inputSettings = new InputSettings();
            public bool showFPS = false;
            public bool enableDebugMode = false;
            public float cameraShakeIntensity = 1.0f;
        }

        [Serializable]
        public class InputSettings
        {
            public string controlScheme = "PhobiaKeyboard";
            public bool enableBuffering = true;
            public float bufferSeconds = 0.15f;
            public bool enableDeadzone = true;
            public float deadzoneThreshold = 0.2f;
            public bool lockAndHideCursor = false;
            public Dictionary<string, string> keyBindings = new Dictionary<string, string>();

            public static InputSettings CreateDefault()
            {
                var settings = new InputSettings();
                settings.keyBindings["UI_UP"] = "<Keyboard>/w";
                settings.keyBindings["UI_DOWN"] = "<Keyboard>/s";
                settings.keyBindings["UI_LEFT"] = "<Keyboard>/a";
                settings.keyBindings["UI_RIGHT"] = "<Keyboard>/d";
                settings.keyBindings["UI_ACCEPT"] = "<Keyboard>/enter";
                settings.keyBindings["UI_BACK"] = "<Keyboard>/escape";
                return settings;
            }
        }

        [Serializable]
        public class PlayerData
        {
            public string playerName = "Player";
            public int playerLevel = 1;
            public int experience = 0;
            public List<string> favoriteLevels = new List<string>();
            public string lastPlayedLevel = "testLevel";
            public bool firstTimePlaying = true;
            public List<string> unlockedAchievements = new List<string>();

            public void AddFavoriteLevel(string levelId)
            {
                if (!favoriteLevels.Contains(levelId))
                {
                    favoriteLevels.Add(levelId);
                }
            }

            public void RemoveFavoriteLevel(string levelId) => favoriteLevels.Remove(levelId);
            public bool IsFavoriteLevel(string levelId) => favoriteLevels.Contains(levelId);

            public void UnlockAchievement(string achievementId)
            {
                if (!unlockedAchievements.Contains(achievementId))
                {
                    unlockedAchievements.Add(achievementId);
                }
            }

            public bool HasAchievement(string achievementId) => unlockedAchievements.Contains(achievementId);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The current save data. Modify and call SaveToDisk() to persist changes.
        /// </summary>
        public SaveData Data => base.Data;

        /// <summary>
        /// Quick access to progress data.
        /// </summary>
        public ProgressData Progress => Data.progress;
        /// <summary>
        /// Quick access to options data.
        /// </summary>
        public OptionsData Options => Data.options;
        /// <summary>
        /// Quick access to player data.
        /// </summary>
        public PlayerData Player => Data.player;

        #endregion

        #region Constructor

        private PhobiaSave() : base(SAVE_FILE_NAME) { }

        #endregion

        #region PhobiaSaveBase Implementation

        /// <summary>
        /// Provide default save data when no save exists.
        /// </summary>
        protected override SaveData GetDefaultData()
        {
            return SaveData.CreateDefault();
        }

        /// <summary>
        /// Validate loaded save data.
        /// </summary>
        protected override bool ValidateData(SaveData data)
        {
            return data != null && data.progress != null && data.options != null && data.player != null;
        }

        /// <summary>
        /// Load JSON data from disk. Override this in derived classes.
        /// </summary>
        protected override object LoadDataFromDisk()
        {
            try
            {
                if (SaveFileExists())
                {
                    string jsonData = System.IO.File.ReadAllText(GetSavePath());
                    return JsonUtility.FromJson<SaveData>(jsonData);
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SAVE] Error loading data: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Override base class method to provide our save data.
        /// </summary>
        protected override object GetDataForSaving()
        {
            return Data;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Save current data to disk. Call this after making changes!
        /// </summary>
        public void SaveToDisk()
        {
            Data.lastSaved = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SaveShit();
        }

        /// <summary>
        /// Clear all save data and create fresh save. Nuclear option!
        /// </summary>
        public static void ClearAllData()
        {
            Debug.Log("[SAVE] Clearing all save data...");

            try
            {
                if (_instance != null)
                {
                    _instance.DeleteSave();
                }
                _instance = new PhobiaSave();
                Debug.Log("[SAVE] All save data cleared successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SAVE] Error clearing save data: {e.Message}");
            }
        }

        #endregion

        #region Level Progress Methods

        /// <summary>
        /// Mark a level as completed and update score.
        /// </summary>
        public void CompleteLevel(string levelId, int score = 0, float time = 0f, int deaths = 0)
        {
            Progress.CompleteLevel(levelId);
            if (Progress.levelScores.ContainsKey(levelId))
            {
                var existingScore = Progress.levelScores[levelId];
                if (score > existingScore.score || (score == existingScore.score && deaths < existingScore.deaths))
                {
                    Progress.levelScores[levelId] = new LevelScore(score, time, deaths, deaths == 0);
                }
            }
            else
            {
                Progress.levelScores[levelId] = new LevelScore(score, time, deaths, deaths == 0);
            }
            Player.lastPlayedLevel = levelId;
            SaveToDisk();
            Debug.Log($"[SAVE] Level completed: {levelId} (Score: {score}, Deaths: {deaths})");
        }

        /// <summary>
        /// Unlock a level.
        /// </summary>
        public void UnlockLevel(string levelId)
        {
            Progress.UnlockLevel(levelId);
            SaveToDisk();
            Debug.Log($"[SAVE] Level unlocked: {levelId}");
        }

        /// <summary>
        /// Get score data for a level.
        /// </summary>
        public LevelScore GetLevelScore(string levelId)
        {
            return Progress.levelScores.ContainsKey(levelId) ? Progress.levelScores[levelId] : null;
        }

        #endregion

        #region Settings Methods

        /// <summary>
        /// Apply saved audio settings to Unity systems.
        /// </summary>
        public void ApplyAudioSettings()
        {
            AudioListener.volume = Options.masterVolume;
            // Additional audio settings can be applied here
        }

        /// <summary>
        /// Apply saved graphics settings to Unity systems.
        /// </summary>
        public void ApplyGraphicsSettings()
        {
            Screen.fullScreen = Options.fullscreen;
            Application.targetFrameRate = Options.targetFramerate;
            QualitySettings.vSyncCount = Options.vsync ? 1 : 0;
            QualitySettings.SetQualityLevel(Options.qualityLevel);
        }

        #endregion

        #region Integration Methods

        /// <summary>
        /// Apply saved input settings to PhobiaInput system.
        /// </summary>
        public void ApplyInputSettings(PhobiaInput inputSystem = null)
        {
            if (inputSystem == null)
            {
                inputSystem = PhobiaInput.Instance;
            }

            if (inputSystem == null)
            {
                Debug.LogWarning("[SAVE] PhobiaInput instance not found");
                return;
            }

            var config = inputSystem.Config;
            config.controlScheme = Options.inputSettings.controlScheme;
            config.enableBuffering = Options.inputSettings.enableBuffering;
            config.bufferSeconds = Options.inputSettings.bufferSeconds;
            config.enableDeadzone = Options.inputSettings.enableDeadzone;
            config.deadzoneThreshold = Options.inputSettings.deadzoneThreshold;
            config.lockAndHideCursor = Options.inputSettings.lockAndHideCursor;

            Debug.Log("[SAVE] Input settings applied from save data");
        }

        /// <summary>
        /// Save current input settings to save data.
        /// </summary>
        public void SaveInputSettings(PhobiaInput inputSystem = null)
        {
            if (inputSystem == null)
            {
                inputSystem = PhobiaInput.Instance;
            }

            if (inputSystem == null)
            {
                Debug.LogWarning("[SAVE] PhobiaInput instance not found");
                return;
            }

            var config = inputSystem.Config;
            Options.inputSettings.controlScheme = config.controlScheme;
            Options.inputSettings.enableBuffering = config.enableBuffering;
            Options.inputSettings.bufferSeconds = config.bufferSeconds;
            Options.inputSettings.enableDeadzone = config.enableDeadzone;
            Options.inputSettings.deadzoneThreshold = config.deadzoneThreshold;
            Options.inputSettings.lockAndHideCursor = config.lockAndHideCursor;

            SaveToDisk();
            Debug.Log("[SAVE] Input settings saved to save data");
        }

        /// <summary>
        /// Initialize PlayState with saved data.
        /// </summary>
        public void InitializePlayState(PlayState playState)
        {
            if (playState == null)
            {
                Debug.LogWarning("[SAVE] PlayState is null");
                return;
            }

            if (!string.IsNullOrEmpty(Player.lastPlayedLevel))
            {
                playState.currentLevel = Player.lastPlayedLevel;
            }

            Debug.Log($"[SAVE] PlayState initialized with level: {playState.currentLevel}");
        }

        /// <summary>
        /// Called when a level is completed in PlayState.
        /// </summary>
        public void OnLevelCompleted(string levelId, int score = 0, float completionTime = 0f, int deaths = 0)
        {
            CompleteLevel(levelId, score, completionTime, deaths);

            Progress.totalPlayTime += Mathf.RoundToInt(completionTime);
            Progress.totalDeaths += deaths;

            if (Player.firstTimePlaying)
            {
                Player.firstTimePlaying = false;
            }

            SaveToDisk();
            Debug.Log($"[SAVE] Level completion recorded: {levelId}");
        }

        /// <summary>
        /// Apply all saved settings to Unity systems.
        /// </summary>
        public void ApplyAllSettings()
        {
            ApplyAudioSettings();
            ApplyGraphicsSettings();
            ApplyInputSettings();
            Debug.Log("[SAVE] All settings applied from save data");
        }

        /// <summary>
        /// Save current Unity settings to save data.
        /// </summary>
        public void SaveCurrentSettings()
        {
            Options.fullscreen = Screen.fullScreen;
            Options.targetFramerate = Application.targetFrameRate;
            Options.vsync = QualitySettings.vSyncCount > 0;
            Options.qualityLevel = QualitySettings.GetQualityLevel();
            Options.masterVolume = AudioListener.volume;

            SaveToDisk();
            Debug.Log("[SAVE] Current settings saved to save data");
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Dump current save data to the Unity console (Editor only).
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugDumpSave()
        {
            if (Application.isEditor)
            {
                string jsonData = JsonUtility.ToJson(Data, true);
                Debug.Log($"[SAVE DEBUG] Current save data:\n{jsonData}");
            }
            else
            {
                Debug.LogWarning("[SAVE DEBUG] DebugDumpSave is only available in the Unity Editor.");
            }
        }

        /// <summary>
        /// Unlock all levels for testing (Editor only).
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugUnlockAllLevels()
        {
            if (Application.isEditor)
            {
                Progress.unlockedLevels.Clear();
                Progress.unlockedLevels.AddRange(new string[] { "testLevel", "offsetMenu" });
                SaveToDisk();
                Debug.Log("[SAVE DEBUG] All levels unlocked for testing");
            }
            else
            {
                Debug.LogWarning("[SAVE DEBUG] DebugUnlockAllLevels is only available in the Unity Editor.");
            }
        }

        #endregion
    }
}
