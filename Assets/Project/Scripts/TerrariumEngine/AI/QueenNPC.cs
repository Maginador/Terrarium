using UnityEngine;
using TerrariumEngine.AI.QueenAI;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Queen NPC that organizes the colony and manages deposits
    /// </summary>
    public class QueenNPC : BaseNPC
    {
        [Header("Queen Specific Settings")]
        [SerializeField] private bool canMoveFromOrigin = false;
        [SerializeField] private float maxDistanceFromOrigin = 3f;
        [SerializeField] private Vector3 originPosition = Vector3.zero;
        
        [Header("Queen Components")]
        [SerializeField] private QueenOrganizationSystem organizationSystem;
        
        [Header("Worker Spawning")]
        [SerializeField] private GameObject workerPrefab;
        [SerializeField] private bool enableWorkerSpawning = true;
        
        // Properties
        public bool CanMoveFromOrigin => canMoveFromOrigin;
        public float MaxDistanceFromOrigin => maxDistanceFromOrigin;
        public Vector3 OriginPosition => originPosition;
        public QueenOrganizationSystem OrganizationSystem => organizationSystem;
        
        // References
        private AIConstants constants;
        
        // Worker spawning
        private float lastWorkerSpawnTime = 0f;
        private int currentWorkerCount = 0;
        
        protected override void Start()
        {
            base.Start();
            
            // Get constants
            constants = AIConstants.Instance;
            
            // Set origin position
            if (originPosition == Vector3.zero)
            {
                originPosition = transform.position;
            }
            
            // Update constants
            maxDistanceFromOrigin = constants.queenMaxDistanceFromOrigin;
            
            // Get or create organization system
            if (organizationSystem == null)
            {
                organizationSystem = GetComponent<QueenOrganizationSystem>();
                if (organizationSystem == null)
                {
                    organizationSystem = gameObject.AddComponent<QueenOrganizationSystem>();
                }
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Queen initialized at origin {originPosition}");
            }
        }
        
        protected override void UpdateBehavior()
        {
            
            // Update organization system
            if (organizationSystem != null)
            {
                // Organization system updates itself
            }
            
            // Check movement constraints
            CheckMovementConstraints();
            
            // Update worker spawning
            if (enableWorkerSpawning)
            {
                UpdateWorkerSpawning();
            }
        }
        
        /// <summary>
        /// Check if Queen can move to a position
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if Queen can move to this position</returns>
        public bool CanMoveTo(Vector3 position)
        {
            if (canMoveFromOrigin) return true;
            
            float distance = Vector3.Distance(position, originPosition);
            return distance <= maxDistanceFromOrigin;
        }
        
        /// <summary>
        /// Check movement constraints
        /// </summary>
        private void CheckMovementConstraints()
        {
            if (canMoveFromOrigin) return;
            
            float distance = Vector3.Distance(transform.position, originPosition);
            
            if (distance > maxDistanceFromOrigin)
            {
                // Move back towards origin
                Vector3 direction = (originPosition - transform.position).normalized;
                Vector3 constrainedPosition = originPosition + direction * maxDistanceFromOrigin;
                transform.position = constrainedPosition;
                
                if (IsDebugEnabled)
                {
                    Debug.Log($"{DebugName}: Moved back to origin constraint");
                }
            }
        }
        
        /// <summary>
        /// Set the origin position
        /// </summary>
        /// <param name="position">New origin position</param>
        public void SetOriginPosition(Vector3 position)
        {
            originPosition = position;
            
            if (organizationSystem != null)
            {
                // Update organization system origin
                // This would need to be implemented in QueenOrganizationSystem
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Origin position set to {position}");
            }
        }
        
        /// <summary>
        /// Enable or disable movement from origin
        /// </summary>
        /// <param name="enabled">Whether to enable movement from origin</param>
        public void SetCanMoveFromOrigin(bool enabled)
        {
            canMoveFromOrigin = enabled;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Can move from origin: {enabled}");
            }
        }
        
        /// <summary>
        /// Get the food deposit
        /// </summary>
        /// <returns>Food deposit, or null if none</returns>
        public DepositSystem.Deposit GetFoodDeposit()
        {
            return organizationSystem != null ? organizationSystem.FoodDeposit : null;
        }
        
        /// <summary>
        /// Get the water deposit
        /// </summary>
        /// <returns>Water deposit, or null if none</returns>
        public DepositSystem.Deposit GetWaterDeposit()
        {
            return organizationSystem != null ? organizationSystem.WaterDeposit : null;
        }
        
        /// <summary>
        /// Check if Queen is requesting a specific resource
        /// </summary>
        /// <param name="type">Type of resource to check</param>
        /// <returns>True if requesting this resource</returns>
        public bool IsRequestingResource(DepositSystem.DepositType type)
        {
            return organizationSystem != null && organizationSystem.IsRequestingResource(type);
        }
        
        /// <summary>
        /// Update worker spawning logic
        /// </summary>
        private void UpdateWorkerSpawning()
        {
            if (workerPrefab == null || constants == null) return;
            
            // Check if we can spawn a worker
            if (currentWorkerCount >= constants.maxWorkers) return;
            
            // Check if enough time has passed since last spawn
            float timeSinceLastSpawn = Time.time - lastWorkerSpawnTime;
            float spawnInterval = constants.workerSpawnInterval;
            
            // Add initial delay for first spawn
            if (currentWorkerCount == 0)
            {
                spawnInterval += constants.initialSpawnDelay;
            }
            
            if (timeSinceLastSpawn >= spawnInterval)
            {
                SpawnWorker();
            }
        }
        
        /// <summary>
        /// Spawn a new worker
        /// </summary>
        private void SpawnWorker()
        {
            if (workerPrefab == null) return;
            
            // Calculate spawn position around the queen
            Vector3 spawnPosition = CalculateWorkerSpawnPosition();
            
            // Instantiate the worker
            GameObject workerObj = Instantiate(workerPrefab, spawnPosition, Quaternion.identity);
            
            // Add WorkerNPC component if it doesn't exist
            WorkerNPC workerNPC = workerObj.GetComponent<WorkerNPC>();
            if (workerNPC == null)
            {
                workerNPC = workerObj.AddComponent<WorkerNPC>();
            }
            
            // Update tracking
            currentWorkerCount++;
            lastWorkerSpawnTime = Time.time;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Spawned worker #{currentWorkerCount} at {spawnPosition}");
            }
        }
        
        /// <summary>
        /// Calculate spawn position for a new worker
        /// </summary>
        /// <returns>Position to spawn the worker</returns>
        private Vector3 CalculateWorkerSpawnPosition()
        {
            // Get spawn distance from constants
            float spawnDistance = constants.workerSpawnDistance;
            
            // Generate random position around the queen
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDistance;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Ensure spawn position is on the ground (same Y as queen)
            spawnPosition.y = transform.position.y;
            
            return spawnPosition;
        }
        
        /// <summary>
        /// Set the worker prefab for spawning
        /// </summary>
        /// <param name="prefab">Worker prefab to use for spawning</param>
        public void SetWorkerPrefab(GameObject prefab)
        {
            workerPrefab = prefab;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Worker prefab set to {(prefab != null ? prefab.name : "null")}");
            }
        }
        
        /// <summary>
        /// Enable or disable worker spawning
        /// </summary>
        /// <param name="enabled">Whether to enable worker spawning</param>
        public void SetWorkerSpawningEnabled(bool enabled)
        {
            enableWorkerSpawning = enabled;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Worker spawning {(enabled ? "enabled" : "disabled")}");
            }
        }
        
        /// <summary>
        /// Get current worker count
        /// </summary>
        /// <returns>Number of workers currently spawned</returns>
        public int GetWorkerCount()
        {
            return currentWorkerCount;
        }
        
        /// <summary>
        /// Called when a worker dies - allows queen to spawn replacements
        /// </summary>
        public void OnWorkerDied()
        {
            if (currentWorkerCount > 0)
            {
                currentWorkerCount--;
                
                if (IsDebugEnabled)
                {
                    Debug.Log($"{DebugName}: Worker died. Current count: {currentWorkerCount}");
                }
            }
        }
        
        
        private void OnDrawGizmos()
        {
            if (!IsDebugEnabled) return;
            
            // Draw origin position
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(originPosition, 0.5f);
            
            // Draw movement constraint
            if (!canMoveFromOrigin)
            {
                Gizmos.color = Color.magenta;
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                Gizmos.DrawSphere(originPosition, maxDistanceFromOrigin);
            }
            
            // Draw worker spawn area
            if (enableWorkerSpawning && constants != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, constants.workerSpawnDistance);
                
                // Draw worker count info
                Gizmos.color = Color.white;
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, $"Workers: {currentWorkerCount}/{constants.maxWorkers}");
                #endif
            }
        }
    }
}
