using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phobia.Input
{
    /// <summary>
    /// Code-first input system with clean property-based API - no visual editor required!
    /// Create all InputActions programmatically with intuitive access patterns.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PhobiaInput : MonoBehaviour
    {
        #region Configuration Class

        [Serializable]
        public class InputConfig
        {
            [Header("Scheme & Lifecycle")]
            public string controlScheme = "PhobiaKeyboard";
            public bool autoEnable = true;
            public bool dontDestroyOnLoad = true;
            public bool autoCreateDefaultActions = true;

            [Header("Input Processing")]
            public bool enableBuffering = true;
            public float bufferSeconds = 0.15f;
            public bool enableDeadzone = true;
            public float deadzoneThreshold = 0.2f;

            [Header("Behavior")]
            public bool lockAndHideCursor = false;
            public bool enableDebugLogging = false;

            [Header("Performance")]
            public bool enableCaching = true;
            public int maxCacheSize = 100;

			public static InputConfig CreateDefault()
			{
				return new InputConfig
				{
					controlScheme = "PhobiaKeyboard",
					autoEnable = true,
					dontDestroyOnLoad = true,
					autoCreateDefaultActions = true,
					enableBuffering = true,
					bufferSeconds = 0.15f,
					enableDeadzone = true,
					deadzoneThreshold = 0.2f,
					lockAndHideCursor = false,
					enableDebugLogging = false,
					enableCaching = true,
					maxCacheSize = 100
				};
			}

			public static InputConfig CreateUIFriendly()
			{
				return new InputConfig
				{
					lockAndHideCursor = false,
					enableBuffering = true,
					bufferSeconds = 0.1f,
					autoCreateDefaultActions = true
				};
			}

			public static InputConfig CreateGaming()
			{
				return new InputConfig
				{
					enableDeadzone = true,
					deadzoneThreshold = 0.15f,
					enableBuffering = true,
					bufferSeconds = 0.12f,
					autoCreateDefaultActions = true
				};
			}
		}

        #endregion

        #region PhobiaAction Class - FNF-Style Input Wrapper

        /// <summary>
        /// Action wrapper that provides clean input checking methods
        /// </summary>
        public class PhobiaAction
        {
            public string Name { get; private set; }
            public InputAction UnityAction { get; private set; }

            // State tracking for frame-based detection
            private bool _isPressed = false;
            private bool _wasPressed = false;
            private bool _justPressed = false;
            private bool _justReleased = false;

            // For vector inputs
            private Vector2 _vector2Value = Vector2.zero;
            private float _floatValue = 0f;

            public PhobiaAction(string name, InputAction unityAction)
            {
                Name = name;
                UnityAction = unityAction;

                // Subscribe to Unity Input System callbacks
                UnityAction.performed += OnPerformed;
                UnityAction.started += OnStarted;
                UnityAction.canceled += OnCanceled;
            }

            private void OnPerformed(InputAction.CallbackContext context)
            {
				// Handle different input types
				if (Constants.EDITOR_DEBUG)
				{
					Debug.Log($"[PhobiaInput] {Name} performed: {context.ReadValueAsObject()}");
				}
                if (context.valueType == typeof(bool))
				{
					UpdateBoolState(context.ReadValueAsButton());
				}
				else if (context.valueType == typeof(Vector2))
				{
					_vector2Value = context.ReadValue<Vector2>();
					UpdateBoolState(_vector2Value.magnitude > 0.1f);
				}
				else if (context.valueType == typeof(float))
				{
					_floatValue = context.ReadValue<float>();
					UpdateBoolState(_floatValue > 0.1f);
				}
            }

			private void OnStarted(InputAction.CallbackContext context)
			{
				UpdateBoolState(true);
			}

			private void OnCanceled(InputAction.CallbackContext context)
			{
				UpdateBoolState(false);
			}

			private void UpdateBoolState(bool pressed)
            {
                _wasPressed = _isPressed;
                _isPressed = pressed;
                _justPressed = _isPressed && !_wasPressed;
                _justReleased = !_isPressed && _wasPressed;
            }

			/// <summary>
			/// Check if input is currently being held
			/// </summary>
			public bool CheckPressed()
			{
				return _isPressed;
			}

			/// <summary>
			/// Check if input was just pressed this frame
			/// </summary>
			public bool CheckJustPressed()
			{
				return _justPressed;
			}

			/// <summary>
			/// Check if input was just released this frame
			/// </summary>
			public bool CheckJustReleased()
			{
				return _justReleased;
			}

			/// <summary>
			/// Default check behavior (just pressed)
			/// </summary>
			public bool Check()
			{
				return CheckJustPressed();
			}

			/// <summary>
			/// Get Vector2 value for movement inputs
			/// </summary>
			public Vector2 GetVector2()
			{
				return _vector2Value;
			}

			/// <summary>
			/// Get float value for analog inputs
			/// </summary>
			public float GetFloat()
			{
				return _floatValue;
			}

			/// <summary>
			/// Update frame-based state (call once per frame)
			/// </summary>
			public void UpdateFrame()
            {
                // Reset frame-based flags
                _justPressed = false;
                _justReleased = false;
            }

            public void Dispose()
            {
                UnityAction.performed -= OnPerformed;
                UnityAction.started -= OnStarted;
                UnityAction.canceled -= OnCanceled;
            }
        }

        #endregion

        #region Static Management & Factory Methods

        public static PhobiaInput Instance { get; private set; }

        // Static collections for management
        private static readonly List<PhobiaInput> _activeInputs = new List<PhobiaInput>();
        private static readonly Dictionary<string, InputConfig> _configCache = new Dictionary<string, InputConfig>();

        // Performance tracking
        private static int _cacheHits = 0;
        private static int _cacheMisses = 0;

        /// <summary>
        /// Create PhobiaInput with default configuration
        /// </summary>
        public static PhobiaInput Create(InputConfig config = null)
        {
            var go = new GameObject("PhobiaInput");
            var input = go.AddComponent<PhobiaInput>();
            input.Initialize(config ?? InputConfig.CreateDefault());
            return input;
        }

        /// <summary>
        /// Create gaming-optimized input system
        /// </summary>
        public static PhobiaInput CreateGaming()
        {
            return Create(InputConfig.CreateGaming());
        }

        /// <summary>
        /// Create UI-friendly input system
        /// </summary>
        public static PhobiaInput CreateUI()
        {
            return Create(InputConfig.CreateUIFriendly());
        }

        /// <summary>
        /// Get all active input instances
        /// </summary>
        public static List<PhobiaInput> GetActiveInputs()
        {
            _activeInputs.RemoveAll(i => i == null);
            return new List<PhobiaInput>(_activeInputs);
        }

        /// <summary>
        /// Get cache performance statistics
        /// </summary>
        public static (int hits, int misses, float hitRate) GetCacheStats()
        {
            int total = _cacheHits + _cacheMisses;
            float hitRate = total > 0 ? (float)_cacheHits / total : 0f;
            return (_cacheHits, _cacheMisses, hitRate);
        }

        /// <summary>
        /// Ensure `PhobiaInput` is instantiated early and persists across scenes.
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance == null)
            {
                var obj = new GameObject("PhobiaInput");
                DontDestroyOnLoad(obj);
                obj.AddComponent<PhobiaInput>();
            }
        }

        #endregion

        #region Instance Variables

        public InputConfig Config { get; private set; } = InputConfig.CreateDefault();

        // Programmatically created Unity Input System components
        private InputActionAsset _inputAsset;
        private InputActionMap _actionMap;

        // Action storage
        private Dictionary<string, PhobiaAction> _actions = new Dictionary<string, PhobiaAction>();
        private Dictionary<string, PhobiaAction> _byName = new Dictionary<string, PhobiaAction>(); // Name-based lookup

        // Pre-defined actions (created automatically)
        private PhobiaAction _ui_up, _ui_down, _ui_left, _ui_right;
        private PhobiaAction _note_up, _note_down, _note_left, _note_right;
        private PhobiaAction _accept, _back, _pause, _reset;
        private PhobiaAction _move; // Vector2 movement

        #endregion

        #region Property-Based API

        // UI Controls - clean property access pattern
        public bool UI_UP => _ui_up?.CheckPressed() ?? false;
        public bool UI_DOWN => _ui_down?.CheckPressed() ?? false;
        public bool UI_LEFT => _ui_left?.CheckPressed() ?? false;
        public bool UI_RIGHT => _ui_right?.CheckPressed() ?? false;

        public bool UI_UP_P => _ui_up?.CheckJustPressed() ?? false;
        public bool UI_DOWN_P => _ui_down?.CheckJustPressed() ?? false;
        public bool UI_LEFT_P => _ui_left?.CheckJustPressed() ?? false;
        public bool UI_RIGHT_P => _ui_right?.CheckJustPressed() ?? false;

        public bool UI_UP_R => _ui_up?.CheckJustReleased() ?? false;
        public bool UI_DOWN_R => _ui_down?.CheckJustReleased() ?? false;
        public bool UI_LEFT_R => _ui_left?.CheckJustReleased() ?? false;
        public bool UI_RIGHT_R => _ui_right?.CheckJustReleased() ?? false;

        // NOTE Controls for rhythm gameplay
        public bool NOTE_UP => _note_up?.CheckPressed() ?? false;
        public bool NOTE_DOWN => _note_down?.CheckPressed() ?? false;
        public bool NOTE_LEFT => _note_left?.CheckPressed() ?? false;
        public bool NOTE_RIGHT => _note_right?.CheckPressed() ?? false;

        public bool NOTE_UP_P => _note_up?.CheckJustPressed() ?? false;
        public bool NOTE_DOWN_P => _note_down?.CheckJustPressed() ?? false;
        public bool NOTE_LEFT_P => _note_left?.CheckJustPressed() ?? false;
        public bool NOTE_RIGHT_P => _note_right?.CheckJustPressed() ?? false;

        public bool NOTE_UP_R => _note_up?.CheckJustReleased() ?? false;
        public bool NOTE_DOWN_R => _note_down?.CheckJustReleased() ?? false;
        public bool NOTE_LEFT_R => _note_left?.CheckJustReleased() ?? false;
        public bool NOTE_RIGHT_R => _note_right?.CheckJustReleased() ?? false;

        // Action Controls
        public bool ACCEPT => _accept?.Check() ?? false;
        public bool BACK => _back?.Check() ?? false;
        public bool PAUSE => _pause?.Check() ?? false;
        public bool RESET => _reset?.Check() ?? false;

        // Movement (Vector2)
        public Vector2 MOVE => _move?.GetVector2() ?? Vector2.zero;

		#endregion

		#region Unity Lifecycle

		private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _activeInputs.Add(this);
        }

		private void OnEnable()
        {
            if (_actionMap == null)
            {
                Initialize(Config);
            }
            if (Config.autoEnable)
            {
                _actionMap?.Enable();
            }
        }

		private void Start()
        {
            if (Config.dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            if (Config.lockAndHideCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

		private void OnDisable()
        {
            _actionMap?.Disable();
        }

		private void Update()
        {
            Debug.Log("[DEBUG] PhobiaInput Update called");
            // Update all actions frame-based state
            foreach (var action in _actions.Values)
            {
                action.UpdateFrame();
            }
        }

		private void OnDestroy()
        {
            // Dispose all actions
            foreach (var action in _actions.Values)
            {
                action.Dispose();
            }

            // InputActionAsset doesn't have Dispose, but we can destroy it
            if (_inputAsset != null)
            {
                DestroyImmediate(_inputAsset);
            }

            _activeInputs.Remove(this);
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Initialization & Programmatic Action Creation

        /// <summary>
        /// Initialize the input system with configuration - creates everything programmatically!
        /// </summary>
        public void Initialize(InputConfig config)
        {
            Config = config ?? InputConfig.CreateDefault();

            // Check cache first
            string configKey = GetConfigCacheKey(config);
            if (Config.enableCaching && _configCache.TryGetValue(configKey, out var cachedConfig))
            {
                _cacheHits++;
                Config = cachedConfig;
                if (Config.enableDebugLogging)
                {
                    Debug.Log($"[PhobiaInput] Using cached config: {configKey}");
                }
            }
            else
            {
                _cacheMisses++;
                if (Config.enableCaching)
                {
                    _configCache[configKey] = Config;
                }
            }

            // Create Unity Input System components programmatically
            CreateInputSystemProgrammatically();

            // Disable InputActionAsset temporarily
            _inputAsset?.Disable();

            // Create default actions if enabled
            if (Config.autoCreateDefaultActions)
            {
                CreateDefaultActions();
            }

            // Re-enable InputActionAsset
            _inputAsset?.Enable();

            if (Config.enableDebugLogging)
            {
                Debug.Log($"[PhobiaInput] Initialized with {_actions.Count} actions.");
            }
        }

        private string GetConfigCacheKey(InputConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[PhobiaInput] InputConfig is null in GetConfigCacheKey.");
                return "InvalidConfig";
            }

            return config.controlScheme + config.bufferSeconds + config.deadzoneThreshold;
        }

        private void CreateInputSystemProgrammatically()
        {
            _actionMap = new InputActionMap("Gameplay");
            var moveAction = _actionMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            _inputAsset.AddActionMap(_actionMap);
        }

        /// <summary>
        /// Create a single action with the specified name and type
        /// </summary>
        private PhobiaAction CreateAction(string name, InputActionType type, string bindingGroup = null)
        {
            var action = _actionMap.AddAction(name, type, bindingGroup);
            var phobiaAction = new PhobiaAction(name, action);
            _actions[name] = phobiaAction;
            _byName[name] = phobiaAction; // Add to name-based lookup
            return phobiaAction;
        }

        /// <summary>
        /// Create default actions based on configuration
        /// </summary>
        private void CreateDefaultActions()
        {
            // Clear existing actions
            foreach (var action in _actions.Values)
            {
                action.Dispose();
            }
            _actions.Clear();
            _byName.Clear();

            // Create actions based on control scheme
            if (Config.controlScheme == "PhobiaKeyboard")
            {
                // Keyboard default bindings - FIXED: Now properly adding bindings
                _ui_up = CreateActionWithBinding("UI/Up", InputActionType.Button, "<Keyboard>/w", "<Keyboard>/upArrow");
                _ui_down = CreateActionWithBinding("UI/Down", InputActionType.Button, "<Keyboard>/s", "<Keyboard>/downArrow");
                _ui_left = CreateActionWithBinding("UI/Left", InputActionType.Button, "<Keyboard>/a", "<Keyboard>/leftArrow");
                _ui_right = CreateActionWithBinding("UI/Right", InputActionType.Button, "<Keyboard>/d", "<Keyboard>/rightArrow");

                _note_up = CreateActionWithBinding("Note/Up", InputActionType.Button, "<Keyboard>/upArrow", "<Keyboard>/w");
                _note_down = CreateActionWithBinding("Note/Down", InputActionType.Button, "<Keyboard>/downArrow", "<Keyboard>/s");
                _note_left = CreateActionWithBinding("Note/Left", InputActionType.Button, "<Keyboard>/leftArrow", "<Keyboard>/a");
                _note_right = CreateActionWithBinding("Note/Right", InputActionType.Button, "<Keyboard>/rightArrow", "<Keyboard>/d");

                _accept = CreateActionWithBinding("Actions/Accept", InputActionType.Button, "<Keyboard>/enter", "<Keyboard>/space", "<Keyboard>/z");
                _back = CreateActionWithBinding("Actions/Back", InputActionType.Button, "<Keyboard>/escape", "<Keyboard>/x");
                _pause = CreateActionWithBinding("Actions/Pause", InputActionType.Button, "<Keyboard>/escape", "<Keyboard>/p");
                _reset = CreateActionWithBinding("Actions/Reset", InputActionType.Button, "<Keyboard>/r");

                // Movement action with composite binding
                _move = CreateAction("Movement/Move", InputActionType.Value);
                _move.UnityAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
            }
            else if (Config.controlScheme == "PhobiaGamepad")
            {
                // Gamepad default bindings
                _ui_up = CreateAction("UI/Up", InputActionType.Button, "<Gamepad>/dpad/up");
                _ui_down = CreateAction("UI/Down", InputActionType.Button, "<Gamepad>/dpad/down");
                _ui_left = CreateAction("UI/Left", InputActionType.Button, "<Gamepad>/dpad/left");
                _ui_right = CreateAction("UI/Right", InputActionType.Button, "<Gamepad>/dpad/right");

                _note_up = CreateAction("Note/Up", InputActionType.Button, "<Gamepad>/buttonNorth");
                _note_down = CreateAction("Note/Down", InputActionType.Button, "<Gamepad>/buttonSouth");
                _note_left = CreateAction("Note/Left", InputActionType.Button, "<Gamepad>/buttonWest");
                _note_right = CreateAction("Note/Right", InputActionType.Button, "<Gamepad>/buttonEast");

                _accept = CreateAction("Actions/Accept", InputActionType.Button, "<Gamepad>/start");
                _back = CreateAction("Actions/Back", InputActionType.Button, "<Gamepad>/select");
                _pause = CreateAction("Actions/Pause", InputActionType.Button, "<Gamepad>/buttonEast");
                _reset = CreateAction("Actions/Reset", InputActionType.Button, "<Gamepad>/buttonWest");

                _move = CreateAction("Movement/Move", InputActionType.Value, "<Gamepad>/leftStick");

                // Touch bindings for mobile (if applicable)
                if (Application.isMobilePlatform)
                {
                    _ui_up = CreateAction("UI/Up", InputActionType.Button, "<Touchscreen>/touch0/up");
                    _ui_down = CreateAction("UI/Down", InputActionType.Button, "<Touchscreen>/touch0/down");
                    _ui_left = CreateAction("UI/Left", InputActionType.Button, "<Touchscreen>/touch0/left");
                    _ui_right = CreateAction("UI/Right", InputActionType.Button, "<Touchscreen>/touch0/right");

                    _note_up = CreateAction("Note/Up", InputActionType.Button, "<Touchscreen>/touch1/up");
                    _note_down = CreateAction("Note/Down", InputActionType.Button, "<Touchscreen>/touch1/down");
                    _note_left = CreateAction("Note/Left", InputActionType.Button, "<Touchscreen>/touch1/left");
                    _note_right = CreateAction("Note/Right", InputActionType.Button, "<Touchscreen>/touch1/right");

                    _accept = CreateAction("Actions/Accept", InputActionType.Button, "<Touchscreen>/touch2/press");
                    _back = CreateAction("Actions/Back", InputActionType.Button, "<Touchscreen>/touch2/press");
                    _pause = CreateAction("Actions/Pause", InputActionType.Button, "<Touchscreen>/touch2/press");
                    _reset = CreateAction("Actions/Reset", InputActionType.Button, "<Touchscreen>/touch2/press");
                }
            }

            // Save the asset to disk (for debugging)
		#if UNITY_EDITOR
			UnityEditor.AssetDatabase.CreateAsset(_inputAsset, "Assets/PhobiaInputActions.asset");
			UnityEditor.AssetDatabase.SaveAssets();
		#else
            Debug.LogWarning("[INPUT DEBUG] Asset saving is only available in the Unity Editor.");
		#endif
        }

		#endregion

		private PhobiaAction CreateActionWithBinding(string name, InputActionType type, params string[] bindings)
        {
            var action = CreateAction(name, type);
            foreach (var binding in bindings)
            {
                action.UnityAction.AddBinding(binding);
            }
            return action;
        }

        public PhobiaAction AddAction(string actionName, params string[] bindings)
		{
			// Disable the asset before modifying
			if (_inputAsset.enabled)
			{
				_inputAsset.Disable();
			}

			// Remove existing action if present
			var existingAction = _actionMap.actions.FirstOrDefault(a => a.name == actionName);
			if (existingAction != null)
			{
				UnityEngine.InputSystem.InputActionSetupExtensions.RemoveAction(_inputAsset, actionName);
			}
			if (_actions.ContainsKey(actionName))
			{
				_actions[actionName].Dispose();
				_actions.Remove(actionName);
			}
			var action = _actionMap.AddAction(actionName, InputActionType.Button);
			foreach (var binding in bindings)
			{
				action.AddBinding(binding);
			}

			// Re-enable the asset if it was previously enabled
			if (Config.autoEnable)
			{
				_inputAsset.Enable();
			}
			var phobiaAction = new PhobiaAction(actionName, action);
			_actions[actionName] = phobiaAction;
			return phobiaAction;
		}

        public bool CheckPressed(string actionName)
        {
            bool exists = _actions.ContainsKey(actionName);
            if (!exists)
            {
                return false;
            }
            var action = _actions[actionName];
            bool pressed = action.CheckPressed();
            return pressed;
        }

        public bool Check(string actionName)
        {
            return _actions.ContainsKey(actionName) && _actions[actionName].Check();
        }

        public bool CheckReleased(string actionName)
        {
            return _actions.ContainsKey(actionName) && _actions[actionName].CheckJustReleased();
        }

        public int GetActionCount()
        {
            return _actions.Count;
        }

        public bool HasAction(string actionName)
        {
            return _actions.ContainsKey(actionName);
        }

        public List<string> GetActionNames()
        {
            return _actions.Keys.ToList();
        }
    }
}
