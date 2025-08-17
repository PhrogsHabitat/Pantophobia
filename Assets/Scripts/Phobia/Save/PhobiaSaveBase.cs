using System;
using System.IO;

namespace Phobia.Save
{
    /// <summary>
    /// Base save system for Pantophobia
    /// Generic base class that can be extended for specialized save modules.
    /// Uses Unity's JsonUtility for maximum compatibility and simplicity.
    ///
    /// Usage:
    /// - Extend this for specialized saves: PhobiaControls, PhobiaPlayerPrefs, etc.
    /// - Override GetDefaultData() to provide default values
    /// - Call SaveShit() after making changes
    /// - Access data via the Data property
    /// </summary>
    /// <typeparam name="T">The data structure type to save/load</typeparam>
    public abstract class PhobiaSaveBase<T> where T : class, new()
    {
        #region Constants & Fields

        protected const string SAVE_VERSION = "1.0.0";
        protected const string SAVE_FOLDER = "PhobiaSaves";

        protected T _data;
        protected string _fileName;
        protected bool _isDirty = false;

        #endregion

        #region Properties

        /// <summary>
        /// The current save data. Modify this and call SaveShit() to persist changes.
        /// </summary>
        public T Data
        {
            get => _data;
            protected set
            {
                _data = value;
                _isDirty = true;
            }
        }

        /// <summary>
        /// Full path to the save file
        /// </summary>
        protected string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FOLDER, _fileName);

        /// <summary>
        /// Whether the data has been modified since last save
        /// </summary>
        public bool IsDirty => _isDirty;

        #endregion

        #region Constructor

        protected PhobiaSaveBase(string fileName)
        {
            _fileName = fileName;
            LoadShit();
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Override this to provide default data when no save exists
        /// </summary>
        protected abstract T GetDefaultData();

        /// <summary>
        /// Override this to perform validation on loaded data
        /// </summary>
        protected virtual bool ValidateData(T data) => data != null;

        /// <summary>
        /// Override this to perform post-load initialization
        /// </summary>
        protected virtual void OnDataLoaded() { }

        /// <summary>
        /// Override this to perform pre-save operations
        /// </summary>
        protected virtual void OnBeforeSave() { }

        #endregion

        #region Core Save/Load - The Important Shit

        /// <summary>
        /// Load data from disk. Creates default if none exists.
        /// FNF-style loading with Unity JsonUtility.
        /// </summary>
        protected virtual void LoadShit()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string jsonData = File.ReadAllText(SavePath);

                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        T loadedData = JsonUtility.FromJson<T>(jsonData);

                        if (ValidateData(loadedData))
                        {
                            _data = loadedData;
                            _isDirty = false;
                            OnDataLoaded();
                            Debug.Log($"[SAVE] Loaded {typeof(T).Name} from {_fileName}");
                            return;
                        }
                    }
                }

                // No valid save found, create default
                CreateDefaultShit();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SAVE] Error loading {_fileName}: {e.Message}");
                CreateDefaultShit();
            }
        }

        /// <summary>
        /// Save current data to disk. Call this after making changes!
        /// FNF-style saving with Unity JsonUtility.
        /// </summary>
        public virtual void SaveShit()
        {
            try
            {
                OnBeforeSave();

                string jsonData = JsonUtility.ToJson(_data, true);
                string saveDir = Path.GetDirectoryName(SavePath);

                // Ensure directory exists
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                File.WriteAllText(SavePath, jsonData);
                _isDirty = false;

                Debug.Log($"[SAVE] Saved {typeof(T).Name} to {_fileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SAVE] Failed to save {_fileName}: {e.Message}");
            }
        }

        /// <summary>
        /// Create default data and save it
        /// </summary>
        protected virtual void CreateDefaultShit()
        {
            _data = GetDefaultData();
            _isDirty = true;
            OnDataLoaded();
            SaveShit();
            Debug.Log($"[SAVE] Created default {typeof(T).Name} data");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Force reload data from disk
        /// </summary>
        public virtual void ReloadShit()
        {
            LoadShit();
        }

        /// <summary>
        /// Check if save file exists
        /// </summary>
        public virtual bool SaveExists()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Delete the save file (dangerous!)
        /// </summary>
        public virtual void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log($"[SAVE] Deleted save file: {_fileName}");
                }
                CreateDefaultShit();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SAVE] Failed to delete save {_fileName}: {e.Message}");
            }
        }

        /// <summary>
        /// Get save file info for debugging
        /// </summary>
        public virtual string GetSaveInfo()
        {
            if (!SaveExists())
            {
                return $"{_fileName}: No save file";
            }

            var fileInfo = new FileInfo(SavePath);
            return $"{_fileName}: {fileInfo.Length} bytes, modified {fileInfo.LastWriteTime}";
        }

        #endregion

        #region Auto-Save Support

        /// <summary>
        /// Enable auto-save when data is modified
        /// </summary>
        protected bool _autoSave = false;

        public virtual void EnableAutoSave(bool enable = true)
        {
            _autoSave = enable;
            if (enable && _isDirty)
            {
                SaveShit();
            }
        }

        /// <summary>
        /// Mark data as dirty and auto-save if enabled
        /// </summary>
        protected virtual void MarkDirty()
        {
            _isDirty = true;
            if (_autoSave)
            {
                SaveShit();
            }
        }

        #endregion
    }
}
