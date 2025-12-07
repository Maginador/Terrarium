using UnityEngine;
using TerrariumEngine;
using TerrariumEngine.AI.DepositSystem;
using TerrariumEngine.SpawnSystem;

namespace TerrariumEngine.AI.QueenAI
{
    /// <summary>
    /// Queen's organization system that manages deposits and coordinates workers
    /// </summary>
    public class QueenOrganizationSystem : MonoBehaviour, IDebuggable
    {
        [Header("Queen Organization Settings")]
        [SerializeField] private Vector3 originPosition = Vector3.zero;
        [SerializeField] private bool autoSetOrigin = true;
        [SerializeField] private float commandRadius = 20f;
        
        [Header("Deposit Management")]
        [SerializeField] private Deposit foodDeposit;
        [SerializeField] private Deposit waterDeposit;
        [SerializeField] private bool autoCreateDeposits = true;
        [SerializeField] private float depositDistance = 8f;
        
        [Header("Consumption Settings")]
        [SerializeField] private float consumptionRange = 2f;
        [SerializeField] private float requestInterval = 5f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        public string DebugName => "QueenOrganizationSystem";
        public bool IsDebugEnabled { get; set; } = true;
        
        // Properties
        public Vector3 OriginPosition => originPosition;
        public Deposit FoodDeposit => foodDeposit;
        public Deposit WaterDeposit => waterDeposit;
        public float CommandRadius => commandRadius;
        public float ConsumptionRange => consumptionRange;
        
        // References
        private QueenNPC queenNPC;
        private TerrariumManager terrariumManager;
        private AIConstants constants;
        
        // State
        private bool isInitialized = false;
        private float lastRequestTime = 0f;
        private bool isRequestingFood = false;
        private bool isRequestingWater = false;
        
        // Events
        public System.Action<DepositType, Vector3> OnDepositCreated;
        public System.Action<DepositType> OnResourceRequested;
        public System.Action<DepositType> OnResourceDelivered;
        public System.Action<DepositType> OnResourceConsumed;
        
        private void Awake()
        {
            queenNPC = GetComponent<QueenNPC>();
            terrariumManager = FindFirstObjectByType<TerrariumManager>();
            constants = AIConstants.Instance;
            
            if (queenNPC == null)
            {
                Debug.LogError($"{DebugName}: QueenNPC component not found!");
                enabled = false;
            }
            
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
            InitializeOrganizationSystem();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Update consumption requests
            UpdateConsumptionRequests();
            
            // Check for nearby resources to consume
            CheckForNearbyResources();
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Initialize the organization system
        /// </summary>
        private void InitializeOrganizationSystem()
        {
            // Set origin position
            if (autoSetOrigin)
            {
                originPosition = transform.position;
            }
            
            // Update constants
            consumptionRange = constants.queenConsumptionRange;
            requestInterval = constants.queenRequestInterval;
            
            // Create deposits if needed
            if (autoCreateDeposits)
            {
                CreateDeposits();
            }
            
            isInitialized = true;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Initialized at origin {originPosition}");
            }
        }
        
        /// <summary>
        /// Create food and water deposits
        /// </summary>
        private void CreateDeposits()
        {
            // Create food deposit
            if (foodDeposit == null)
            {
                Vector3 foodDepositPos = originPosition + Vector3.right * depositDistance;
                foodDeposit = CreateDeposit(DepositType.Food, foodDepositPos);
            }
            
            // Create water deposit
            if (waterDeposit == null)
            {
                Vector3 waterDepositPos = originPosition + Vector3.left * depositDistance;
                waterDeposit = CreateDeposit(DepositType.Water, waterDepositPos);
            }
        }
        
