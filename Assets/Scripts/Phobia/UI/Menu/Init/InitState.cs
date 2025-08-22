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
		private PhobiaSound confirmSound;
		private bool isFirstTime;
		private bool promptShown;

		public override void Initialize(PlayState playStateRef = null)
		{
			base.Initialize(playStateRef);
			isFirstTime = !PlayerPrefs.HasKey("HasSeenFlashingLightsWarning");
			promptShown = false;

			// Load confirmation sound (modular, from Resources)
			confirmSound = PhobiaSound.Create(Resources.Load<AudioClip>("Audio/UI/confirm"), 1f, false, false);
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
			warningPrompt = PromptBox.CreatePromptBox(
				uiCanvas.transform,
				"Warning: This game contains flashing lights and visual effects that may trigger seizures for people with photosensitive epilepsy. Viewer discretion is advised.",
				PromptBox.PromptType.Warning
			);
			warningPrompt.OnPromptConfirmed += OnContinueClicked;
			warningPrompt.ShowPrompt();
			promptShown = true;
		}

		private void OnContinueClicked()
		{
			PlayerPrefs.SetInt("HasSeenFlashingLightsWarning", 1);
			PlayerPrefs.Save();
			if (confirmSound != null)
			{
				confirmSound.Play();
			}
			if (warningPrompt != null)
			{
				warningPrompt.HidePrompt();
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
