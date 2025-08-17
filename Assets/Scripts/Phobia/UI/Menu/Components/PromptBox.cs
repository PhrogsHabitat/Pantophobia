using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Controls = Phobia.Input.Controls;

namespace Phobia.ui.Menu.Components
{

    /// <summary>
    /// Reusable prompt UI component for displaying warnings, confirmations, and messages.
    /// Features flashing effects, customizable styling, and input handling.
    ///
    /// Features:
    /// - Customizable prompt messages
    /// - Flashing warning effects
    /// - Button-based or key-based input
    /// - Fade in/out animations
    /// - Multiple prompt types (warning, info, confirmation)
    /// - Sound effects support
    /// </summary>
    public class PromptBox : MonoBehaviour
    {
        [Header("Prompt Settings")]
        [SerializeField] private PromptType promptType = PromptType.Warning;
        [SerializeField] private string promptMessage = "Press any key to continue...";
        [SerializeField] private bool autoShow = false;
        [SerializeField] private float autoHideDelay = 0f;

        [Header("Visual Settings")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text messageText;
        [SerializeField] private Image warningIcon;
        [SerializeField] private bool enableFlashing = true;
        [SerializeField] private float flashSpeed = 2f;
        [SerializeField] private Color flashColor = Color.red;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Input Settings")]
        [SerializeField] private bool acceptAnyKey = true;
        [SerializeField] private List<string> acceptedActions = new List<string> { "accept", "ui_up", "ui_down", "ui_left", "ui_right" };
        [SerializeField] private Button confirmButton;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip showSound;
        [SerializeField] private AudioClip confirmSound;
        [SerializeField] private AudioClip warningSound;

        public enum PromptType
        {
            Info,
            Warning,
            Confirmation,
            Error
        }

        private bool isVisible = false;
        private bool isAnimating = false;
        private Coroutine flashCoroutine;
        private Coroutine fadeCoroutine;
        private CanvasGroup canvasGroup;
        private Color originalBackgroundColor;
        private Color originalIconColor;

        // Events

        public System.Action OnPromptShown;
        public System.Action OnPromptConfirmed;
        public System.Action OnPromptHidden;

        private void Awake()
        {
            // Setup canvas group for fading

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Setup audio source

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Store original colors

            if (backgroundImage != null)
            {
                originalBackgroundColor = backgroundImage.color;
            }
            if (warningIcon != null)
            {
                originalIconColor = warningIcon.color;
            }

            // Setup button if provided

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmPrompt);
            }

            // Initially hide the prompt

            SetVisibility(false, false);
        }

        private void Start()
        {
            if (autoShow)
            {
                ShowPrompt();
            }
        }

        private void Update()
        {
            if (isVisible && !isAnimating)
            {
                HandleInput();
            }
        }


        /// <summary>
        /// Show the prompt with the current settings.
        /// </summary>
        public void ShowPrompt()
        {
            ShowPrompt(promptMessage, promptType);
        }


        /// <summary>
        /// Show the prompt with custom message and type.
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="type">Type of prompt</param>
        public void ShowPrompt(string message, PromptType type = PromptType.Info)
        {
            if (isVisible || isAnimating)
            {
                return;
            }

            promptMessage = message;
            promptType = type;

            // Update UI elements

            if (messageText != null)
            {
                messageText.text = promptMessage;
            }

            ApplyPromptTypeStyle();
            PlayShowSound();

            // Start fade in animation

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeIn());

            // Start flashing effect if enabled

            if (enableFlashing && (promptType == PromptType.Warning || promptType == PromptType.Error))
            {
                StartFlashing();
            }

            OnPromptShown?.Invoke();

            // Auto hide if specified

