using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrariumEngine.AI;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Main spawn manager for the terrarium system
    /// Handles both automatic and manual spawning of Water, Food, and Entities
    /// </summary>
    public class SpawnManager : MonoBehaviour, IDebuggable
    {
        [Header("Spawn Categories")]
        [SerializeField] private SpawnCategoryConfig waterConfig = new SpawnCategoryConfig(SpawnCategory.Water, "Water");
        [SerializeField] private SpawnCategoryConfig foodConfig = new SpawnCategoryConfig(SpawnCategory.Food, "Food");
        [SerializeField] private SpawnCategoryConfig entitiesConfig = new SpawnCategoryConfig(SpawnCategory.Entities, "Entities");
        
        [Header("Spawn Settings")]
        [SerializeField] private bool enableAutoSpawn = true;
        [SerializeField] private bool spawnOnTopOfTerrarium = true;
        [SerializeField] private float spawnAreaMargin = 1f; // Margin from terrarium edges
        
        [Header("Manual Spawn Settings")]
        [SerializeField] private bool enableManualSpawn = true;
        [SerializeField] private LayerMask spawnLayerMask = -1; // What layers can be spawned on
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showSpawnGizmos = true;
        
        public string DebugName => "SpawnManager";
        public bool IsDebugEnabled { get; set; } = true;
        
        // References
        private TerrariumManager terrariumManager;
        private Camera playerCamera;
        
        // Spawn tracking
        private Dictionary<SpawnCategory, List<GameObject>> spawnedItems = new Dictionary<SpawnCategory, List<GameObject>>();
        private Dictionary<SpawnCategory, float> nextSpawnTimes = new Dictionary<SpawnCategory, float>();
        
        // Events
        public System.Action<SpawnCategory, GameObject, Vector3> OnItemSpawned;
        public System.Action<SpawnCategory, GameObject> OnItemDestroyed;
        public System.Action<SpawnCategory, SpawnableItem> OnManualSpawnRequested;
        
        // Properties
        public SpawnCategoryConfig WaterConfig => waterConfig;
        public SpawnCategoryConfig FoodConfig => foodConfig;
        public SpawnCategoryConfig EntitiesConfig => entitiesConfig;
        public bool EnableAutoSpawn { get => enableAutoSpawn; set => enableAutoSpawn = value; }
        public bool EnableManualSpawn { get => enableManualSpawn; set => enableManualSpawn = value; }
        
        private void Awake()
        {
            // Initialize spawn tracking
            spawnedItems[SpawnCategory.Water] = new List<GameObject>();
            spawnedItems[SpawnCategory.Food] = new List<GameObject>();
            spawnedItems[SpawnCategory.Entities] = new List<GameObject>();
            
            nextSpawnTimes[SpawnCategory.Water] = 0f;
            nextSpawnTimes[SpawnCategory.Food] = 0f;
            nextSpawnTimes[SpawnCategory.Entities] = 0f;
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void Start()
        {
            // Find required components
            terrariumManager = FindFirstObjectByType<TerrariumManager>();
            if (terrariumManager == null)
            {
                Debug.LogError($"{DebugName}: TerrariumManager not found!");
                enabled = false;
                return;
            }
            
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
            
            // Initialize next spawn times
            if (enableAutoSpawn)
            {
                InitializeSpawnTimes();
            }
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        private void Update()
        {
            if (enableAutoSpawn)
            {
                UpdateAutoSpawn();
            }
            
            // Clean up destroyed items from tracking lists
            CleanupDestroyedItems();
        }
        
        /// <summary>
        /// Initialize spawn times for all categories
        /// </summary>
        private void InitializeSpawnTimes()
        {
            if (waterConfig.isEnabled && waterConfig.autoSpawnConfig.IsEnabled)
            {
                nextSpawnTimes[SpawnCategory.Water] = Time.time + waterConfig.autoSpawnConfig.GetNextSpawnTime();
            }
            
            if (foodConfig.isEnabled && foodConfig.autoSpawnConfig.IsEnabled)
            {
                nextSpawnTimes[SpawnCategory.Food] = Time.time + foodConfig.autoSpawnConfig.GetNextSpawnTime();
            }
            
            if (entitiesConfig.isEnabled && entitiesConfig.autoSpawnConfig.IsEnabled)
            {
                nextSpawnTimes[SpawnCategory.Entities] = Time.time + entitiesConfig.autoSpawnConfig.GetNextSpawnTime();
            }
        }
        
        /// <summary>
        /// Update automatic spawning
        /// </summary>
        private void UpdateAutoSpawn()
        {
            float currentTime = Time.time;
            
            // Check water spawning
            if (waterConfig.isEnabled && waterConfig.autoSpawnConfig.IsEnabled && 
                currentTime >= nextSpawnTimes[SpawnCategory.Water])
            {
                if (CanSpawnItem(SpawnCategory.Water))
                {
                    SpawnRandomItem(SpawnCategory.Water);
                }
                nextSpawnTimes[SpawnCategory.Water] = currentTime + waterConfig.autoSpawnConfig.GetNextSpawnTime();
            }
            
            // Check food spawning
            if (foodConfig.isEnabled && foodConfig.autoSpawnConfig.IsEnabled && 
                currentTime >= nextSpawnTimes[SpawnCategory.Food])
            {
                if (CanSpawnItem(SpawnCategory.Food))
                {
                    SpawnRandomItem(SpawnCategory.Food);
                }
                nextSpawnTimes[SpawnCategory.Food] = currentTime + foodConfig.autoSpawnConfig.GetNextSpawnTime();
            }
            
            // Check entities spawning
            if (entitiesConfig.isEnabled && entitiesConfig.autoSpawnConfig.IsEnabled && 
                currentTime >= nextSpawnTimes[SpawnCategory.Entities])
            {
                if (CanSpawnItem(SpawnCategory.Entities))
                {
                    SpawnRandomItem(SpawnCategory.Entities);
                }
                nextSpawnTimes[SpawnCategory.Entities] = currentTime + entitiesConfig.autoSpawnConfig.GetNextSpawnTime();
            }
        }
        
        /// <summary>
        /// Check if an item can be spawned (respects limits and distances)
        /// </summary>
        /// <param name="category">Spawn category</param>
        /// <returns>True if item can be spawned</returns>
        private bool CanSpawnItem(SpawnCategory category)
        {
            var config = GetCategoryConfig(category);
            if (config == null) return false;
            
            // Check max items limit
            if (spawnedItems[category].Count >= config.autoSpawnConfig.maxItems)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Spawn a random item from the specified category
        /// </summary>
        /// <param name="category">Category to spawn from</param>
        /// <returns>Spawned GameObject or null if failed</returns>
        public GameObject SpawnRandomItem(SpawnCategory category)
        {
            var config = GetCategoryConfig(category);
            if (config == null) return null;
            
            var spawnableItem = config.GetRandomSpawnableItem();
            if (spawnableItem == null) return null;
            
            Vector3 spawnPosition = GetRandomSpawnPosition(category);
            if (spawnPosition == Vector3.zero) return null;
            
            return SpawnItem(spawnableItem, spawnPosition, category);
        }
        
        /// <summary>
        /// Spawn a specific item at a specific position
        /// </summary>
        /// <param name="spawnableItem">Item to spawn</param>
        /// <param name="position">Position to spawn at</param>
        /// <param name="category">Category of the item</param>
        /// <returns>Spawned GameObject or null if failed</returns>
        public GameObject SpawnItem(SpawnableItem spawnableItem, Vector3 position, SpawnCategory category)
        {
            if (spawnableItem == null || spawnableItem.prefab == null)
            {
                Debug.LogWarning($"{DebugName}: Cannot spawn null item or prefab");
                return null;
            }
            
            // Apply spawn offset
            Vector3 finalPosition = position + spawnableItem.spawnOffset;
            
            // Instantiate the item
            GameObject spawnedObject = Instantiate(spawnableItem.prefab, finalPosition, Quaternion.identity);
            
            // Apply random rotation if enabled
            if (spawnableItem.randomizeRotation)
            {
                Vector3 randomRotation = new Vector3(
                    Random.Range(-spawnableItem.randomRotationRange.x, spawnableItem.randomRotationRange.x),
                    Random.Range(-spawnableItem.randomRotationRange.y, spawnableItem.randomRotationRange.y),
                    Random.Range(-spawnableItem.randomRotationRange.z, spawnableItem.randomRotationRange.z)
                );
                spawnedObject.transform.rotation = Quaternion.Euler(randomRotation);
            }
            
            // Add to tracking
            spawnedItems[category].Add(spawnedObject);
            
            // Set up destruction tracking
            SetupDestructionTracking(spawnedObject, category);
            
            // Fire event
            OnItemSpawned?.Invoke(category, spawnedObject, finalPosition);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Spawned {spawnableItem.itemName} at {finalPosition}");
            }
            
            return spawnedObject;
        }
        
        /// <summary>
        /// Manual spawn API - spawn item at mouse position
        /// </summary>
        /// <param name="category">Category to spawn from</param>
        /// <param name="itemName">Name of specific item to spawn (null for random)</param>
        /// <returns>True if spawn was successful</returns>
        public bool ManualSpawnAtMouse(SpawnCategory category, string itemName = null)
        {
            if (!enableManualSpawn)
            {
                Debug.LogWarning($"{DebugName}: Manual spawn is disabled");
                return false;
            }
            
            Vector3 mousePosition = GetMouseWorldPosition();
            if (mousePosition == Vector3.zero)
            {
                Debug.LogWarning($"{DebugName}: Could not get valid mouse position for spawning");
                return false;
            }
            
            return ManualSpawnAtPosition(category, mousePosition, itemName);
        }
        
        /// <summary>
        /// Manual spawn API - spawn item at specific position
        /// </summary>
        /// <param name="category">Category to spawn from</param>
        /// <param name="position">Position to spawn at</param>
        /// <param name="itemName">Name of specific item to spawn (null for random)</param>
        /// <returns>True if spawn was successful</returns>
        public bool ManualSpawnAtPosition(SpawnCategory category, Vector3 position, string itemName = null)
        {
            if (!enableManualSpawn)
            {
                Debug.LogWarning($"{DebugName}: Manual spawn is disabled");
                return false;
            }
            
            var config = GetCategoryConfig(category);
            if (config == null || !config.isEnabled)
            {
                Debug.LogWarning($"{DebugName}: Category {category} is not enabled");
                return false;
            }
            
            SpawnableItem spawnableItem;
            
            if (string.IsNullOrEmpty(itemName))
            {
                spawnableItem = config.GetRandomSpawnableItem();
            }
            else
            {
                spawnableItem = config.GetSpawnableItem(itemName);
            }
            
            if (spawnableItem == null)
            {
                Debug.LogWarning($"{DebugName}: Could not find spawnable item '{itemName}' in category {category}");
                return false;
            }
            
            GameObject spawnedObject = SpawnItem(spawnableItem, position, category);
            
            if (spawnedObject != null)
            {
                OnManualSpawnRequested?.Invoke(category, spawnableItem);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get a random spawn position within the terrarium bounds
        /// </summary>
        /// <param name="category">Category for spawn position calculation</param>
        /// <returns>Random spawn position or Vector3.zero if invalid</returns>
        private Vector3 GetRandomSpawnPosition(SpawnCategory category)
        {
            if (terrariumManager == null) return Vector3.zero;
            
            var config = GetCategoryConfig(category);
            if (config == null) return Vector3.zero;
            
            Vector3 terrariumSize = terrariumManager.TerrariumSize;
            Vector3 terrariumCenter = terrariumManager.transform.position + terrariumSize * 0.5f;
            
            // Calculate spawn area (with margin from edges)
            float spawnAreaX = terrariumSize.x - (spawnAreaMargin * 2f);
            float spawnAreaZ = terrariumSize.z - (spawnAreaMargin * 2f);
            
            // Generate random position within spawn area
            Vector3 randomPosition = new Vector3(
                terrariumCenter.x + Random.Range(-spawnAreaX * 0.5f, spawnAreaX * 0.5f),
                0f, // Will be set based on spawn method
                terrariumCenter.z + Random.Range(-spawnAreaZ * 0.5f, spawnAreaZ * 0.5f)
            );
            
            if (spawnOnTopOfTerrarium)
            {
                // Find the highest point at this X,Z position
                Vector3Int gridPos = terrariumManager.WorldToGridPosition(randomPosition);
                Vector3 highestPosition = terrariumManager.GetHighestBlockPosition(gridPos.x, gridPos.z);
                
                if (highestPosition != Vector3.zero)
                {
                    randomPosition.y = highestPosition.y + config.autoSpawnConfig.spawnHeight;
                }
                else
                {
                    // Fallback to terrarium base + spawn height
                    randomPosition.y = terrariumManager.transform.position.y + config.autoSpawnConfig.spawnHeight;
                }
            }
            else
            {
                // Spawn at terrarium base + spawn height
                randomPosition.y = terrariumManager.transform.position.y + config.autoSpawnConfig.spawnHeight;
            }
            
            // Add height variation
            randomPosition.y += Random.Range(-config.autoSpawnConfig.heightVariation, config.autoSpawnConfig.heightVariation);
            
            return randomPosition;
        }
        
        /// <summary>
        /// Get mouse world position for manual spawning
        /// </summary>
        /// <returns>World position or Vector3.zero if invalid</returns>
        private Vector3 GetMouseWorldPosition()
        {
            if (playerCamera == null) return Vector3.zero;
            
            Ray ray = playerCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, spawnLayerMask))
            {
                return hit.point;
            }
            
            // Fallback: project onto terrarium plane
            if (terrariumManager != null)
            {
                Plane terrariumPlane = new Plane(Vector3.up, terrariumManager.transform.position);
                if (terrariumPlane.Raycast(ray, out float distance))
                {
                    return ray.GetPoint(distance);
                }
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Get category configuration
        /// </summary>
        /// <param name="category">Category to get config for</param>
        /// <returns>Category configuration or null</returns>
        private SpawnCategoryConfig GetCategoryConfig(SpawnCategory category)
        {
            switch (category)
            {
                case SpawnCategory.Water:
                    return waterConfig;
                case SpawnCategory.Food:
                    return foodConfig;
                case SpawnCategory.Entities:
                    return entitiesConfig;
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// Setup destruction tracking for spawned items
        /// </summary>
        /// <param name="spawnedObject">Object to track</param>
        /// <param name="category">Category of the object</param>
        private void SetupDestructionTracking(GameObject spawnedObject, SpawnCategory category)
        {
            // Add a component to track destruction
            var tracker = spawnedObject.GetComponent<SpawnedItemTracker>();
            if (tracker == null)
            {
                tracker = spawnedObject.AddComponent<SpawnedItemTracker>();
            }
            
            tracker.Initialize(this, category);
        }
        
        /// <summary>
        /// Clean up destroyed items from tracking lists
        /// </summary>
        private void CleanupDestroyedItems()
        {
            foreach (var category in spawnedItems.Keys)
            {
                spawnedItems[category].RemoveAll(item => item == null);
            }
        }
        
        /// <summary>
        /// Called when a spawned item is destroyed
        /// </summary>
        /// <param name="destroyedObject">Destroyed object</param>
        /// <param name="category">Category of the object</param>
        public void OnSpawnedItemDestroyed(GameObject destroyedObject, SpawnCategory category)
        {
            if (spawnedItems.ContainsKey(category))
            {
                spawnedItems[category].Remove(destroyedObject);
            }
            
            OnItemDestroyed?.Invoke(category, destroyedObject);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: {category} item destroyed");
            }
        }
        
        /// <summary>
        /// Get count of spawned items for a category
        /// </summary>
        /// <param name="category">Category to count</param>
        /// <returns>Number of spawned items</returns>
        public int GetSpawnedItemCount(SpawnCategory category)
        {
            if (spawnedItems.ContainsKey(category))
            {
                return spawnedItems[category].Count;
            }
            return 0;
        }
        
        /// <summary>
        /// Clear all spawned items of a category
        /// </summary>
        /// <param name="category">Category to clear</param>
        public void ClearSpawnedItems(SpawnCategory category)
        {
            if (spawnedItems.ContainsKey(category))
            {
                foreach (var item in spawnedItems[category])
                {
                    if (item != null)
                    {
                        Destroy(item);
                    }
                }
                spawnedItems[category].Clear();
            }
        }
        
        /// <summary>
        /// Clear all spawned items
        /// </summary>
        public void ClearAllSpawnedItems()
        {
            ClearSpawnedItems(SpawnCategory.Water);
            ClearSpawnedItems(SpawnCategory.Food);
            ClearSpawnedItems(SpawnCategory.Entities);
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            GUILayout.BeginArea(new Rect(10, 380, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Spawn Manager", GUI.skin.box);
            GUILayout.Space(5);
            
            GUILayout.Label($"Auto Spawn: {(enableAutoSpawn ? "ON" : "OFF")}", GUI.skin.label);
            GUILayout.Label($"Manual Spawn: {(enableManualSpawn ? "ON" : "OFF")}", GUI.skin.label);
            GUILayout.Space(5);
            
            GUILayout.Label($"Water Items: {GetSpawnedItemCount(SpawnCategory.Water)}", GUI.skin.label);
            GUILayout.Label($"Food Items: {GetSpawnedItemCount(SpawnCategory.Food)}", GUI.skin.label);
            GUILayout.Label($"Entities: {GetSpawnedItemCount(SpawnCategory.Entities)}", GUI.skin.label);
            GUILayout.Space(5);
            
            // Show next spawn times
            if (enableAutoSpawn)
            {
                GUILayout.Label("Next Spawns:", GUI.skin.label);
                foreach (var kvp in nextSpawnTimes)
                {
                    float timeLeft = kvp.Value - Time.time;
                    if (timeLeft > 0)
                    {
                        GUILayout.Label($"  {kvp.Key}: {timeLeft:F1}s", GUI.skin.label);
                    }
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnDrawGizmos()
        {
            if (!showSpawnGizmos || !IsDebugEnabled || terrariumManager == null) return;
            
            // Draw spawn area
            Gizmos.color = Color.cyan;
            Vector3 terrariumSize = terrariumManager.TerrariumSize;
            Vector3 spawnAreaSize = new Vector3(
                terrariumSize.x - (spawnAreaMargin * 2f),
                terrariumSize.y,
                terrariumSize.z - (spawnAreaMargin * 2f)
            );
            Gizmos.DrawWireCube(terrariumManager.transform.position + terrariumSize * 0.5f, spawnAreaSize);
            
            // Draw spawned items
            foreach (var category in spawnedItems.Keys)
            {
                Color gizmoColor = category == SpawnCategory.Water ? Color.blue : 
                                 category == SpawnCategory.Food ? Color.green : Color.red;
                
                Gizmos.color = gizmoColor;
                foreach (var item in spawnedItems[category])
                {
                    if (item != null)
                    {
                        Gizmos.DrawWireSphere(item.transform.position, 0.5f);
                    }
                }
            }
        }
    }
}