        /// <summary>
        /// Create a deposit at the specified position
        /// </summary>
        /// <param name="type">Type of deposit to create</param>
        /// <param name="position">Position for the deposit</param>
        /// <returns>Created deposit</returns>
        private Deposit CreateDeposit(DepositType type, Vector3 position)
        {
            GameObject depositObj = new GameObject($"{type}Deposit");
            depositObj.transform.position = position;
            
            Deposit deposit = depositObj.AddComponent<Deposit>();
            deposit.SetDepositPosition(position);
            
            OnDepositCreated?.Invoke(type, position);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Created {type} deposit at {position}");
            }
            
            return deposit;
        }
        
        /// <summary>
        /// Update consumption requests
        /// </summary>
        private void UpdateConsumptionRequests()
        {
            if (Time.time - lastRequestTime < requestInterval) return;
            
            lastRequestTime = Time.time;
            
            // Check if Queen needs food
            if (queenNPC != null && queenNPC.NeedsResource(StatType.Food, 0.3f))
            {
                RequestResource(DepositType.Food);
            }
            
            // Check if Queen needs water
            if (queenNPC != null && queenNPC.NeedsResource(StatType.Water, 0.3f))
            {
                RequestResource(DepositType.Water);
            }
        }
        
        /// <summary>
        /// Request a resource from workers
        /// </summary>
        /// <param name="type">Type of resource to request</param>
        public void RequestResource(DepositType type)
        {
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Requesting {type} from workers");
            }
            
            OnResourceRequested?.Invoke(type);
            
