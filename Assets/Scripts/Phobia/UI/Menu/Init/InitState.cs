using Phobia.Gameplay;
using UnityEngine;
using Phobia.Graphics;
using Phobia.Audio;
using Phobia.Input;
using Phobia.ui.Menu.Components;

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
			// Use modular PromptBox for warning
			warningPrompt = PromptBox.CreateWorldSpacePromptBox(
				uiCanvas.transform,
				"Warning: This game contains flashing lights and visual effects that may trigger seizures for people with photosensitive epilepsy. Viewer discretion is advised.",
				PromptBox.PromptType.Warning,
				new Vector3(4, 2.5f, -5),
				new Vector2(600, 200)
			);


			warningPrompt.OnPromptConfirmed += OnContinueClicked;

			warningPrompt.ShowPrompt();
			promptShown = true;
		}

		private void OnContinueClicked()
		{
			PlayerPrefs.SetInt("HasSeenFlashingLightsWarning", 1);
			PlayerPrefs.Save();
			// Sound and animation handled by PromptBox
			if (warningPrompt != null)
			{
				warningPrompt.ConfirmPrompt(); // Plays sound and animation, then hides
				Destroy(warningPrompt.gameObject);
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
			// Use PhobiaInput for modular input
			if (promptShown && Controls.isPressed("accept"))
			{
				OnContinueClicked();
			}

			if (Controls.isPressed("ui_up"))
			{
				warningPrompt.ConfirmPrompt();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (warningPrompt != null)
			{
				Destroy(warningPrompt.gameObject);
			}
		}
	}
}
