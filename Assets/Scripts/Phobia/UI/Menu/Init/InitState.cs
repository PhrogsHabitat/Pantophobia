using Phobia.Gameplay;
using UnityEngine;
using Phobia.Graphics;
using Phobia.Audio;
using Phobia.Input;
using Phobia.ui.Menu.Components;
using UnityEngine.UI;
using System.Collections;

namespace Phobia.ui.Menu.Init
{
    public class InitState : Phobia.ui.UIBase
    {
        private PromptBox warningPrompt;
        private bool isFirstTime;
        private bool promptShown;

        public override void Initialize(PlayState playStateRef = null)
        {
            base.Initialize(playStateRef);
            isFirstTime = !PlayerPrefs.HasKey("HasSeenFlashingLightsWarning");
            promptShown = false;
        }

        public override void Create()
        {
            if (!isInitialized)
            {
                Initialize(PlayState.Instance);
            }
            base.Create();

            if (isFirstTime)
            {
                ShowWarningPrompt();
            }
            else
            {
                ContinueToNextState();
            }
        }

        private void ShowWarningPrompt()
        {
            // Create a dedicated canvas for the prompt
            GameObject canvasObj = new GameObject("PromptCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();

            // Use the PhobiaCamera from PlayState
            var phobiaCamera = PlayState.Instance?.mainCamera;
            if (phobiaCamera != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = phobiaCamera;
                canvas.planeDistance = 100; // Increased for better visibility
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            canvas.sortingOrder = 100; // High sorting order to ensure it's on top

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Set the canvas to cover the entire screen
            var canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;

            // Create a parent object for the prompt to center it
            GameObject promptParent = new GameObject("PromptParent");
            promptParent.transform.SetParent(canvasObj.transform, false);
            var parentRect = promptParent.AddComponent<RectTransform>();
            parentRect.anchorMin = new Vector2(0.5f, 0.5f);
            parentRect.anchorMax = new Vector2(0.5f, 0.5f);
            parentRect.pivot = new Vector2(0.5f, 0.5f);
            parentRect.anchoredPosition = Vector2.zero;

            // Increased size for better visibility
            Vector2 promptSize = new Vector2(500, 500);

            warningPrompt = PromptBox.CreatePromptBox(
                promptParent.transform,
                "WARNING: This game contains flashing lights and visual effects that may trigger seizures for people with photosensitive epilepsy. Viewer discretion is advised.",
                PromptBox.PromptType.Warning,
                promptSize
            );

            // Additional text configuration
            if (warningPrompt != null && warningPrompt.messageText != null)
            {
                warningPrompt.messageText.fontSize = 1;
                warningPrompt.messageText.color = Color.yellow;
                warningPrompt.messageText.fontStyle = FontStyle.Bold;
            }

            warningPrompt.OnPromptConfirmed += OnContinueClicked;

            // Wait a frame before showing the prompt to ensure everything is set up
            StartCoroutine(DelayedShowPrompt());
        }

        private IEnumerator DelayedShowPrompt()
        {
            yield return null;
            warningPrompt.ShowPrompt();
            promptShown = true;
        }

        private void OnContinueClicked()
        {
            PlayerPrefs.SetInt("HasSeenFlashingLightsWarning", 1);
            PlayerPrefs.Save();

            // Clean up the prompt
            if (warningPrompt != null)
            {
                warningPrompt.OnPromptConfirmed -= OnContinueClicked;
                warningPrompt.HidePrompt();

                // Destroy the canvas too
                if (warningPrompt.transform.parent != null &&
                    warningPrompt.transform.parent.parent != null)
                {
                    Destroy(warningPrompt.transform.parent.parent.gameObject, 0.5f);
                }
                else if (warningPrompt.transform.parent != null)
                {
                    Destroy(warningPrompt.transform.parent.gameObject, 0.5f);
                }
                else
                {
                    Destroy(warningPrompt.gameObject, 0.5f);
                }

                warningPrompt = null;
            }

            promptShown = false;
            ContinueToNextState();
        }

        private void ContinueToNextState()
        {
            Debug.Log("[InitState] Transitioning to next state...");
			PlayState.Instance.LoadScene("MainMenu");
        }

        public override void Update()
        {
            base.Update();

            // The PromptBox now handles its own input, so we don't need to manually check for input here
            // We only need to handle the case where we want to bypass the prompt for testing
            #if UNITY_EDITOR
            if (Controls.isPressed("debug_skip") && promptShown)
            {
                OnContinueClicked();
            }
            #endif
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (warningPrompt != null)
            {
                warningPrompt.OnPromptConfirmed -= OnContinueClicked;

                if (warningPrompt.transform.parent != null &&
                    warningPrompt.transform.parent.parent != null)
                {
                    Destroy(warningPrompt.transform.parent.parent.gameObject);
                }
                else if (warningPrompt.transform.parent != null)
                {
                    Destroy(warningPrompt.transform.parent.gameObject);
                }
                else
                {
                    Destroy(warningPrompt.gameObject);
                }
            }
        }
    }
}
