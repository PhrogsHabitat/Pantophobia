using System.Collections.Generic;

namespace Phobia.Camera
{
    public enum BlendMode
    {
        Normal = 0,
        Darken = 1,
        Lighten = 2,
        Overlay = 3,
        HardLight = 4
    }

    [RequireComponent(typeof(UnityEngine.Camera))]
    public class PhobiaCamera : MonoBehaviour
    {
        #region Configuration Data

        /// <summary>
        /// Configuration for PhobiaCamera instances - runtime configurable
        /// </summary>
        [System.Serializable]
        public class CameraConfig
        {
            [Header("Basic Settings")]
            public string cameraId = "unknown";
            public bool enableBlending = true;
            public bool enableFilters = true;
            public bool enableTexturePooling = true;

            [Header("Render Settings")]
            public int renderTextureDepth = 24;
            public RenderTextureFormat textureFormat = RenderTextureFormat.ARGB32;
            public FilterMode filterMode = FilterMode.Bilinear;

            [Header("Performance")]
            public int maxPoolSize = 10;
            public bool autoCleanup = true;
            public float cleanupInterval = 30f;

            [Header("Quality")]
            public bool useAntiAliasing = false;
            public int antiAliasingLevel = 1;

            /// <summary>
            /// Create default camera config
            /// </summary>
            public static CameraConfig CreateDefault()
            {
                return new CameraConfig();
            }

            /// <summary>
            /// Create config optimized for high quality rendering
            /// </summary>
            public static CameraConfig CreateHighQualityConfig()
            {
                return new CameraConfig
                {
                    renderTextureDepth = 32,
                    textureFormat = RenderTextureFormat.ARGB32,
                    useAntiAliasing = true,
                    antiAliasingLevel = 4,
                    maxPoolSize = 20
                };
            }

            /// <summary>
            /// Create config optimized for performance
            /// </summary>
            public static CameraConfig CreatePerformanceConfig()
            {
                return new CameraConfig
                {
                    renderTextureDepth = 16,
                    textureFormat = RenderTextureFormat.RGB565,
                    useAntiAliasing = false,
                    maxPoolSize = 5,
                    enableFilters = false
                };
            }
        }

        #endregion

        #region Public Properties

        [SerializeField] private CameraConfig _config;
        public CameraConfig Config
        {
            get => _config ?? (_config = CameraConfig.CreateDefault());
            set => _config = value;
        }

        [Header("Legacy Support")]
        public string id = "unknown";

        [Header("Shader Settings")]
        public Shader blendShader; // Assign in Inspector!
        public Material blendMaterial; // Optional: assign material directly

        #endregion

        #region Private Fields

        // Core components
        private UnityEngine.Camera _camera;

        // Texture management
        private Queue<RenderTexture> texturePool = new Queue<RenderTexture>();
        private List<RenderTexture> grabbedTextures = new List<RenderTexture>();

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();

            // Initialize with legacy values for backward compatibility
            if (string.IsNullOrEmpty(Config.cameraId) && !string.IsNullOrEmpty(id))
            {
                Config.cameraId = id;
            }
        }

        void Start()
        {
            InitializeMaterials();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize materials and shaders
        /// </summary>
        private void InitializeMaterials()
        {
            if (blendMaterial == null)
            {
                Debug.LogWarning("[PhobiaCamera] No blend material assigned. Using default behavior.");
                // Create a simple default material
                blendMaterial = new Material(Shader.Find("Unlit/Color"));
                blendMaterial.color = Color.white;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the camera configuration at runtime
        /// </summary>
        public void UpdateConfig(CameraConfig newConfig)
        {
            if (newConfig == null)
            {
                return;
            }

            _config = newConfig;

            // Apply configuration changes
            ApplyConfigurationChanges();
        }

        /// <summary>
        /// Apply configuration changes to the camera
        /// </summary>
        private void ApplyConfigurationChanges()
        {
            // Update legacy id for backward compatibility
            if (!string.IsNullOrEmpty(Config.cameraId))
            {
                id = Config.cameraId;
            }
        }

        public RenderTexture GrabScreen()
        {
            RenderTexture target = GetPooledTexture(Screen.width, Screen.height);

            _camera.targetTexture = target;
            _camera.Render();
            _camera.targetTexture = null;

            grabbedTextures.Add(target);
            return target;
        }

        // Fixed: Added implementation for ApplyFilter
        public void ApplyFilter(RenderTexture target, Material filterMaterial)
        {
            RenderTexture temp = RenderTexture.GetTemporary(
                target.width,
                target.height
            );

            UnityEngine.Graphics.Blit(target, temp, filterMaterial);
            UnityEngine.Graphics.Blit(temp, target);
            RenderTexture.ReleaseTemporary(temp);
        }

        // Fixed: Added BlendMode parameter type
        public void RenderWithBlend(SpriteRenderer sprite, BlendMode blendMode)
        {
            if (blendMaterial == null)
            {
                return;
            }

            blendMaterial.SetInt("_BlendMode", (int)blendMode);
            RenderTexture preRender = GrabScreen();
            blendMaterial.SetTexture("_PreTex", preRender);
            sprite.material = blendMaterial;
        }

        private RenderTexture GetPooledTexture(int width, int height)
        {
            if (texturePool.Count > 0)
            {
                RenderTexture rt = texturePool.Dequeue();
                if (rt.width != width || rt.height != height)
                {
                    rt.Release();
                    return CreateRenderTexture(width, height);
                }
                return rt;
            }
            return CreateRenderTexture(width, height);
        }

        private RenderTexture CreateRenderTexture(int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24);
            rt.Create();
            return rt;
        }

        private void ReleaseTexture(RenderTexture rt)
        {
            texturePool.Enqueue(rt);
        }

        void LateUpdate()
        {
            foreach (var rt in grabbedTextures)
            {
                ReleaseTexture(rt);
            }
            grabbedTextures.Clear();
        }

        void OnDestroy()
        {
            if (blendMaterial != null)
            {
                Destroy(blendMaterial);
            }

            foreach (var rt in texturePool)
            {
                rt.Release();
            }
        }

        #endregion

    }
}
