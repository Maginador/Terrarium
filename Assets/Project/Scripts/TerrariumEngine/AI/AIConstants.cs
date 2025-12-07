using UnityEngine;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Centralized constants and configuration for AI systems
    /// All AI behavior parameters are defined here for easy tuning
    /// </summary>
    [CreateAssetMenu(fileName = "AIConstants", menuName = "Terrarium/AI Constants")]
    public class AIConstants : ScriptableObject
    {
        [Header("Queen Configuration")]
        [Tooltip("Maximum distance Queen can move from origin")]
        public float queenMaxDistanceFromOrigin = 3f;
        
        [Tooltip("Distance at which Queen can consume food/water")]
        public float queenConsumptionRange = 2f;
        
        [Tooltip("Time between Queen consumption requests")]
        public float queenRequestInterval = 5f;
        
        [Header("Queen Worker Spawning")]
        [Tooltip("Time between worker spawns (in seconds)")]
        public float workerSpawnInterval = 10f;
        
        [Tooltip("Maximum number of workers the queen can spawn")]
        public int maxWorkers = 5;
        
        [Tooltip("Distance from queen to spawn workers")]
        public float workerSpawnDistance = 2f;
        
        [Tooltip("Initial delay before queen starts spawning workers")]
        public float initialSpawnDelay = 5f;
        
        [Header("Worker Transport System")]
        [Tooltip("Radius of deposit areas")]
        public float depositRadius = 3f;
        
        [Tooltip("Maximum sand levels to clear in deposit areas")]
        public int maxSandLevelsToClear = 2;
        
        [Tooltip("Distance at which workers can pick up items")]
        public float pickupRange = 2f;
        
        [Tooltip("Distance at which workers can drop items")]
        public float dropRange = 1.5f;
        
        [Tooltip("Speed multiplier when carrying items")]
        public float carryingSpeedMultiplier = 0.7f;
        
        [Header("Worker Consumption")]
        [Tooltip("Food increase per second during consumption")]
        public float foodConsumptionRate = 10f;
        
        [Tooltip("Water increase per second during consumption")]
        public float waterConsumptionRate = 10f;
        
        [Tooltip("Food percentage decrease per second during consumption")]
        public float foodPercentageDecrease = 10f;
        
        [Tooltip("Water percentage decrease per second during consumption")]
        public float waterPercentageDecrease = 10f;
        
        [Tooltip("Distance at which workers can consume items")]
        public float consumptionRange = 1f;
        
        [Header("Worker Combat System")]
        [Tooltip("Stress threshold below which workers become aggressive")]
        public float combatStressThreshold = 0.2f;
        
        [Tooltip("Health damage when attacked by another worker")]
        public float combatDamage = 10f;
        
        [Tooltip("Maximum health for workers")]
        public float maxWorkerHealth = 100f;
        
        [Tooltip("Stress reduction when a worker dies")]
        public float stressReductionOnDeath = 0.1f;
        
        [Tooltip("Distance at which workers can attack each other")]
        public float combatRange = 1.5f;
        
        [Header("Stress System")]
        [Tooltip("Stress increase interval when well-fed")]
        public float stressIncreaseInterval = 10f;
        
        [Tooltip("Food/water threshold for stress increase")]
        public float stressIncreaseThreshold = 0.5f;
        
        [Tooltip("Stress increase amount when well-fed")]
        public float stressIncreaseAmount = 0.05f;
        
        [Tooltip("Stress decrease when space is low")]
        public float stressDecreaseOnLowSpace = 0.1f;
        
        [Header("Fallback Behaviors")]
        [Tooltip("Chance to break a random sand block (0-1)")]
        public float randomSandBreakChance = 0.1f;
        
        [Tooltip("Chance to pick an existing hole for more breaking (0-1)")]
        public float existingHoleBreakChance = 0.8f;
        
        [Tooltip("Distance to search for existing holes")]
        public float holeSearchRange = 5f;
        
        [Tooltip("Time between fallback behavior checks")]
        public float fallbackCheckInterval = 2f;
        
        [Header("AI Behavior Priorities")]
        [Tooltip("Priority for combat behavior")]
        public int combatPriority = 100;
        
        [Tooltip("Priority for transport behavior")]
        public int transportPriority = 80;
        
        [Tooltip("Priority for consumption behavior")]
        public int consumptionPriority = 60;
        
        [Tooltip("Priority for pickup behavior")]
        public int pickupPriority = 40;
        
        [Tooltip("Priority for fallback behavior")]
        public int fallbackPriority = 20;
        
        [Header("Movement and Navigation")]
        [Tooltip("Base movement speed for workers")]
        public float baseMovementSpeed = 3f;
        
        [Tooltip("Rotation speed for workers")]
        public float rotationSpeed = 180f;
        
        [Tooltip("Stopping distance for navigation")]
        public float stoppingDistance = 0.5f;
        
        [Tooltip("Update interval for AI decisions")]
        public float aiUpdateInterval = 0.5f;
        
        [Header("Debug Settings")]
        [Tooltip("Show AI debug information")]
        public bool showAIDebug = true;
        
        [Tooltip("Show behavior state transitions")]
        public bool showBehaviorTransitions = true;
        
        [Tooltip("Show deposit areas")]
        public bool showDepositAreas = true;
        
        [Tooltip("Show combat ranges")]
        public bool showCombatRanges = true;
        
        // Singleton instance
        private static AIConstants _instance;
        public static AIConstants Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AIConstants>("AIConstants");
                    if (_instance == null)
                    {
                        Debug.LogWarning("AIConstants not found in Resources. Creating default instance.");
                        _instance = CreateInstance<AIConstants>();
                    }
                }
                return _instance;
            }
        }
        
        private void OnValidate()
        {
            // Ensure values are within valid ranges
            queenMaxDistanceFromOrigin = Mathf.Max(0.1f, queenMaxDistanceFromOrigin);
            workerSpawnInterval = Mathf.Max(1f, workerSpawnInterval);
            maxWorkers = Mathf.Max(0, maxWorkers);
            workerSpawnDistance = Mathf.Max(0.5f, workerSpawnDistance);
            initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
            depositRadius = Mathf.Max(0.5f, depositRadius);
            maxSandLevelsToClear = Mathf.Max(1, maxSandLevelsToClear);
            pickupRange = Mathf.Max(0.5f, pickupRange);
            dropRange = Mathf.Max(0.1f, dropRange);
            carryingSpeedMultiplier = Mathf.Clamp01(carryingSpeedMultiplier);
            
            foodConsumptionRate = Mathf.Max(0.1f, foodConsumptionRate);
            waterConsumptionRate = Mathf.Max(0.1f, waterConsumptionRate);
            foodPercentageDecrease = Mathf.Clamp01(foodPercentageDecrease);
            waterPercentageDecrease = Mathf.Clamp01(waterPercentageDecrease);
            
            combatStressThreshold = Mathf.Clamp01(combatStressThreshold);
            combatDamage = Mathf.Max(0.1f, combatDamage);
            maxWorkerHealth = Mathf.Max(1f, maxWorkerHealth);
            stressReductionOnDeath = Mathf.Clamp01(stressReductionOnDeath);
            combatRange = Mathf.Max(0.5f, combatRange);
            
            stressIncreaseInterval = Mathf.Max(0.1f, stressIncreaseInterval);
            stressIncreaseThreshold = Mathf.Clamp01(stressIncreaseThreshold);
            stressIncreaseAmount = Mathf.Clamp01(stressIncreaseAmount);
            stressDecreaseOnLowSpace = Mathf.Clamp01(stressDecreaseOnLowSpace);
            
            randomSandBreakChance = Mathf.Clamp01(randomSandBreakChance);
            existingHoleBreakChance = Mathf.Clamp01(existingHoleBreakChance);
            holeSearchRange = Mathf.Max(1f, holeSearchRange);
            fallbackCheckInterval = Mathf.Max(0.1f, fallbackCheckInterval);
            
            baseMovementSpeed = Mathf.Max(0.1f, baseMovementSpeed);
            rotationSpeed = Mathf.Max(1f, rotationSpeed);
            stoppingDistance = Mathf.Max(0.1f, stoppingDistance);
            aiUpdateInterval = Mathf.Max(0.1f, aiUpdateInterval);
        }
    }
}
