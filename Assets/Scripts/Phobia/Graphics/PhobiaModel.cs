using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Phobia.Graphics
{
    /// <summary>
    /// Enhanced 3D model management system with runtime configuration and caching
    /// Handles mesh loading, animations, and material management with performance optimization
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PhobiaModel : MonoBehaviour
    {
        #region Configuration Data

        /// <summary>
        /// Configuration for PhobiaModel instances - runtime configurable like a boss
        /// </summary>
        [System.Serializable]
        public class ModelConfig
        {
            [Header("Basic Settings")]
            public bool enableCaching = true;
            public bool autoDestroy = false;
            public bool enableLOD = false;

            [Header("Animation Settings")]
            public bool enableAnimations = true;
            public float animationSpeed = 1f;
            public bool loopAnimations = true;
            public bool enableRootMotion = false;

            [Header("Rendering")]
            public bool enableShadows = true;
            public bool receiveShadows = true;
            public bool enableOcclusion = true;

            [Header("Performance")]
            public int maxCacheSize = 50;
            public bool useObjectPooling = false;
            public float cullingDistance = 100f;

            /// <summary>
            /// Create default model config
            /// </summary>
            public static ModelConfig CreateDefault()
            {
                return new ModelConfig();
            }

            /// <summary>
            /// Create config optimized for characters
            /// </summary>
            public static ModelConfig CreateCharacterConfig()
            {
                return new ModelConfig
                {
                    enableAnimations = true,
                    enableRootMotion = true,
                    enableShadows = true,
                    enableLOD = true,
                    cullingDistance = 150f
                };
            }

            /// <summary>
            /// Create config for static props
            /// </summary>
            public static ModelConfig CreateStaticConfig()
            {
                return new ModelConfig
                {
                    enableAnimations = false,
                    enableRootMotion = false,
                    enableLOD = false,
                    useObjectPooling = true
                };
            }
        }

        #endregion

        #region Static Cache System

        private static Dictionary<string, GameObject> _modelCache = new Dictionary<string, GameObject>();
        private static Dictionary<string, Material> _materialCache = new Dictionary<string, Material>();
        private static int _cacheHits = 0;
        private static int _cacheMisses = 0;

        /// <summary>
        /// Clear all cached models and materials
        /// </summary>
        public static void ClearCache()
        {
            _modelCache.Clear();
            _materialCache.Clear();
            Resources.UnloadUnusedAssets();
            Debug.Log($"[PhobiaModel] Cache cleared. Stats - Hits: {_cacheHits}, Misses: {_cacheMisses}");
        }

        /// <summary>
        /// Get cache performance stats
        /// </summary>
        public static (int hits, int misses, float hitRate) GetCacheStats()
        {
            float hitRate = _cacheHits + _cacheMisses > 0 ? (float)_cacheHits / (_cacheHits + _cacheMisses) : 0f;
            return (_cacheHits, _cacheMisses, hitRate);
        }

        #endregion

        #region Public Properties

        [SerializeField] private ModelConfig _config;
        public ModelConfig Config
        {
            get => _config ?? (_config = ModelConfig.CreateDefault());
            set => _config = value;
        }

        public Mesh Mesh => _meshFilter != null ? _meshFilter.mesh : _skinnedRenderer?.sharedMesh;
        public bool IsSkinned => _skinnedRenderer != null;
        public Animator Animator => _animator;
        public string CurrentAnimation { get; private set; }
        public bool IsVisible => _meshRenderer != null ? _meshRenderer.enabled :
                                _skinnedRenderer != null ? _skinnedRenderer.enabled : false;

        // Animation state tracking
        public bool IsAnimationPlaying => _animator != null && _animator.enabled &&
                                         _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1;

        #endregion

        #region Private Fields

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private SkinnedMeshRenderer _skinnedRenderer;
        private Animator _animator;
        private LODGroup _lodGroup;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create a PhobiaModel with basic configuration
        /// </summary>
        public static PhobiaModel Create(Vector3 position, string modelPath, Transform parent = null, ModelConfig config = null)
        {
            // Create container object
            GameObject container = new GameObject($"PhobiaModel_{Path.GetFileNameWithoutExtension(modelPath)}");
            container.transform.position = position;

            if (parent != null)
            {
                container.transform.SetParent(parent);
            }

            // Add PhobiaModel component
            PhobiaModel model = container.AddComponent<PhobiaModel>();

            // Apply configuration
            if (config != null)
            {
                model.Config = config;
            }

            // Load the model
            model.LoadModel(modelPath);

            return model;
        }

        /// <summary>
        /// Create a character model with appropriate settings
        /// </summary>
        public static PhobiaModel CreateCharacter(Vector3 position, string modelPath, Transform parent = null)
        {
            var config = ModelConfig.CreateCharacterConfig();
            return Create(position, modelPath, parent, config);
        }

        /// <summary>
        /// Create a static prop model
        /// </summary>
        public static PhobiaModel CreateStatic(Vector3 position, string modelPath, Transform parent = null)
        {
            var config = ModelConfig.CreateStaticConfig();
            return Create(position, modelPath, parent, config);
        }

        #endregion

        #region Initialization
        private void Awake()
        {
            // Get or add required components
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            // SkinnedMeshRenderer is optional
            _skinnedRenderer = GetComponent<SkinnedMeshRenderer>();
        }
        #endregion

        #region Model Loading
        public void LoadModel(string modelPath)
        {
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError("Model path is null or empty");
                return;
            }

            // Check cache first
            if (_modelCache.TryGetValue(modelPath, out GameObject cachedModel))
            {
                InstantiateModel(cachedModel);
                return;
            }

            // Load from resources
            GameObject modelPrefab = Resources.Load<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                Debug.LogError($"Model not found: {modelPath}");
                return;
            }

            // Add to cache
            _modelCache[modelPath] = modelPrefab;
            InstantiateModel(modelPrefab);
        }

        private void InstantiateModel(GameObject modelPrefab)
        {
            // Instantiate the model as a child
            GameObject modelInstance = Instantiate(modelPrefab, transform);
            modelInstance.name = $"{modelPrefab.name}_Instance";

            // Get render components from the instance
            _meshFilter = modelInstance.GetComponent<MeshFilter>() ?? _meshFilter;
            _skinnedRenderer = modelInstance.GetComponent<SkinnedMeshRenderer>() ?? _skinnedRenderer;
            _meshRenderer = modelInstance.GetComponent<MeshRenderer>() ?? _meshRenderer;

            // Handle animations - pass the original prefab
            SetupAnimator(modelInstance, modelPrefab);
        }

        private void SetupAnimator(GameObject modelInstance, GameObject modelPrefab)
        {
            // Get animator from the instance
            _animator = modelInstance.GetComponent<Animator>();

            // Create Animator if needed
            if (_animator == null)
            {
                Debug.LogWarning("No Animator component found on model instance, adding one");
                _animator = modelInstance.AddComponent<Animator>();
            }

            // Try to get the controller from the prefab
            Animator prefabAnimator = modelPrefab.GetComponent<Animator>();
            if (prefabAnimator != null && prefabAnimator.runtimeAnimatorController != null)
            {
                // Copy controller from prefab
                _animator.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
                Debug.Log($"Assigned animator controller: {prefabAnimator.runtimeAnimatorController.name}");
            }
            else
            {
                Debug.LogWarning("Prefab has no Animator or AnimatorController");
            }
        }
        #endregion

        #region Animation System
        public void PlayAnimation(string animationName, float transitionTime = 0.1f)
        {
            if (_animator == null)
            {
                Debug.LogWarning("Animator not available - cannot play animation");
                return;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("Animator has no controller - cannot play animation");
                return;
            }

            // Check if animation exists in controller
            bool animationExists = false;
            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName)
                {
                    animationExists = true;
                    break;
                }
            }

            if (!animationExists)
            {
                Debug.LogWarning($"Animation '{animationName}' not found in controller");
                return;
            }

            // Play the animation
            _animator.CrossFade(animationName, transitionTime);
            CurrentAnimation = animationName;
            Debug.Log($"Playing animation: {animationName}");
        }

        public void StopAnimation()
        {
            if (_animator != null)
            {
                _animator.enabled = false;
            }
        }

        public void ResumeAnimation()
        {
            if (_animator != null)
            {
                _animator.enabled = true;
            }
        }

        public void SetAnimationSpeed(float speed)
        {
            if (_animator != null)
            {
                _animator.speed = speed;
            }
        }
        #endregion

        #region Material Management
        public void SetMaterial(Material newMaterial)
        {
            Renderer renderer = GetRenderer();
            if (renderer == null)
            {
                return;
            }

            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = newMaterial;
            }
            renderer.materials = materials;
        }

        public void SetMaterialProperty(string property, float value)
        {
            Renderer renderer = GetRenderer();
            if (renderer == null)
            {
                return;
            }

            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty(property))
                {
                    mat.SetFloat(property, value);
                }
            }
        }

        public void SetMaterialProperty(string property, Color value)
        {
            Renderer renderer = GetRenderer();
            if (renderer == null)
            {
                return;
            }

            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty(property))
                {
                    mat.SetColor(property, value);
                }
            }
        }

        public void SetMaterialProperty(string property, Texture value)
        {
            Renderer renderer = GetRenderer();
            if (renderer == null)
            {
                return;
            }

            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty(property))
                {
                    mat.SetTexture(property, value);
                }
            }
        }
        #endregion

        #region Rendering Controls
        public void SetVisible(bool visible)
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = visible;
            }

            if (_skinnedRenderer != null)
            {
                _skinnedRenderer.enabled = visible;
            }
        }

        public void Fade(float targetAlpha, float duration)
        {
            StartCoroutine(FadeRoutine(targetAlpha, duration));
        }

        private IEnumerator FadeRoutine(float targetAlpha, float duration)
        {
            Renderer renderer = GetRenderer();
            if (renderer == null)
            {
                yield break;
            }

            Material[] materials = renderer.materials;
            float startAlpha = materials[0].color.a;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);

                foreach (Material mat in materials)
                {
                    Color color = mat.color;
                    color.a = newAlpha;
                    mat.color = color;
                }

                yield return null;
            }

            // Ensure final alpha
            foreach (Material mat in materials)
            {
                Color color = mat.color;
                color.a = targetAlpha;
                mat.color = color;
            }
        }
        #endregion

        #region Transformation Utilities
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetRotation(Vector3 eulerAngles)
        {
            transform.rotation = Quaternion.Euler(eulerAngles);
        }

        public void SetScale(Vector3 scale)
        {
            transform.localScale = scale;
        }

        public void SetScale(float uniformScale)
        {
            transform.localScale = Vector3.one * uniformScale;
        }

        public void ResetTransform()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        #endregion

        #region Helper Methods
        private Renderer GetRenderer()
        {
            if (_meshRenderer != null)
            {
                return _meshRenderer;
            }

            if (_skinnedRenderer != null)
            {
                return _skinnedRenderer;
            }

            return GetComponent<Renderer>();
        }

        public void LogAnimations()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                Debug.Log("No animator controller available");
                return;
            }

            Debug.Log("Available animations:");
            foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
            {
                Debug.Log($"- {clip.name} ({clip.length}s)");
            }
        }
        #endregion

        #region Physics
        public void EnablePhysics(bool enable, bool isKinematic = false)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null && enable)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            if (rb != null)
            {
                rb.isKinematic = isKinematic;
            }

            Collider collider = GetComponent<Collider>();
            if (collider == null && enable)
            {
                // Add appropriate collider based on mesh type
                if (Mesh != null)
                {
                    if (IsSkinned)
                    {
                        gameObject.AddComponent<BoxCollider>();
                    }
                    else
                    {
                        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = Mesh;
                        meshCollider.convex = true;
                    }
                }
            }

            if (collider != null)
            {
                collider.enabled = enable;
            }
        }
        #endregion
    }
}