            // Set request flags
            if (type == DepositType.Food)
            {
                isRequestingFood = true;
            }
            else if (type == DepositType.Water)
            {
                isRequestingWater = true;
            }
        }
        
        /// <summary>
        /// Check for nearby resources to consume
        /// </summary>
        private void CheckForNearbyResources()
        {
            // Find nearby food items
            var nearbyFood = FindNearbyResources(DepositType.Food);
            if (nearbyFood.Count > 0)
            {
                ConsumeResource(DepositType.Food, nearbyFood[0]);
            }
            
            // Find nearby water items
            var nearbyWater = FindNearbyResources(DepositType.Water);
            if (nearbyWater.Count > 0)
            {
                ConsumeResource(DepositType.Water, nearbyWater[0]);
            }
        }
        
        /// <summary>
        /// Find nearby resources of a specific type
        /// </summary>
        /// <param name="type">Type of resource to find</param>
        /// <returns>List of nearby resources</returns>
        private System.Collections.Generic.List<BaseConsumable> FindNearbyResources(DepositType type)
        {
            var nearbyResources = new System.Collections.Generic.List<BaseConsumable>();
            
            // Find all consumables of the appropriate type
            var allConsumables = FindObjectsByType<BaseConsumable>(FindObjectsSortMode.None);
            
            foreach (var consumable in allConsumables)
            {
                float distance = Vector3.Distance(transform.position, consumable.transform.position);
                
                if (distance <= consumptionRange)
                {
                    // Check if it's the right type
                    bool isCorrectType = false;
                    
                    if (type == DepositType.Food && consumable is FoodItem)
                    {
                        isCorrectType = true;
                    }
                    else if (type == DepositType.Water && consumable is WaterItem)
                    {
                        isCorrectType = true;
                    }
                    
                    if (isCorrectType && consumable.CanBeConsumedBy(queenNPC))
                    {
                        nearbyResources.Add(consumable);
                    }
                }
            }
            
            return nearbyResources;
        }
        
        /// <summary>
        /// Consume a resource
        /// </summary>
        /// <param name="type">Type of resource being consumed</param>
        /// <param name="resource">Resource to consume</param>
        private void ConsumeResource(DepositType type, BaseConsumable resource)
        {
            if (resource == null || queenNPC == null) return;
            
            // Consume the resource
            float consumedAmount = resource.Consume(queenNPC);
            
            if (consumedAmount > 0)
            {
                OnResourceConsumed?.Invoke(type);
                
                // Clear request flags
                if (type == DepositType.Food)
                {
                    isRequestingFood = false;
                }
                else if (type == DepositType.Water)
                {
                    isRequestingWater = false;
                }
                
                if (IsDebugEnabled)
                {
                    Debug.Log($"{DebugName}: Consumed {consumedAmount} {type}");
                }
            }
        }
        
        /// <summary>
        /// Set the food deposit position
        /// </summary>
        /// <param name="position">New position for food deposit</param>
        public void SetFoodDepositPosition(Vector3 position)
        {
            if (foodDeposit == null)
            {
                foodDeposit = CreateDeposit(DepositType.Food, position);
            }
            else
            {
                foodDeposit.SetDepositPosition(position);
            }
        }
        
        /// <summary>
        /// Set the water deposit position
        /// </summary>
        /// <param name="position">New position for water deposit</param>
        public void SetWaterDepositPosition(Vector3 position)
        {
            if (waterDeposit == null)
            {
                waterDeposit = CreateDeposit(DepositType.Water, position);
            }
            else
            {
                waterDeposit.SetDepositPosition(position);
            }
        }
        
        /// <summary>
        /// Get the deposit for a specific type
        /// </summary>
        /// <param name="type">Type of deposit to get</param>
        /// <returns>Deposit of the specified type</returns>
        public Deposit GetDeposit(DepositType type)
        {
            switch (type)
            {
                case DepositType.Food:
                    return foodDeposit;
                case DepositType.Water:
                    return waterDeposit;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Check if Queen is requesting a specific resource
        /// </summary>
        /// <param name="type">Type of resource to check</param>
        /// <returns>True if requesting this resource</returns>
        public bool IsRequestingResource(DepositType type)
        {
            switch (type)
            {
                case DepositType.Food:
                    return isRequestingFood;
                case DepositType.Water:
                    return isRequestingWater;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get all workers within command radius
        /// </summary>
        /// <returns>List of workers within command radius</returns>
        public System.Collections.Generic.List<WorkerNPC> GetWorkersInRange()
        {
            var workersInRange = new System.Collections.Generic.List<WorkerNPC>();
            var allWorkers = FindObjectsByType<WorkerNPC>(FindObjectsSortMode.None);
            
            foreach (var worker in allWorkers)
            {
                float distance = Vector3.Distance(transform.position, worker.transform.position);
                if (distance <= commandRadius)
                {
                    workersInRange.Add(worker);
                }
            }
            
            return workersInRange;
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw organization system info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 4f);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                GUI.color = Color.magenta;
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + yOffset, 200, labelHeight), 
                    "Queen Organization System");
                yOffset += labelHeight;
                
                GUI.color = Color.white;
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + yOffset, 200, labelHeight), 
                    $"Origin: {originPosition}");
                yOffset += labelHeight;
                
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + yOffset, 200, labelHeight), 
                    $"Command Radius: {commandRadius:F1}m");
                yOffset += labelHeight;
                
                GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + yOffset, 200, labelHeight), 
                    $"Workers in Range: {GetWorkersInRange().Count}");
                yOffset += labelHeight;
                
                if (isRequestingFood)
                {
                    GUI.color = Color.green;
                    GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + yOffset, 200, labelHeight), 
                        "REQUESTING FOOD");
                    yOffset += labelHeight;
                }
                
                if (isRequestingWater)
                {
                    GUI.color = Color.blue;
                    GUI.Label(new Rect(screenPos.x - 100, Screen.height - screenPos.y + yOffset, 200, labelHeight), 
                        "REQUESTING WATER");
                }
                
                GUI.color = Color.white;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!IsDebugEnabled) return;
            
            // Draw origin position
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(originPosition, 0.5f);
            
            // Draw command radius
            Gizmos.color = Color.magenta;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
            Gizmos.DrawSphere(transform.position, commandRadius);
            
            // Draw consumption range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, consumptionRange);
            
            // Draw deposit connections
            if (foodDeposit != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, foodDeposit.transform.position);
            }
            
            if (waterDeposit != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, waterDeposit.transform.position);
            }
        }
    }
}

