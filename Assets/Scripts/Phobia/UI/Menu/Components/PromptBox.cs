using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Phobia.Graphics;
using Controls = Phobia.Input.Controls;
using Phobia.Gameplay;

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

        // Internal PhobiaSound wrappers for modular sound playback
        private Phobia.Audio.PhobiaSound showPhobiaSound;
        private Phobia.Audio.PhobiaSound confirmPhobiaSound;
        private Phobia.Audio.PhobiaSound warningPhobiaSound;

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

        // PhobiaSprite for prompt visuals/animation
        private PhobiaSprite promptSprite;

        // Events
        public System.Action OnPromptShown;
        public System.Action OnPromptConfirmed;
        public System.Action OnPromptHidden;

        // Overlayed text at top of prompt
        private Text overlayText;

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

            // Setup PhobiaSound wrappers for modular playback
            if (showSound != null)
            {
                showPhobiaSound = Phobia.Audio.PhobiaSound.Create(showSound, 1f, false, false);
            }
            if (confirmSound != null)
            {
                confirmPhobiaSound = Phobia.Audio.PhobiaSound.Create(confirmSound, 1f, false, false);
            }
            if (warningSound != null)
            {
                warningPhobiaSound = Phobia.Audio.PhobiaSound.Create(warningSound, 1f, false, false);
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

            // Setup PhobiaSprite for prompt visuals/animation
            if (promptSprite == null)
            {
                // Create PhobiaSprite as child for prompt visuals
                var spriteGO = new GameObject("PromptSprite");
                spriteGO.transform.SetParent(transform, false);
                promptSprite = spriteGO.AddComponent<PhobiaSprite>();

                // Ensure PromptSprite is centered and scaled to fill parent
                var spriteRect = spriteGO.AddComponent<RectTransform>();
                var parentRect = GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    spriteRect.anchorMin = Vector2.zero;
                    spriteRect.anchorMax = Vector2.one;
                    spriteRect.pivot = new Vector2(0.5f, 0.5f);
                    spriteRect.anchoredPosition = Vector2.zero;
                    spriteRect.sizeDelta = Vector2.zero;
                    spriteRect.localScale = Vector3.one;
                }
                else
                {
                    spriteGO.transform.localPosition = Vector3.zero;
                    spriteGO.transform.localScale = Vector3.one;
                }

                // Use correct resource path (no extension)
                string resourcePath = "Images/ui/PromptBox";
                string spriteName = "PromptBox";

                try
                {
                    promptSprite.LoadSparrowXML(resourcePath, spriteName);

                    // Register animation aliases and frame rates
                    promptSprite.AddAnim("PromptShow", "show", 30f);
                    promptSprite.AddAnim("PromptIdle", "idle", 30f);
                    promptSprite.AddAnim("PromptHide", "hide", 30f);
                    promptSprite.AddAnim("PromptConfirm", "confirm", 30f);
                    promptSprite.AddAnim("PromptHover", "hover", 30f);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PromptBox] Failed to load Sparrow animation: {ex.Message}");

                    // Fallback: if no Sparrow animation, load static PNG
                    try
                    {
                        promptSprite.LoadTexture(resourcePath);
                    }
                    catch (System.Exception ex2)
                    {
                        Debug.LogWarning($"[PromptBox] Failed to load static PNG: {ex2.Message}");
                    }
                }

                promptSprite.ApplyConfiguration();
            }

            // Ensure overlayText exists and is always on top of the PromptBox
            if (overlayText == null)
            {
                GameObject overlayGO = new GameObject("PromptOverlayText");
                overlayGO.transform.SetParent(transform, false);
                var rect = overlayGO.AddComponent<RectTransform>();
                // Stretch horizontally, anchor to top
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0, 60); // Height for text area

                overlayText = overlayGO.AddComponent<Text>();
                overlayText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                overlayText.fontSize = 36;
                overlayText.alignment = TextAnchor.UpperCenter;
                overlayText.color = Color.yellow;
                overlayText.horizontalOverflow = HorizontalWrapMode.Wrap;
                overlayText.verticalOverflow = VerticalWrapMode.Overflow;
                overlayText.text = "";
                overlayText.enabled = false;

                // Ensure overlay is rendered above everything else in PromptBox
                rect.SetAsLastSibling();
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
                // Play idle animation if visible and not animating
                if (promptSprite != null && !promptSprite.IsAnimationPlaying)
                {
                    promptSprite.PlayAnimation("PromptIdle");
                }
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
            // Ensure PromptBox is enabled before showing
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
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
                messageText.enabled = true;
            }
            // Example: show overlay text as prompt type (customize as needed)
            if (overlayText != null)
            {
                overlayText.text = type.ToString().ToUpperInvariant();
                overlayText.enabled = true;
            }

            ApplyPromptTypeStyle();
            PlayShowSound();

            // Play show animation on sprite
            if (promptSprite != null)
            {
                // Play "show" animation, do not loop, force play
                promptSprite.PlayAnim("show", shouldLoop: false, force: true);

                // Add callback for when show animation completes
                promptSprite.AddAnimationCompleteCallback("PromptShow", () => {
                    // Start playing idle animation after show animation completes
                    if (isVisible && !isAnimating)
                    {
                        // Play "idle" animation, loop, force play
                        promptSprite.PlayAnim("idle", shouldLoop: true, force: true);
                    }
                });
            }

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

            // Play hide animation on sprite
            if (promptSprite != null)
            {
                promptSprite.PlayAnimation("PromptHide");
            }

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
            // Play confirm animation on sprite
            if (promptSprite != null)
            {
                // Play "confirm" animation, do not loop, force play
                promptSprite.PlayAnim("confirm", shouldLoop: false, force: true);

                // Add callback for when confirm animation completes
                promptSprite.AddAnimationCompleteCallback("PromptConfirm", () => {
                    OnPromptConfirmed?.Invoke();
                    HidePrompt();
                });
            }
            else
            {
                OnPromptConfirmed?.Invoke();
                HidePrompt();
            }
        }

        private void HandleInput()
        {
            if (acceptAnyKey && IsAnyControlPressed())
            {
                ConfirmPrompt();
                return;
            }
            // Mouse hover animation (optional)
            if (confirmButton != null && IsMouseOverButton())
            {
                if (promptSprite != null)
                {
                    promptSprite.PlayAnimation("PromptHover");
                }
            }
        }

        private bool IsMouseOverButton()
        {
            if (confirmButton == null)
            {
                return false;
            }
            // Simple check: is mouse over button rect
            var pointer = UnityEngine.EventSystems.EventSystem.current;
            if (pointer == null)
            {
                return false;
            }

            return pointer.currentSelectedGameObject == confirmButton.gameObject;
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
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[PromptBox] Tried to start flashing while inactive.");
                return;
            }
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
            // Use PhobiaSound for modular playback
            if (promptType == PromptType.Warning || promptType == PromptType.Error)
            {
                if (warningPhobiaSound != null)
                {
                    warningPhobiaSound.Play();
                }
            }
            else
            {
                if (showPhobiaSound != null)
                {
                    showPhobiaSound.Play();
                }
            }
        }

        private void PlayConfirmSound()
        {
            if (confirmPhobiaSound != null)
            {
                confirmPhobiaSound.Play();
            }
        }

        // Add a public method to set overlay text
        public void SetOverlayText(string text, Color? color = null, int? fontSize = null)
        {
            if (overlayText != null)
            {
                overlayText.text = text;
                overlayText.enabled = !string.IsNullOrEmpty(text);
                if (color.HasValue)
				{
					overlayText.color = color.Value;
				}

				if (fontSize.HasValue)
				{
					overlayText.fontSize = fontSize.Value;
				}
			}
        }

        public static PromptBox CreateWorldSpacePromptBox(Transform parent, string message, PromptType type, Vector3 position, Vector2 size)
        {
            GameObject promptObj = new GameObject("PromptBox");

            var promptBox = promptObj.AddComponent<PromptBox>();
            promptBox.promptMessage = message;
            promptBox.promptType = type;

            // Set up RectTransform for world space
            var rect = promptObj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            // Position in world space
            promptObj.transform.position = position;

            // Create a world space canvas
            GameObject canvasObj = new GameObject("WorldSpaceCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = PlayState.Instance?.mainCamera;

            // Set the canvas as parent
            promptObj.transform.SetParent(canvasObj.transform, false);

            return promptBox;
        }

        /// <summary>
        /// Create a PromptBox on a GameObject with a specific size.
        /// </summary>
        public static PromptBox CreatePromptBox(Transform parent, string message, PromptType type, Vector2 size)
        {
            GameObject promptObj = new GameObject("PromptBox");
            promptObj.transform.SetParent(parent, false);

            var promptBox = promptObj.AddComponent<PromptBox>();
            promptBox.promptMessage = message;
            promptBox.promptType = type;

            // Set up RectTransform
            var rect = promptObj.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = promptObj.AddComponent<RectTransform>();
            }

            // Center and scale PromptBox
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = new Vector3(125, 125, 0); // Ensure scale is 1 for proper UI scaling

            // Find the PhobiaCamera and ensure the canvas is properly set up
            var phobiaCamera = PlayState.Instance?.mainCamera;
            if (phobiaCamera != null)
            {
                // Find or create a canvas if needed
                Canvas canvas = parent.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    // Create a canvas for this prompt
                    GameObject canvasObj = new GameObject("PromptCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = phobiaCamera;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();

                    // Set the canvas as parent of the prompt
                    promptObj.transform.SetParent(canvasObj.transform, false);
                }
                else
                {
                    // Ensure the existing canvas uses the PhobiaCamera
                    canvas.worldCamera = phobiaCamera;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
            }

            return promptBox;
        }
    }
}
