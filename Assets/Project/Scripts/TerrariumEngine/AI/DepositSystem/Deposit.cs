using UnityEngine;
using TerrariumEngine;
using TerrariumEngine.SpawnSystem;

namespace TerrariumEngine.AI.DepositSystem
{
    /// <summary>
    /// Represents a deposit area for food or water
    /// Manages the deposit location and area clearing
    /// </summary>
    public class Deposit : MonoBehaviour, IDebuggable
    {
        [Header("Deposit Settings")]
        [SerializeField] private DepositType depositType = DepositType.Food;
        [SerializeField] private Vector3 depositPosition = Vector3.zero;
        [SerializeField] private float depositRadius = 3f;
        [SerializeField] private bool isActive = true;
        
        [Header("Sand Clearing")]
        [SerializeField] private bool clearSandBlocks = true;
        [SerializeField] private int maxSandLevelsToClear = 2;
        [SerializeField] private float clearingRadius = 5f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        public string DebugName => $"{depositType}Deposit";
        public bool IsDebugEnabled { get; set; } = true;
        
        // Properties
        public DepositType DepositType => depositType;
        public Vector3 DepositPosition => depositPosition;
        public float DepositRadius => depositRadius;
        public bool IsActive => isActive;
        public bool ClearSandBlocks => clearSandBlocks;
        public int MaxSandLevelsToClear => maxSandLevelsToClear;
        public float ClearingRadius => clearingRadius;
        
        // References
        private TerrariumManager terrariumManager;
        private AIConstants constants;
        
        // State
        private bool isInitialized = false;
        private float lastClearingCheck = 0f;
        private float clearingCheckInterval = 2f;
        
        // Events
        public System.Action<Deposit> OnDepositActivated;
        public System.Action<Deposit> OnDepositDeactivated;
        public System.Action<Deposit, Vector3Int> OnSandBlockCleared;
        
        private void Awake()
        {
            terrariumManager = FindFirstObjectByType<TerrariumManager>();
            constants = AIConstants.Instance;
            
            if (terrariumManager == null)
            {
                Debug.LogError($"{DebugName}: TerrariumManager not found!");
                enabled = false;
            }
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void Start()
        {
            InitializeDeposit();
        }
        
        private void Update()
        {
            if (!isActive || !isInitialized) return;
            
            // Periodically check for sand blocks to clear
            if (clearSandBlocks && Time.time - lastClearingCheck >= clearingCheckInterval)
            {
                CheckAndClearSandBlocks();
                lastClearingCheck = Time.time;
            }
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Initialize the deposit
        /// </summary>
        private void InitializeDeposit()
        {
            // Set deposit position to current transform position if not set
            if (depositPosition == Vector3.zero)
            {
                depositPosition = transform.position;
            }
            
            // Update constants from AIConstants
            depositRadius = constants.depositRadius;
            maxSandLevelsToClear = constants.maxSandLevelsToClear;
            
            isInitialized = true;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Initialized at {depositPosition} with radius {depositRadius}");
            }
        }
        
        /// <summary>
        /// Set the deposit position
        /// </summary>
        /// <param name="position">New deposit position</param>
        public void SetDepositPosition(Vector3 position)
        {
            depositPosition = position;
            transform.position = position;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Position set to {position}");
            }
        }
        
        /// <summary>
        /// Activate the deposit
        /// </summary>
        public void Activate()
        {
            if (isActive) return;
            
            isActive = true;
            OnDepositActivated?.Invoke(this);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Activated");
            }
        }
        
