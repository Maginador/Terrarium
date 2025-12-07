using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TerrariumEngine
{
    /// <summary>
    /// Main manager for the terrarium system - handles sand blocks, terrain generation, and world interactions
    /// </summary>
    public class TerrariumManager : MonoBehaviour, IDebuggable
    {
        [Header("Terrarium Settings")]
        [SerializeField] private Vector3 terrariumSize = new Vector3(20, 10, 20);
        [SerializeField] private float levelOfQuality = 1f; // Blocks per unit
        [SerializeField] private Material defaultSandMaterial;
        [SerializeField] private Color defaultSandColor = Color.yellow;
        
        [Header("Sand Block Pool")]
        [SerializeField] private GameObject sandBlockPrefab;
        [SerializeField] private int initialPoolSize = 100;
        [SerializeField] private int maxPoolSize = 500;
        
        [Header("Glass Wall Settings")]
        [SerializeField] private GameObject glassBlockPrefab;
        [SerializeField] private bool generateGlassWalls = true;
        [SerializeField] private int glassWallHeight = 5; // Height of glass walls in blocks
        [SerializeField] private int initialGlassPoolSize = 50;
        [SerializeField] private int maxGlassPoolSize = 200;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        public string DebugName => "TerrariumManager";
        public bool IsDebugEnabled { get; set; } = true;
        
        private ObjectPool<SandBlock> _sandBlockPool;
        private ObjectPool<GlassBlock> _glassBlockPool;
        private Dictionary<Vector3Int, SandBlock> _activeSandBlocks = new Dictionary<Vector3Int, SandBlock>();
        private Dictionary<Vector3Int, GlassBlock> _activeGlassBlocks = new Dictionary<Vector3Int, GlassBlock>();
        private Transform _sandBlocksParent;
        private Transform _glassBlocksParent;
        
        public Vector3 TerrariumSize => terrariumSize;
        public float LevelOfQuality => levelOfQuality;
        public int ActiveSandBlocks => _activeSandBlocks.Count;
        public int ActiveGlassBlocks => _activeGlassBlocks.Count;
        
        public System.Action<Vector3Int> OnSandBlockDestroyed;
        public System.Action<Vector3Int> OnSandBlockCreated;
        
        private void Awake()
        {
            // Create parent objects
            _sandBlocksParent = new GameObject("SandBlocks").transform;
            _sandBlocksParent.SetParent(transform);
            
            _glassBlocksParent = new GameObject("GlassBlocks").transform;
            _glassBlocksParent.SetParent(transform);
            
            // Create sand block prefab if none exists
            if (sandBlockPrefab == null)
            {
                CreateSandBlockPrefab();
            }
            
            // Initialize sand block pool
            SandBlock sandBlockComponent = sandBlockPrefab.GetComponent<SandBlock>();
            if (sandBlockComponent == null)
            {
                sandBlockComponent = sandBlockPrefab.AddComponent<SandBlock>();
            }
            
            _sandBlockPool = new ObjectPool<SandBlock>(sandBlockComponent, _sandBlocksParent, initialPoolSize, maxPoolSize);
            
            // Create glass block prefab if none exists
            if (glassBlockPrefab == null)
            {
                CreateGlassBlockPrefab();
            }
            
            // Initialize glass block pool
            GlassBlock glassBlockComponent = glassBlockPrefab.GetComponent<GlassBlock>();
            if (glassBlockComponent == null)
            {
                glassBlockComponent = glassBlockPrefab.AddComponent<GlassBlock>();
            }
            
            _glassBlockPool = new ObjectPool<GlassBlock>(glassBlockComponent, _glassBlocksParent, initialGlassPoolSize, maxGlassPoolSize);
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void Start()
        {
            GenerateInitialTerrain();
            
            if (generateGlassWalls)
            {
                GenerateGlassWalls();
            }
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Generate the initial terrain
        /// </summary>
        private void GenerateInitialTerrain()
        {
            Vector3Int blockCount = GetBlockCount();
            
            for (int x = 0; x < blockCount.x; x++)
            {
                for (int z = 0; z < blockCount.z; z++)
                {
                    // Create a simple flat terrain for now
                    for (int y = 0; y < Mathf.FloorToInt(blockCount.y * 0.3f); y++)
                    {
                        Vector3Int blockPos = new Vector3Int(x, y, z);
                        CreateSandBlock(blockPos);
                    }
                }
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"TerrariumManager: Generated {_activeSandBlocks.Count} sand blocks");
            }
        }
        
        /// <summary>
        /// Generate glass walls around the terrarium
        /// </summary>
        private void GenerateGlassWalls()
        {
            Vector3Int blockCount = GetBlockCount();
            
            // Generate walls around the perimeter
            for (int x = 0; x < blockCount.x; x++)
            {
                for (int z = 0; z < blockCount.z; z++)
                {
                    // Check if this position is on the perimeter
                    bool isPerimeter = (x == 0 || x == blockCount.x - 1 || z == 0 || z == blockCount.z - 1);
                    
                    if (isPerimeter)
                    {
                        // Create glass wall from ground level to specified height
                        for (int y = 0; y < glassWallHeight; y++)
                        {
                            Vector3Int glassPos = new Vector3Int(x, y, z);
                            
                            // Only place glass if there's no sand block at this position
                            if (!_activeSandBlocks.ContainsKey(glassPos))
                            {
                                CreateGlassBlock(glassPos);
                            }
                        }
                    }
                }
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"TerrariumManager: Generated {_activeGlassBlocks.Count} glass blocks");
            }
        }
        
        /// <summary>
        /// Get the highest sand block position at a given X,Z coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>World position of the highest block, or Vector3.zero if none found</returns>
        public Vector3 GetHighestBlockPosition(int x, int z)
        {
            Vector3Int blockCount = GetBlockCount();
            int highestY = -1;
            
            // Find the highest block at this X,Z position
            for (int y = blockCount.y - 1; y >= 0; y--)
            {
                Vector3Int gridPos = new Vector3Int(x, y, z);
                if (_activeSandBlocks.ContainsKey(gridPos))
                {
                    highestY = y;
                    break;
                }
            }
            
            if (highestY >= 0)
            {
                Vector3Int gridPos = new Vector3Int(x, highestY, z);
                return GridToWorldPosition(gridPos) + Vector3.up * (1f / levelOfQuality); // Position on top of block
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Get the center position of the terrarium with the highest block
        /// </summary>
        /// <returns>World position at center, on top of highest block</returns>
        public Vector3 GetCenterPosition()
        {
            Vector3Int blockCount = GetBlockCount();
            int centerX = blockCount.x / 2;
            int centerZ = blockCount.z / 2;
            
            Vector3 centerPosition = GetHighestBlockPosition(centerX, centerZ);
            
            // If no block found at exact center, try nearby positions
            if (centerPosition == Vector3.zero)
            {
                // Try a small area around the center
                for (int offset = 1; offset <= 3; offset++)
                {
                    // Try 4 directions around center
                    Vector3[] offsets = {
                        GetHighestBlockPosition(centerX + offset, centerZ),
                        GetHighestBlockPosition(centerX - offset, centerZ),
                        GetHighestBlockPosition(centerX, centerZ + offset),
                        GetHighestBlockPosition(centerX, centerZ - offset)
                    };
                    
                    foreach (Vector3 pos in offsets)
                    {
                        if (pos != Vector3.zero)
                        {
                            if (IsDebugEnabled)
                            {
                                Debug.Log($"TerrariumManager: Found center position at offset {offset}: {pos}");
                            }
                            return pos;
                        }
                    }
                }
            }
            
            return centerPosition;
        }
        
        /// <summary>
        /// Create a sand block at the specified grid position
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>Created sand block or null if position is occupied</returns>
        public SandBlock CreateSandBlock(Vector3Int gridPosition)
        {
            if (_activeSandBlocks.ContainsKey(gridPosition))
            {
                return _activeSandBlocks[gridPosition];
            }
            
            SandBlock sandBlock = _sandBlockPool.Get();
            if (sandBlock == null)
            {
                Debug.LogWarning("TerrariumManager: Could not create sand block - pool is full");
                return null;
            }
            
            Vector3 worldPosition = GridToWorldPosition(gridPosition);
            sandBlock.Initialize(worldPosition);
            
            _activeSandBlocks[gridPosition] = sandBlock;
            
            // Set up destruction callback
            sandBlock.OnDestroyed += (destructible) => {
                _activeSandBlocks.Remove(gridPosition);
                _sandBlockPool.Return(sandBlock);
                OnSandBlockDestroyed?.Invoke(gridPosition);
            };
            
            OnSandBlockCreated?.Invoke(gridPosition);
            return sandBlock;
        }
        
        /// <summary>
        /// Create a glass block at the specified grid position
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>Created glass block or null if position is occupied</returns>
        public GlassBlock CreateGlassBlock(Vector3Int gridPosition)
        {
            if (_activeGlassBlocks.ContainsKey(gridPosition))
            {
                return _activeGlassBlocks[gridPosition];
            }
            
            GlassBlock glassBlock = _glassBlockPool.Get();
            if (glassBlock == null)
            {
                Debug.LogWarning("TerrariumManager: Could not create glass block - pool is full");
                return null;
            }
            
            Vector3 worldPosition = GridToWorldPosition(gridPosition);
            glassBlock.Initialize(worldPosition);
            
            _activeGlassBlocks[gridPosition] = glassBlock;
            
            // Set up destruction callback
            glassBlock.OnDestroyed += (destructible) => {
                _activeGlassBlocks.Remove(gridPosition);
                _glassBlockPool.Return(glassBlock);
            };
            
            return glassBlock;
        }
        
        /// <summary>
        /// Destroy a sand block at the specified grid position
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>True if block was destroyed, false if no block existed at position</returns>
        public bool DestroySandBlock(Vector3Int gridPosition)
        {
            if (_activeSandBlocks.TryGetValue(gridPosition, out SandBlock sandBlock))
            {
                sandBlock.Destroy();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get sand block at grid position
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>Sand block or null if none exists</returns>
        public SandBlock GetSandBlock(Vector3Int gridPosition)
        {
            _activeSandBlocks.TryGetValue(gridPosition, out SandBlock sandBlock);
            return sandBlock;
        }
        
        /// <summary>
        /// Check if a position has a sand block
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>True if block exists</returns>
        public bool HasSandBlock(Vector3Int gridPosition)
        {
            return _activeSandBlocks.ContainsKey(gridPosition);
        }
        
        /// <summary>
        /// Convert world position to grid position
        /// </summary>
        /// <param name="worldPosition">World position</param>
        /// <returns>Grid position</returns>
        public Vector3Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - transform.position;
            return new Vector3Int(
                Mathf.FloorToInt(localPos.x * levelOfQuality),
                Mathf.FloorToInt(localPos.y * levelOfQuality),
                Mathf.FloorToInt(localPos.z * levelOfQuality)
            );
        }
        
        /// <summary>
        /// Convert grid position to world position
        /// </summary>
        /// <param name="gridPosition">Grid position</param>
        /// <returns>World position</returns>
        public Vector3 GridToWorldPosition(Vector3Int gridPosition)
        {
            return transform.position + new Vector3(
                gridPosition.x / levelOfQuality,
                gridPosition.y / levelOfQuality,
                gridPosition.z / levelOfQuality
            );
        }
        
        /// <summary>
        /// Get the number of blocks in each dimension
        /// </summary>
        /// <returns>Block count as Vector3Int</returns>
        public Vector3Int GetBlockCount()
        {
            return new Vector3Int(
                Mathf.FloorToInt(terrariumSize.x * levelOfQuality),
                Mathf.FloorToInt(terrariumSize.y * levelOfQuality),
                Mathf.FloorToInt(terrariumSize.z * levelOfQuality)
            );
        }
        
        /// <summary>
        /// Check if a grid position is within terrarium bounds
        /// </summary>
        /// <param name="gridPosition">Grid position to check</param>
        /// <returns>True if within bounds</returns>
        public bool IsWithinBounds(Vector3Int gridPosition)
        {
            Vector3Int blockCount = GetBlockCount();
            return gridPosition.x >= 0 && gridPosition.x < blockCount.x &&
                   gridPosition.y >= 0 && gridPosition.y < blockCount.y &&
                   gridPosition.z >= 0 && gridPosition.z < blockCount.z;
        }
        
        private void CreateSandBlockPrefab()
        {
            // Create a simple cube prefab
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "SandBlock";
            cube.transform.localScale = Vector3.one / levelOfQuality;
            
            // Set default material
            Renderer renderer = cube.GetComponent<Renderer>();
            if (defaultSandMaterial != null)
            {
                renderer.material = defaultSandMaterial;
            }
            else
            {
                renderer.material.color = defaultSandColor;
            }
            
            sandBlockPrefab = cube;
        }
        
        private void CreateGlassBlockPrefab()
        {
            // Create a simple cube prefab for glass
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "GlassBlock";
            cube.transform.localScale = Vector3.one / levelOfQuality;
            
            // Set default glass material
            Renderer renderer = cube.GetComponent<Renderer>();
            Material glassMat = new Material(Shader.Find("Standard"));
            glassMat.color = new Color(0.8f, 0.9f, 1f, 0.3f); // Light blue with transparency
            glassMat.SetFloat("_Mode", 3); // Transparent mode
            glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glassMat.SetInt("_ZWrite", 0);
            glassMat.DisableKeyword("_ALPHATEST_ON");
            glassMat.EnableKeyword("_ALPHABLEND_ON");
            glassMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            glassMat.renderQueue = 3000;
            
            renderer.material = glassMat;
            
            // Make collider a trigger for click-through
            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            glassBlockPrefab = cube;
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            GUILayout.BeginArea(new Rect(10, 220, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Terrarium Info", GUI.skin.box);
            GUILayout.Space(5);
            
            GUILayout.Label($"Size: {terrariumSize}", GUI.skin.label);
            GUILayout.Label($"LoQ: {levelOfQuality}", GUI.skin.label);
            GUILayout.Label($"Sand Blocks: {ActiveSandBlocks}", GUI.skin.label);
            GUILayout.Label($"Glass Blocks: {ActiveGlassBlocks}", GUI.skin.label);
            GUILayout.Label($"Sand Pool: {_sandBlockPool.PooledCount}", GUI.skin.label);
            GUILayout.Label($"Glass Pool: {_glassBlockPool.PooledCount}", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnDrawGizmos()
        {
            if (!IsDebugEnabled) return;
            
            // Draw terrarium bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + terrariumSize * 0.5f, terrariumSize);
        }
    }
}
