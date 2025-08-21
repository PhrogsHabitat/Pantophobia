using System;
using System.Collections.Generic;
using UnityEngine;

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

            RegisterScene("TestLevel", typeof(Gameplay.Components.Level.Levels.TestLevel), SceneType.Level);
            RegisterScene("OffsetMenu", typeof(ui.Menu.Offset.OffsetMenu), SceneType.UI);
			RegisterScene("InitState", typeof(ui.Menu.Init.InitState), SceneType.UI);
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

            // Create new GameObject
            var go = new GameObject($"{sceneId}_Scene");

            // Make it a child of PlayState for visibility
            go.transform.SetParent(playState.transform);

            // Ensure GameObject is active before adding component
            go.SetActive(true);

            component = go.AddComponent(info.ComponentType) as MonoBehaviour;

            // Ensure MonoBehaviour is enabled
            if (component != null)
            {
                component.enabled = true;
                Debug.Log($"[SceneRegistry] Added component: {component.GetType().Name}, enabled: {component.enabled}, GameObject active: {component.gameObject.activeSelf}");
            }
            else
            {
                Debug.LogError($"[SceneRegistry] Failed to add component of type: {info.ComponentType.Name}");
            }

            // Ensure it's visible in hierarchy
            Debug.Log($"[SceneRegistry] Created scene object: {go.name}", go);

            return true;
		}
    }
}
