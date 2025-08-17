using System.Collections.Generic;

namespace Phobia.Gameplay.Components.Level
{
    [System.Serializable]
    public class LevelData
    {
        public string levelId;
        public string displayName;
        public float defaultZoom;
        public Color bgColor;
        public string songId;
        public float bpm = 120;
        public int songSigNum = 4;
        public int songSigDenum = 4;
        public List<Conductor.TimeChange> timeChanges = new List<Conductor.TimeChange>();

        public static LevelData LoadLevelMetadata(string worldId)
        {
            string path = $"Data/{worldId}/metadata";
            TextAsset jsonFile = Resources.Load<TextAsset>(path);
            if (jsonFile == null)
            {
                Debug.LogError($"Level metadata not found: {path}");
                return null;
            }

            // Create a new instance to deserialize into
            var levelData = new LevelData();
            JsonUtility.FromJsonOverwrite(jsonFile.text, levelData);
            return levelData;
        }
    }
}
