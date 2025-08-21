using Phobia.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using Phobia.Input;

namespace Phobia.ui.Menu.Init
{
	public class InitState : Phobia.ui.UIBase
	{
		private GameObject warningPrompt;
		private Button continueButton;
		private bool isFirstTime;
		private bool promptShown;
		private Font swagFont;

		public override void Initialize(PlayState playStateRef = null)
		{
			base.Initialize(playStateRef);
			// Check if it's the user's first time (PlayerPrefs or similar)
			isFirstTime = !PlayerPrefs.HasKey("HasSeenFlashingLightsWarning");
			promptShown = false;

			// Load local font resource (assumes you have a font at Resources/Fonts/MyFont.ttf)
			swagFont = Resources.Load<Font>(Paths.levelFont("initState", "swagFont"));
			if (swagFont == null)
			{
				Debug.LogWarning("[InitState] Local font not found at Resources/Fonts/MyFont.ttf, using Arial fallback.");
				swagFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
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
			// Create a simple UI prompt for flashing lights warning
			warningPrompt = new GameObject("FlashingLightsWarning");
			var rectTransform = warningPrompt.AddComponent<RectTransform>();
			rectTransform.sizeDelta = new Vector2(600, 300);
			warningPrompt.transform.SetParent(uiCanvas.transform, false);

			var image = warningPrompt.AddComponent<Image>();
			image.color = new Color(0, 0, 0, 0.85f);

			var textGO = new GameObject("WarningText");
			textGO.transform.SetParent(warningPrompt.transform, false);
			var text = textGO.AddComponent<Text>();
			text.text = "Warning: This game contains flashing lights and visual effects that may trigger seizures for people with photosensitive epilepsy. Viewer discretion is advised.";
			text.font = swagFont;
			text.alignment = TextAnchor.MiddleCenter;
			text.color = Color.white;
			text.rectTransform.sizeDelta = new Vector2(580, 200);
			text.rectTransform.anchoredPosition = new Vector2(0, 40);

			var buttonGO = new GameObject("ContinueButton");
			buttonGO.transform.SetParent(warningPrompt.transform, false);
			continueButton = buttonGO.AddComponent<Button>();
			var btnImage = buttonGO.AddComponent<Image>();
			btnImage.color = new Color(0.2f, 0.6f, 1f, 1f);
			var btnTextGO = new GameObject("ButtonText");
			btnTextGO.transform.SetParent(buttonGO.transform, false);
			var btnText = btnTextGO.AddComponent<Text>();
			btnText.text = "Continue";
			btnText.font = swagFont;
			btnText.alignment = TextAnchor.MiddleCenter;
			btnText.color = Color.white;
			btnText.rectTransform.sizeDelta = new Vector2(200, 40);
			btnText.rectTransform.anchoredPosition = Vector2.zero;
			var btnRect = buttonGO.GetComponent<RectTransform>();
			btnRect.sizeDelta = new Vector2(200, 40);
			btnRect.anchoredPosition = new Vector2(0, -80);

			continueButton.onClick.AddListener(OnContinueClicked);
			promptShown = true;
		}

		private void OnContinueClicked()
		{
			PlayerPrefs.SetInt("HasSeenFlashingLightsWarning", 1);
			PlayerPrefs.Save();
			if (warningPrompt != null)
			{
				Destroy(warningPrompt);
			}
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
			// Optionally allow keyboard input to continue
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
				Destroy(warningPrompt);
			}
		}
	}
}
