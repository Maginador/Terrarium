using System.Collections.Generic;
using UnityEngine;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Data structure for spawnable items
    /// </summary>
    [System.Serializable]
    public class SpawnableItem
    {
        [Header("Item Settings")]
        public GameObject prefab;
        public string itemName = "Item";
        public float spawnWeight = 1f; // Higher weight = more likely to be selected
        public bool isEnabled = true;
        
        [Header("Spawn Settings")]
        public Vector3 spawnOffset = Vector3.zero;
        public Vector3 randomRotationRange = Vector3.zero;
        public bool randomizeRotation = false;
        
        public SpawnableItem()
        {
            itemName = "New Item";
            spawnWeight = 1f;
            isEnabled = true;
        }
        
        public SpawnableItem(GameObject prefab, string name = "Item", float weight = 1f)
        {
            this.prefab = prefab;
            this.itemName = name;
            this.spawnWeight = weight;
            this.isEnabled = true;
        }
    }
    
    /// <summary>
    /// Configuration for automatic spawning
    /// </summary>
    [System.Serializable]
    public class AutoSpawnConfig
    {
        [Header("Spawn Timing")]
        [Tooltip("Time interval between spawns (0 or negative = disabled)")]
        public float spawnInterval = 5f;
        
        [Tooltip("Random variation in spawn time (±spawnInterval * this value)")]
        [Range(0f, 1f)]
        public float timeVariation = 0.2f;
        
        [Header("Spawn Limits")]
        [Tooltip("Maximum number of items of this type in the terrarium")]
        public int maxItems = 10;
        
        [Tooltip("Minimum distance between spawned items")]
        public float minSpawnDistance = 2f;
        
        [Header("Spawn Area")]
        [Tooltip("Height above terrarium surface to spawn items")]
        public float spawnHeight = 2f;
        
        [Tooltip("Random height variation")]
        public float heightVariation = 1f;
        
        public bool IsEnabled => spawnInterval > 0f;
        
        public float GetNextSpawnTime()
        {
            if (!IsEnabled) return float.MaxValue;
            
            float variation = Random.Range(-timeVariation, timeVariation);
            return spawnInterval * (1f + variation);
        }
    }
    
    /// <summary>
    /// Spawn category types
    /// </summary>
    public enum SpawnCategory
    {
        Water,
        Food,
        Entities
    }
    
    /// <summary>
    /// Configuration for a spawn category
    /// </summary>
    [System.Serializable]
    public class SpawnCategoryConfig
    {
        [Header("Category Settings")]
        public SpawnCategory category;
        public string categoryName = "Category";
        public bool isEnabled = true;
        
        [Header("Spawnable Items")]
        public List<SpawnableItem> spawnableItems = new List<SpawnableItem>();
        
        [Header("Auto Spawn Configuration")]
        public AutoSpawnConfig autoSpawnConfig = new AutoSpawnConfig();
        
        [Header("Debug")]
        public bool showDebugInfo = true;
        
        public SpawnCategoryConfig()
        {
            spawnableItems = new List<SpawnableItem>();
            autoSpawnConfig = new AutoSpawnConfig();
        }
        
        public SpawnCategoryConfig(SpawnCategory category, string name = "Category")
        {
            this.category = category;
            this.categoryName = name;
            this.isEnabled = true;
            this.spawnableItems = new List<SpawnableItem>();
            this.autoSpawnConfig = new AutoSpawnConfig();
        }
        
        /// <summary>
        /// Get a random spawnable item based on weights
        /// </summary>
        /// <returns>Random spawnable item or null if none available</returns>
        public SpawnableItem GetRandomSpawnableItem()
        {
            if (spawnableItems == null || spawnableItems.Count == 0)
                return null;
            
            // Filter enabled items
            var enabledItems = new List<SpawnableItem>();
            float totalWeight = 0f;
            
            foreach (var item in spawnableItems)
            {
                if (item != null && item.isEnabled && item.prefab != null)
                {
                    enabledItems.Add(item);
                    totalWeight += item.spawnWeight;
                }
            }
            
            if (enabledItems.Count == 0)
                return null;
            
            // Select based on weight
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var item in enabledItems)
            {
                currentWeight += item.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return item;
                }
            }
            
            // Fallback to last item
            return enabledItems[enabledItems.Count - 1];
        }
        
        /// <summary>
        /// Get spawnable item by name
        /// </summary>
        /// <param name="itemName">Name of the item</param>
        /// <returns>Spawnable item or null if not found</returns>
        public SpawnableItem GetSpawnableItem(string itemName)
        {
            if (spawnableItems == null) return null;
            
            foreach (var item in spawnableItems)
            {
                if (item != null && item.itemName == itemName && item.isEnabled && item.prefab != null)
                {
                    return item;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all enabled spawnable items
        /// </summary>
        /// <returns>List of enabled spawnable items</returns>
        public List<SpawnableItem> GetEnabledItems()
        {
            var enabledItems = new List<SpawnableItem>();
            
            if (spawnableItems != null)
            {
                foreach (var item in spawnableItems)
                {
                    if (item != null && item.isEnabled && item.prefab != null)
                    {
                        enabledItems.Add(item);
                    }
                }
            }
            
            return enabledItems;
        }
    }
}

