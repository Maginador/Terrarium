using UnityEngine;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Helper script to set up the spawn system with default configurations
    /// This can be attached to a GameObject to automatically configure the SpawnManager
    /// </summary>
    public class SpawnSystemSetup : MonoBehaviour
    {
        [Header("Setup Configuration")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createDefaultPrefabs = true;
        
        [Header("Default Spawn Settings")]
        [SerializeField] private float defaultSpawnInterval = 10f;
        [SerializeField] private int defaultMaxItems = 5;
        [SerializeField] private float defaultSpawnHeight = 2f;
        
        private SpawnManager spawnManager;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupSpawnSystem();
            }
        }
        
        /// <summary>
        /// Set up the spawn system with default configurations
        /// </summary>
        public void SetupSpawnSystem()
        {
            // Find or create SpawnManager
            spawnManager = FindFirstObjectByType<SpawnManager>();
            if (spawnManager == null)
            {
                GameObject spawnManagerObj = new GameObject("SpawnManager");
                spawnManager = spawnManagerObj.AddComponent<SpawnManager>();
            }
            
            // Configure default settings
            ConfigureDefaultSettings();
            
            // Create default prefabs if requested
            if (createDefaultPrefabs)
            {
                CreateDefaultPrefabs();
            }
            
            Debug.Log("SpawnSystemSetup: Spawn system configured successfully");
        }
        
        /// <summary>
        /// Configure default spawn settings
        /// </summary>
        private void ConfigureDefaultSettings()
        {
            // Configure water spawning
            spawnManager.WaterConfig.autoSpawnConfig.spawnInterval = defaultSpawnInterval;
            spawnManager.WaterConfig.autoSpawnConfig.maxItems = defaultMaxItems;
            spawnManager.WaterConfig.autoSpawnConfig.spawnHeight = defaultSpawnHeight;
            spawnManager.WaterConfig.isEnabled = true;
            
            // Configure food spawning
            spawnManager.FoodConfig.autoSpawnConfig.spawnInterval = defaultSpawnInterval * 1.5f; // Slightly less frequent
            spawnManager.FoodConfig.autoSpawnConfig.maxItems = defaultMaxItems;
            spawnManager.FoodConfig.autoSpawnConfig.spawnHeight = defaultSpawnHeight;
            spawnManager.FoodConfig.isEnabled = true;
            
            // Configure entities spawning (less frequent)
            spawnManager.EntitiesConfig.autoSpawnConfig.spawnInterval = defaultSpawnInterval * 3f;
            spawnManager.EntitiesConfig.autoSpawnConfig.maxItems = defaultMaxItems / 2;
            spawnManager.EntitiesConfig.autoSpawnConfig.spawnHeight = defaultSpawnHeight;
            spawnManager.EntitiesConfig.isEnabled = true;
            
            // Enable auto spawn
            spawnManager.EnableAutoSpawn = true;
            spawnManager.EnableManualSpawn = true;
        }
        
        /// <summary>
        /// Create default prefabs for testing
        /// </summary>
        private void CreateDefaultPrefabs()
        {
            // Create default water prefab
            GameObject waterPrefab = CreateWaterPrefab();
            spawnManager.WaterConfig.spawnableItems.Add(new SpawnableItem(waterPrefab, "Water Drop", 1f));
            
            // Create default food prefab
            GameObject foodPrefab = CreateFoodPrefab();
            spawnManager.FoodConfig.spawnableItems.Add(new SpawnableItem(foodPrefab, "Food Pellet", 1f));
            
            // Create default entity prefab (simple cube for now)
            GameObject entityPrefab = CreateEntityPrefab();
            spawnManager.EntitiesConfig.spawnableItems.Add(new SpawnableItem(entityPrefab, "Small Entity", 1f));
            
            Debug.Log("SpawnSystemSetup: Default prefabs created");
        }
        
        /// <summary>
        /// Create a default water prefab
        /// </summary>
        /// <returns>Water prefab GameObject</returns>
        private GameObject CreateWaterPrefab()
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            water.name = "WaterPrefab";
            water.transform.localScale = Vector3.one * 0.5f;
            
            // Set water color and material
            Renderer renderer = water.GetComponent<Renderer>();
            Material waterMat = new Material(Shader.Find("Standard"));
            waterMat.color = new Color(0.2f, 0.6f, 1f, 0.8f); // Blue with transparency
            waterMat.SetFloat("_Mode", 3); // Transparent mode
            waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMat.SetInt("_ZWrite", 0);
            waterMat.DisableKeyword("_ALPHATEST_ON");
            waterMat.EnableKeyword("_ALPHABLEND_ON");
            waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMat.renderQueue = 3000;
            renderer.material = waterMat;
            
            // Add water component
            WaterItem waterItem = water.AddComponent<WaterItem>();
            
            // Add rigidbody for physics
            Rigidbody rb = water.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.linearDamping = 1f;
            rb.angularDamping = 2f;
            
            // Make collider a trigger for easy consumption
            Collider collider = water.GetComponent<Collider>();
            collider.isTrigger = true;
            
            return water;
        }
        
        /// <summary>
        /// Create a default food prefab
        /// </summary>
        /// <returns>Food prefab GameObject</returns>
        private GameObject CreateFoodPrefab()
        {
            GameObject food = GameObject.CreatePrimitive(PrimitiveType.Cube);
            food.name = "FoodPrefab";
            food.transform.localScale = Vector3.one * 0.3f;
            
            // Set food color
            Renderer renderer = food.GetComponent<Renderer>();
            renderer.material.color = Color.green;
            
            // Add food component
            FoodItem foodItem = food.AddComponent<FoodItem>();
            
            // Add rigidbody for physics
            Rigidbody rb = food.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.linearDamping = 1f;
            rb.angularDamping = 2f;
            
            // Make collider a trigger for easy consumption
            Collider collider = food.GetComponent<Collider>();
            collider.isTrigger = true;
            
            return food;
        }
        
        /// <summary>
        /// Create a default entity prefab
        /// </summary>
        /// <returns>Entity prefab GameObject</returns>
        private GameObject CreateEntityPrefab()
        {
            GameObject entity = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            entity.name = "EntityPrefab";
            entity.transform.localScale = Vector3.one * 0.4f;
            
            // Set entity color
            Renderer renderer = entity.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;
            
            // Add rigidbody for physics
            Rigidbody rb = entity.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1f;
            
            return entity;
        }
        
        /// <summary>
        /// Test manual spawning at mouse position
        /// </summary>
        /// <param name="category">Category to spawn</param>
        public void TestManualSpawn(SpawnCategory category)
        {
            if (spawnManager != null)
            {
                bool success = spawnManager.ManualSpawnAtMouse(category);
                Debug.Log($"Manual spawn test for {category}: {(success ? "Success" : "Failed")}");
            }
        }
        
        /// <summary>
        /// Test manual spawning at mouse position with specific item
        /// </summary>
        /// <param name="category">Category to spawn from</param>
        /// <param name="itemName">Name of specific item to spawn</param>
        public void TestManualSpawnSpecific(SpawnCategory category, string itemName)
        {
            if (spawnManager != null)
            {
                bool success = spawnManager.ManualSpawnAtMouse(category, itemName);
                Debug.Log($"Manual spawn test for {category} item '{itemName}': {(success ? "Success" : "Failed")}");
            }
        }
        
        /// <summary>
        /// Clear all spawned items
        /// </summary>
        public void ClearAllSpawnedItems()
        {
            if (spawnManager != null)
            {
                spawnManager.ClearAllSpawnedItems();
                Debug.Log("All spawned items cleared");
            }
        }
        
        /// <summary>
        /// Toggle auto spawn
        /// </summary>
        public void ToggleAutoSpawn()
        {
            if (spawnManager != null)
            {
                spawnManager.EnableAutoSpawn = !spawnManager.EnableAutoSpawn;
                Debug.Log($"Auto spawn: {(spawnManager.EnableAutoSpawn ? "Enabled" : "Disabled")}");
            }
        }
        
        /// <summary>
        /// Toggle manual spawn
        /// </summary>
        public void ToggleManualSpawn()
        {
            if (spawnManager != null)
            {
                spawnManager.EnableManualSpawn = !spawnManager.EnableManualSpawn;
                Debug.Log($"Manual spawn: {(spawnManager.EnableManualSpawn ? "Enabled" : "Disabled")}");
            }
        }
    }
}

