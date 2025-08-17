using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Phobia.ui.Menu.Components
{

    /// <summary>
    /// Reusable component for creating typewriter-style text animations.
    /// Displays text letter by letter with customizable timing and effects.
    ///
    /// Features:
    /// - Letter-by-letter text reveal
    /// - Customizable typing speed
    /// - Sound effects support
    /// - Pause and resume functionality
    /// - Completion callbacks
    /// - Rich text support
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TypewriterText : MonoBehaviour
    {
        [Header("Typewriter Settings")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private float punctuationDelay = 0.2f;
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private bool loopAnimation = false;


        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip typingSound;
        [SerializeField] private float audioVolume = 0.5f;
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

        [Header("Effects")]
        [SerializeField] private bool fadeInLetters = false;
        [SerializeField] private float fadeInDuration = 0.1f;

        private Text textComponent;
        private string fullText;
        private string currentText;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private bool isPaused = false;
        private int currentCharIndex = 0;

        // Events

        public System.Action OnTypingStarted;
        public System.Action OnTypingCompleted;
        public System.Action<char> OnCharacterTyped;

        private void Awake()
        {
            textComponent = GetComponent<Text>();

            // Setup audio source if not provided

            if (audioSource == null && typingSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = audioVolume;
            }
        }

        private void Start()
        {
            if (playOnStart && !string.IsNullOrEmpty(textComponent.text))
            {
                StartTypewriter(textComponent.text);
            }
        }


        /// <summary>
        /// Start the typewriter animation with the given text.
        /// </summary>
        /// <param name="text">Text to animate</param>
        public void StartTypewriter(string text)
        {
            if (isTyping)
            {
                StopTypewriter();
            }

            fullText = text;
            currentText = "";
            currentCharIndex = 0;
            textComponent.text = "";


            typingCoroutine = StartCoroutine(TypewriterCoroutine());
        }


        /// <summary>
        /// Stop the typewriter animation.
        /// </summary>
        public void StopTypewriter()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            isTyping = false;
            isPaused = false;
        }


        /// <summary>
        /// Pause the typewriter animation.
        /// </summary>
        public void PauseTypewriter()
        {
            isPaused = true;
        }


        /// <summary>
        /// Resume the typewriter animation.
        /// </summary>
        public void ResumeTypewriter()
        {
            isPaused = false;
        }


        /// <summary>
        /// Skip to the end of the animation immediately.
        /// </summary>
        public void SkipToEnd()
        {
            if (isTyping)
            {
                StopTypewriter();
                textComponent.text = fullText;
                OnTypingCompleted?.Invoke();
            }
        }


        /// <summary>
        /// Set the typing speed.
        /// </summary>
        /// <param name="speed">Time between characters in seconds</param>
        public void SetTypingSpeed(float speed)
        {
            typingSpeed = speed;
        }


        /// <summary>
        /// Check if the typewriter is currently animating.
        /// </summary>
        /// <returns>True if typing is in progress</returns>
        public bool IsTyping()
        {
            return isTyping;
        }

        private IEnumerator TypewriterCoroutine()
        {
            isTyping = true;
            OnTypingStarted?.Invoke();

            while (currentCharIndex < fullText.Length)
            {
                // Wait if paused

                while (isPaused)
                {
                    yield return null;
                }

                char currentChar = fullText[currentCharIndex];
                currentText += currentChar;
                textComponent.text = currentText;

                // Play typing sound

                PlayTypingSound(currentChar);

                // Trigger character event

                OnCharacterTyped?.Invoke(currentChar);

                // Apply fade effect if enabled

                if (fadeInLetters)
                {
                    StartCoroutine(FadeInLastCharacter());
                }

                currentCharIndex++;

                // Determine delay based on character type

                float delay = typingSpeed;
                if (IsPunctuation(currentChar))
                {
                    delay = punctuationDelay;
                }

                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
            OnTypingCompleted?.Invoke();

            // Loop if enabled

            if (loopAnimation)
            {
                yield return new WaitForSeconds(1f);
                StartTypewriter(fullText);
            }
        }

        private void PlayTypingSound(char character)
        {
            if (audioSource != null && typingSound != null && !char.IsWhiteSpace(character))
            {
                if (randomizePitch)
                {
                    audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                }
                audioSource.PlayOneShot(typingSound, audioVolume);
            }
        }

        private bool IsPunctuation(char character)
        {
            return character == '.' || character == ',' || character == '!' ||
                   character == '?' || character == ';' || character == ':';
        }

        private IEnumerator FadeInLastCharacter()
        {
            // This is a simplified fade effect
            // In a real implementation, you might use TextMeshPro for better character-level control

            Color originalColor = textComponent.color;
            Color transparentColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);


            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, originalColor.a, elapsed / fadeInDuration);
                textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }


            textComponent.color = originalColor;
        }


        /// <summary>
        /// Create a TypewriterText component on a GameObject with a Text component.
        /// </summary>
        /// <param name="textObject">GameObject with Text component</param>
        /// <param name="speed">Typing speed</param>
        /// <returns>The created TypewriterText component</returns>
        public static TypewriterText CreateTypewriter(GameObject textObject, float speed = 0.05f)
        {
            var typewriter = textObject.GetComponent<TypewriterText>();
            if (typewriter == null)
            {
                typewriter = textObject.AddComponent<TypewriterText>();
            }
            typewriter.SetTypingSpeed(speed);
            return typewriter;
        }
    }
}
