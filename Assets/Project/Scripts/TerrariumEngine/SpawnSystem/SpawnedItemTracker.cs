using UnityEngine;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Component that tracks spawned items and notifies the SpawnManager when they are destroyed
    /// </summary>
    public class SpawnedItemTracker : MonoBehaviour
    {
        private SpawnManager spawnManager;
        private SpawnCategory category;
        private bool isInitialized = false;
        
        /// <summary>
        /// Initialize the tracker
        /// </summary>
        /// <param name="manager">Spawn manager to notify</param>
        /// <param name="itemCategory">Category of the spawned item</param>
        public void Initialize(SpawnManager manager, SpawnCategory itemCategory)
        {
            spawnManager = manager;
            category = itemCategory;
            isInitialized = true;
        }
        
        private void OnDestroy()
        {
            if (isInitialized && spawnManager != null)
            {
                spawnManager.OnSpawnedItemDestroyed(gameObject, category);
            }
        }
        
        private void OnDisable()
        {
            // Also notify when disabled (in case object is disabled instead of destroyed)
            if (isInitialized && spawnManager != null)
            {
                spawnManager.OnSpawnedItemDestroyed(gameObject, category);
            }
        }
    }
}

