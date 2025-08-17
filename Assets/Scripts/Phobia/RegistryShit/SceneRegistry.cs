using System;
using System.Collections.Generic;

namespace Phobia.RegistryShit
{
    /// <summary>
    /// Unified registry for all game states (levels, menus, UI screens)
    /// </summary>
    public static class SceneRegistry
    {
        public enum SceneType { Level, UI, System }

        private struct SceneInfo
        {
            public Type ComponentType;
            public SceneType Type;
        }

        private static readonly Dictionary<string, SceneInfo> _scenes = new Dictionary<string, SceneInfo>(StringComparer.OrdinalIgnoreCase);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            Debug.Log("[SCENE REGISTRY] Initializing...");

            // Register all scenes (could also load from config)
            RegisterScene("TestLevel", typeof(Gameplay.Components.Level.Levels.TestLevel), SceneType.Level);
            RegisterScene("OffsetMenu", typeof(ui.Menu.Offset.OffsetMenu), SceneType.UI);
            // Add more scenes here

            Debug.Log($"[SCENE REGISTRY] Registered {_scenes.Count} scenes");
        }

        public static void RegisterScene(string sceneId, Type componentType, SceneType sceneType)
        {
            if (_scenes.ContainsKey(sceneId))
            {
                Debug.LogWarning($"[SCENE REGISTRY] Overwriting existing scene: {sceneId}");
            }

            _scenes[sceneId] = new SceneInfo
            {
                ComponentType = componentType,
                Type = sceneType
            };
        }

        public static bool TryCreateScene(string sceneId, Gameplay.PlayState playState, out MonoBehaviour component)
        {
            component = null;

            if (!_scenes.TryGetValue(sceneId, out var info))
            {
                Debug.LogError($"[SCENE REGISTRY] Scene not registered: {sceneId}");
                return false;
            }

            var go = new GameObject($"{sceneId}_Scene");
            component = go.AddComponent(info.ComponentType) as MonoBehaviour;

            if (component is Gameplay.IPlayStateInitializable initializable)
            {
                initializable.Initialize(playState);
            }

            return true;
        }
    }
}