            if (autoHideDelay > 0)
            {
                Invoke(nameof(HidePrompt), autoHideDelay);
            }
        }


        /// <summary>
        /// Hide the prompt.
        /// </summary>
        public void HidePrompt()
        {
            if (!isVisible || isAnimating)
            {
                return;
            }

            StopFlashing();

            // Start fade out animation

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOut());

            OnPromptHidden?.Invoke();
        }


        /// <summary>
        /// Confirm the prompt (same as pressing accepted key).
        /// </summary>
        public void ConfirmPrompt()
        {
            if (!isVisible || isAnimating)
            {
                return;
            }

            PlayConfirmSound();
            OnPromptConfirmed?.Invoke();
            HidePrompt();
        }

        private void HandleInput()
        {
            if (acceptAnyKey && IsAnyControlPressed())
            {
                ConfirmPrompt();
                return;
            }

            foreach (var actionName in acceptedActions)
            {
                if (Controls.isPressed(actionName))
                {
                    ConfirmPrompt();
                    return;
                }
            }
        }

        private bool IsAnyControlPressed()
        {
            // Check common UI and action controls for "any key" behavior

            return Controls.ACCEPT || Controls.BACK ||
                   Controls.UI_UP || Controls.UI_DOWN || Controls.UI_LEFT || Controls.UI_RIGHT ||
                   Controls.isPressed("note_up") || Controls.isPressed("note_down") ||
                   Controls.isPressed("note_left") || Controls.isPressed("note_right") ||
                   Controls.isPressed("pause") || Controls.isPressed("reset");
        }

        private void ApplyPromptTypeStyle()
        {
            Color styleColor = Color.white;

            switch (promptType)
            {
                case PromptType.Info:
                    styleColor = Color.cyan;
                    break;
                case PromptType.Warning:
                    styleColor = Color.yellow;
                    break;
                case PromptType.Confirmation:
                    styleColor = Color.green;
                    break;
                case PromptType.Error:
                    styleColor = Color.red;
                    break;
            }

            if (warningIcon != null)
            {
                warningIcon.color = styleColor;
                warningIcon.gameObject.SetActive(promptType != PromptType.Info);
            }
        }

        private void StartFlashing()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(FlashCoroutine());
        }

        private void StopFlashing()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }

            // Restore original colors

            if (backgroundImage != null)
            {
                backgroundImage.color = originalBackgroundColor;
            }
            if (warningIcon != null)
            {
                warningIcon.color = originalIconColor;
            }
        }

        private IEnumerator FlashCoroutine()
        {
            while (true)
            {
                float time = 0f;
                while (time < 1f / flashSpeed)
                {
                    time += Time.deltaTime;
                    float alpha = Mathf.PingPong(time * flashSpeed * 2f, 1f);

                    if (backgroundImage != null)
                    {
                        backgroundImage.color = Color.Lerp(originalBackgroundColor, flashColor, alpha * 0.3f);
                    }

                    yield return null;
                }
            }
        }

        private IEnumerator FadeIn()
        {
            isAnimating = true;
            SetVisibility(true, false);

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeInDuration;
                float alpha = fadeInCurve.Evaluate(progress);
                canvasGroup.alpha = alpha;
                yield return null;
            }

            canvasGroup.alpha = 1f;
            isVisible = true;
            isAnimating = false;
        }

        private IEnumerator FadeOut()
        {
            isAnimating = true;

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeOutDuration;
                float alpha = fadeOutCurve.Evaluate(progress);
                canvasGroup.alpha = alpha;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            SetVisibility(false, false);
            isVisible = false;
            isAnimating = false;
        }

        private void SetVisibility(bool visible, bool immediate)
        {
            gameObject.SetActive(visible);
            if (immediate)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                isVisible = visible;
            }
        }

        private void PlayShowSound()
        {
            AudioClip soundToPlay = promptType == PromptType.Warning || promptType == PromptType.Error ? warningSound : showSound;
            if (audioSource != null && soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
        }

        private void PlayConfirmSound()
        {
            if (audioSource != null && confirmSound != null)
            {
                audioSource.PlayOneShot(confirmSound);
            }
        }


        /// <summary>
        /// Create a PromptBox on a GameObject.
        /// </summary>
        /// <param name="parent">Parent GameObject</param>
        /// <param name="message">Prompt message</param>
        /// <param name="type">Prompt type</param>
        /// <returns>Created PromptBox component</returns>
        public static PromptBox CreatePromptBox(Transform parent, string message, PromptType type = PromptType.Info)
        {
            GameObject promptObj = new GameObject("PromptBox");
            promptObj.transform.SetParent(parent);

            var promptBox = promptObj.AddComponent<PromptBox>();
            promptBox.promptMessage = message;
            promptBox.promptType = type;

            return promptBox;
        }
    }
}
