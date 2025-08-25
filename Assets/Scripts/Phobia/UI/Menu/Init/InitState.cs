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
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 100; // High sorting order to ensure it's on top

			CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;

			canvasObj.AddComponent<GraphicRaycaster>();

			Vector2 promptSize = new Vector2(800, 300);

			warningPrompt = PromptBox.CreatePromptBox(
				canvas.transform,
				"WARNING: This game contains flashing lights and visual effects that may trigger seizures for people with photosensitive epilepsy. Viewer discretion is advised.",
				PromptBox.PromptType.Warning,
				promptSize
			);

			// Center the prompt (PromptBox handles its own scaling/animation now)
			if (warningPrompt != null)
			{
				RectTransform rect = warningPrompt.GetComponent<RectTransform>();
				if (rect != null)
				{
					rect.anchorMin = new Vector2(0.5f, 0.5f);
					rect.anchorMax = new Vector2(0.5f, 0.5f);
					rect.pivot = new Vector2(0.5f, 0.5f);
					rect.anchoredPosition = Vector2.zero;
					rect.sizeDelta = promptSize;
					// rect.localScale = Vector3.one * 140.5f; // No longer needed if PromptBox handles scaling

					Debug.Log($"[InitState] PromptBox centered: scale={rect.localScale}");
				}
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
                if (warningPrompt.transform.parent != null)
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
            // TODO: Replace with actual next state logic
            Debug.Log("[InitState] Transitioning to next state...");
            // Example: PlayState.Instance.LoadScene("MainMenu");
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

                if (warningPrompt.transform.parent != null)
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
