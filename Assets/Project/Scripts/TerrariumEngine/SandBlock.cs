using UnityEngine;

namespace TerrariumEngine
{
    /// <summary>
    /// Individual sand block that can be destroyed and pooled
    /// </summary>
    public class SandBlock : MonoBehaviour, IDestructible
    {
        [Header("Sand Block Settings")]
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Color defaultColor = Color.yellow;
        
        private bool _isDestroyed = false;
        private Vector3 _worldPosition;
        private Renderer _renderer;
        private Collider _collider;
        
        public bool IsDestroyed => _isDestroyed;
        public Vector3 WorldPosition => _worldPosition;
        
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
            
            // Set default material and color
            if (defaultMaterial != null)
            {
                _renderer.material = defaultMaterial;
            }
            else
            {
                _renderer.material.color = defaultColor;
            }
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
        /// Initialize the sand block at a specific position
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
        /// Set the material for this sand block
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
        /// Set the color for this sand block
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
