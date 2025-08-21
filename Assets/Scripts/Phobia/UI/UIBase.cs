using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Phobia.ui
{
    /// <summary>
    /// Base class for all UI components.
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        [HideInInspector] public Phobia.Gameplay.PlayState playState;
        protected Dictionary<string, GameObject> uiElements = new();
        protected Canvas uiCanvas;
        protected bool isInitialized;


        /// <summary>
        /// Initialize the UI component.
        /// </summary>
        public virtual void Initialize(Phobia.Gameplay.PlayState playStateRef = null)
        {
            playState = playStateRef;
            isInitialized = true;
            SetupCanvas();
        }

        /// <summary>
        /// Setup the Canvas component.
        /// </summary>
        protected virtual void SetupCanvas()
        {
            Debug.Log("[UIBase] Setting up Canvas...");

            // Ensure the GameObject is active
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[UIBase] GameObject is inactive. Activating it temporarily to add components.");
                gameObject.SetActive(true);
            }

            // Add or get the Canvas component
            uiCanvas = GetComponent<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogWarning("[UIBase] Canvas component missing. Creating a new GameObject for UI.");
                var uiObject = new GameObject("UI_Canvas");
                uiCanvas = uiObject.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = uiObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                uiObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                uiObject.transform.SetParent(transform, false);
                Debug.Log("[UIBase] New UI GameObject created and configured.");
            }
            else
            {
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                if (!TryGetComponent<UnityEngine.UI.CanvasScaler>(out _))
                {
                    var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    Debug.Log("[UIBase] CanvasScaler added.");
                }

                if (!TryGetComponent<UnityEngine.UI.GraphicRaycaster>(out _))
                {
                    gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    Debug.Log("[UIBase] GraphicRaycaster added.");
                }
            }
        }

		/// <summary>
		/// Create and setup UI elements.
		/// </summary>
		public virtual void Create() { }

		/// <summary>
		/// Reset the UI to initial state.
		/// </summary>
		public virtual void Reset() { }
		public virtual void Update() { }

        /// <summary>
        /// Add a UI element to management.
        /// </summary>
        protected void AddUIElement(GameObject element, string name)
        {
            if (element == null)
            {
                return;
            }

            uiElements[name] = element;
            element.transform.SetParent(transform);
        }

        /// <summary>
        /// Get a managed UI element.
        /// </summary>
        protected GameObject GetUIElement(string name)
        {
            return uiElements.TryGetValue(name, out var element) ? element : null;
        }

        /// <summary>
        /// Cleanup when destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            foreach (var element in uiElements.Values)
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }
            uiElements.Clear();
        }
    }
}
