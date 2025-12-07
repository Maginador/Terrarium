using UnityEngine;

namespace TerrariumEngine
{
    /// <summary>
    /// Glass block that forms the walls of the terrarium aquarium
    /// </summary>
    public class GlassBlock : MonoBehaviour, IDestructible
    {
        [Header("Glass Block Settings")]
        [SerializeField] private Material glassMaterial;
        [SerializeField] private Color glassColor = new Color(0.8f, 0.9f, 1f, 0.3f); // Light blue with transparency
        
        private bool _isDestroyed = false;
        private Vector3 _worldPosition;
        private Renderer _renderer;
        private Collider _collider;
        
        public bool IsDestroyed => _isDestroyed;
        public Vector3 WorldPosition => _worldPosition;
        public GameObject GameObject => gameObject;
        
        public System.Action<IDestructible> OnDestroyed { get; set; }
        public System.Action<IDestructible> OnRestored { get; set; }
        
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            
            if (_renderer == null)
            {
                _renderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }
            
            SetupGlassMaterial();
            SetupClickThrough();
        }
        
        private void Start()
        {
            _worldPosition = transform.position;
        }
        
        private void Update()
        {
            _worldPosition = transform.position;
        }
        
        /// <summary>
        /// Setup the glass material with transparency
        /// </summary>
        private void SetupGlassMaterial()
        {
            if (glassMaterial != null)
            {
                _renderer.material = glassMaterial;
            }
            else
            {
                // Create a simple transparent material
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = glassColor;
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                
                _renderer.material = mat;
            }
        }
        
        /// <summary>
        /// Setup the glass block to ignore mouse clicks (click-through)
        /// </summary>
        private void SetupClickThrough()
        {
            // Set the collider to be a trigger so it doesn't block physics
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
            
            // Add a component to handle click-through behavior
            var clickThrough = gameObject.GetComponent<ClickThroughHandler>();
            if (clickThrough == null)
            {
                clickThrough = gameObject.AddComponent<ClickThroughHandler>();
            }
        }
        
        /// <summary>
        /// Initialize the glass block at a specific position
        /// </summary>
        /// <param name="position">World position</param>
        public void Initialize(Vector3 position)
        {
            transform.position = position;
            _worldPosition = position;
            Restore();
            
            // Ensure the block is active and visible
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
        }
        
        public void Destroy()
        {
            if (_isDestroyed) return;
            
            _isDestroyed = true;
            gameObject.SetActive(false);
            
            OnDestroyed?.Invoke(this);
        }
        
        public void Restore()
        {
            _isDestroyed = false;
            gameObject.SetActive(true);
            
            OnRestored?.Invoke(this);
        }
        
        /// <summary>
        /// Set the material for this glass block
        /// </summary>
        /// <param name="material">Material to apply</param>
        public void SetMaterial(Material material)
        {
            if (_renderer != null && material != null)
            {
                _renderer.material = material;
            }
        }
        
        /// <summary>
        /// Set the color for this glass block
        /// </summary>
        /// <param name="color">Color to apply</param>
        public void SetColor(Color color)
        {
            if (_renderer != null)
            {
                _renderer.material.color = color;
            }
        }
    }
}