        /// <summary>
        /// Deactivate the deposit
        /// </summary>
        public void Deactivate()
        {
            if (!isActive) return;
            
            isActive = false;
            OnDepositDeactivated?.Invoke(this);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Deactivated");
            }
        }
        
        /// <summary>
        /// Check if a position is within the deposit area
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if within deposit area</returns>
        public bool IsWithinDepositArea(Vector3 position)
        {
            if (!isActive) return false;
            
            float distance = Vector3.Distance(position, depositPosition);
            return distance <= depositRadius;
        }
        
        /// <summary>
        /// Get the closest point within the deposit area to a given position
        /// </summary>
        /// <param name="position">Reference position</param>
        /// <returns>Closest point within deposit area</returns>
        public Vector3 GetClosestPointInArea(Vector3 position)
        {
            if (!isActive) return depositPosition;
            
            Vector3 direction = (position - depositPosition).normalized;
            return depositPosition + direction * depositRadius;
        }
        
        /// <summary>
        /// Check and clear sand blocks in the deposit area
        /// </summary>
        private void CheckAndClearSandBlocks()
        {
            if (terrariumManager == null) return;
            
            // Get the grid position of the deposit center
            Vector3Int centerGridPos = terrariumManager.WorldToGridPosition(depositPosition);
            
            // Calculate clearing area in grid units
            int clearingRadiusInBlocks = Mathf.CeilToInt(clearingRadius * terrariumManager.LevelOfQuality);
            
            int blocksCleared = 0;
            
            // Check each grid position in the clearing area
            for (int x = centerGridPos.x - clearingRadiusInBlocks; x <= centerGridPos.x + clearingRadiusInBlocks; x++)
            {
                for (int z = centerGridPos.z - clearingRadiusInBlocks; z <= centerGridPos.z + clearingRadiusInBlocks; z++)
                {
                    // Check if this position is within the clearing radius
                    Vector3 worldPos = terrariumManager.GridToWorldPosition(new Vector3Int(x, 0, z));
                    float distance = Vector3.Distance(worldPos, depositPosition);
                    
                    if (distance <= clearingRadius)
                    {
                        // Clear sand blocks up to the specified levels
                        for (int y = 0; y < maxSandLevelsToClear; y++)
                        {
                            Vector3Int gridPos = new Vector3Int(x, y, z);
                            
                            if (terrariumManager.HasSandBlock(gridPos))
                            {
                                terrariumManager.DestroySandBlock(gridPos);
                                blocksCleared++;
                                OnSandBlockCleared?.Invoke(this, gridPos);
                            }
                        }
                    }
                }
            }
            
            if (blocksCleared > 0 && IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Cleared {blocksCleared} sand blocks");
            }
        }
        
        /// <summary>
        /// Get the highest point in the deposit area
        /// </summary>
        /// <returns>Highest point in the deposit area</returns>
        public Vector3 GetHighestPointInArea()
        {
            if (terrariumManager == null) return depositPosition;
            
            Vector3Int centerGridPos = terrariumManager.WorldToGridPosition(depositPosition);
            int highestY = -1;
            
            // Find the highest sand block in the deposit area
            for (int x = centerGridPos.x - 2; x <= centerGridPos.x + 2; x++)
            {
                for (int z = centerGridPos.z - 2; z <= centerGridPos.z + 2; z++)
                {
                    for (int y = terrariumManager.GetBlockCount().y - 1; y >= 0; y--)
                    {
                        Vector3Int gridPos = new Vector3Int(x, y, z);
                        if (terrariumManager.HasSandBlock(gridPos))
                        {
                            highestY = Mathf.Max(highestY, y);
                            break;
                        }
                    }
                }
            }
            
            if (highestY >= 0)
            {
                return terrariumManager.GridToWorldPosition(new Vector3Int(centerGridPos.x, highestY, centerGridPos.z)) + Vector3.up * 0.5f;
            }
            
            return depositPosition;
        }
        
        /// <summary>
        /// Get the number of sand blocks in the deposit area
        /// </summary>
        /// <returns>Number of sand blocks</returns>
        public int GetSandBlockCount()
        {
            if (terrariumManager == null) return 0;
            
            Vector3Int centerGridPos = terrariumManager.WorldToGridPosition(depositPosition);
            int clearingRadiusInBlocks = Mathf.CeilToInt(clearingRadius * terrariumManager.LevelOfQuality);
            int count = 0;
            
            for (int x = centerGridPos.x - clearingRadiusInBlocks; x <= centerGridPos.x + clearingRadiusInBlocks; x++)
            {
                for (int z = centerGridPos.z - clearingRadiusInBlocks; z <= centerGridPos.z + clearingRadiusInBlocks; z++)
                {
                    Vector3 worldPos = terrariumManager.GridToWorldPosition(new Vector3Int(x, 0, z));
                    float distance = Vector3.Distance(worldPos, depositPosition);
                    
                    if (distance <= clearingRadius)
                    {
                        for (int y = 0; y < terrariumManager.GetBlockCount().y; y++)
                        {
                            Vector3Int gridPos = new Vector3Int(x, y, z);
                            if (terrariumManager.HasSandBlock(gridPos))
                            {
                                count++;
                            }
                        }
                    }
                }
            }
            
            return count;
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw deposit info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                GUI.color = GetDepositColor();
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), 
                    $"{depositType} Deposit");
                yOffset += labelHeight;
                
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), 
                    $"Radius: {depositRadius:F1}m");
                yOffset += labelHeight;
                
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), 
                    $"Sand Blocks: {GetSandBlockCount()}");
                yOffset += labelHeight;
                
                GUI.color = isActive ? Color.green : Color.red;
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), 
                    isActive ? "ACTIVE" : "INACTIVE");
                GUI.color = Color.white;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!IsDebugEnabled) return;
            
            // Draw deposit area
            Gizmos.color = GetDepositColor();
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawSphere(depositPosition, depositRadius);
            
            // Draw clearing area
            if (clearSandBlocks)
            {
                Gizmos.color = Color.yellow;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                Gizmos.DrawSphere(depositPosition, clearingRadius);
            }
            
            // Draw deposit center
            Gizmos.color = GetDepositColor();
            Gizmos.DrawWireSphere(depositPosition, 0.5f);
        }
        
        /// <summary>
        /// Get color for this deposit type
        /// </summary>
        /// <returns>Color for the deposit type</returns>
        private Color GetDepositColor()
        {
            switch (depositType)
            {
                case DepositType.Food:
                    return Color.green;
                case DepositType.Water:
                    return Color.blue;
                default:
                    return Color.white;
            }
        }
    }
    
    /// <summary>
    /// Types of deposits
    /// </summary>
    public enum DepositType
    {
        Food,
        Water
    }
}

