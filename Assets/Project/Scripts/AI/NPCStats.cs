using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Base stats system for NPCs with comprehensive stat management
    /// </summary>
    [System.Serializable]
    public class NPCStats
    {
        [Header("Legacy Health (Deprecated - use Stat system)")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        
        [Header("Movement")]
        public float moveSpeed = 2f;
        public float rotationSpeed = 90f;
        
        [Header("Behavior")]
        public float detectionRange = 5f;
        public float actionCooldown = 1f;
        
        [Header("Lifespan")]
        public float maxLifespan = 300f; // 5 minutes at 1x speed
        public float currentAge = 0f;
        
        [Header("Entity Stats")]
        [SerializeField] private List<Stat> entityStats = new List<Stat>();
        [SerializeField] private List<StatDefinition> statDefinitions = new List<StatDefinition>();
        
        // Death condition settings
        [Header("Death Conditions")]
        [Tooltip("Number of bad stats required to trigger death")]
        public int badStatsForDeath = 2;
        
        // Variation timers
        private float foodTimer = 0f;
        private float waterTimer = 0f;
        private float spaceTimer = 0f;
        
        // Events
        public System.Action<StatType, float> OnStatChanged;
        public System.Action<List<StatType>> OnDeathConditionMet;
        
        // Properties
        public bool IsAlive => GetStat(StatType.Health)?.CurrentValue > 0f && currentAge < maxLifespan;
        public float HealthPercentage => GetStat(StatType.Health)?.GetPercentage() ?? (currentHealth / maxHealth);
        public float AgePercentage => currentAge / maxLifespan;
        
        // Legacy compatibility
        public float CurrentHealth => GetStat(StatType.Health)?.CurrentValue ?? currentHealth;
        
        /// <summary>
        /// Initialize the stats system with default definitions
        /// </summary>
        public void InitializeStats()
        {
            if (statDefinitions.Count == 0)
            {
                CreateDefaultStatDefinitions();
            }
            
            entityStats.Clear();
            foreach (var definition in statDefinitions)
            {
                entityStats.Add(new Stat(definition));
            }
            
            // Reset timers
            foodTimer = 0f;
            waterTimer = 0f;
            spaceTimer = 0f;
        }
        
        /// <summary>
        /// Create default stat definitions based on requirements
        /// </summary>
        private void CreateDefaultStatDefinitions()
        {
            statDefinitions.Clear();
            
            // Health: 50-100 baseline, no automatic variation
            statDefinitions.Add(new StatDefinition
            {
                statType = StatType.Health,
                displayName = "Health",
                baselineMin = 50f,
                baselineMax = 100f,
                absoluteMin = 0f,
                absoluteMax = 100f,
                variationAmount = 0f,
                variationInterval = 0f
            });
            
            // Food: 50-100 baseline, -1 every 10 seconds
            statDefinitions.Add(new StatDefinition
            {
                statType = StatType.Food,
                displayName = "Food",
                baselineMin = 50f,
                baselineMax = 100f,
                absoluteMin = 0f,
                absoluteMax = 100f,
                variationAmount = -1f,
                variationInterval = 10f
            });
            
            // Water: 50-100 baseline, -1 every 5 seconds
            statDefinitions.Add(new StatDefinition
            {
                statType = StatType.Water,
                displayName = "Water",
                baselineMin = 50f,
                baselineMax = 100f,
                absoluteMin = 0f,
                absoluteMax = 100f,
                variationAmount = -1f,
                variationInterval = 5f
            });
            
            // Stress: 50-100 baseline, affected externally
            statDefinitions.Add(new StatDefinition
            {
                statType = StatType.Stress,
                displayName = "Stress",
                baselineMin = 50f,
                baselineMax = 100f,
                absoluteMin = 0f,
                absoluteMax = 100f,
                variationAmount = 0f,
                variationInterval = 0f,
                affectedByExternal = true
            });
            
            // Environment: 50-100 baseline, affected externally
            statDefinitions.Add(new StatDefinition
            {
                statType = StatType.Environment,
                displayName = "Environment",
                baselineMin = 50f,
                baselineMax = 100f,
                absoluteMin = 0f,
                absoluteMax = 100f,
                variationAmount = 0f,
                variationInterval = 0f,
                affectedByExternal = true
            });
            
            // Temperature: 50-100 baseline, affected externally
            statDefinitions.Add(new StatDefinition
            {
                statType = StatType.Temperature,
                displayName = "Temperature",
                baselineMin = 50f,
                baselineMax = 100f,
                absoluteMin = 0f,
                absoluteMax = 100f,
                variationAmount = 0f,
                variationInterval = 0f,
                affectedByExternal = true
            });
            
            // Space: 50-100 baseline, calculated every 20 seconds
            statDefinitions.Add(new StatDefinition
            {
                statType = StatType.Space,
                displayName = "Space",
                baselineMin = 50f,
                baselineMax = 100f,
                absoluteMin = 0f,
                absoluteMax = 100f,
                variationAmount = 0f,
                variationInterval = 20f
            });
        }
        
        /// <summary>
        /// Get a stat by type
        /// </summary>
        /// <param name="statType">Type of stat to get</param>
        /// <returns>Stat instance or null if not found</returns>
        public Stat GetStat(StatType statType)
        {
            return entityStats.FirstOrDefault(s => s.Type == statType);
        }
        
        /// <summary>
        /// Modify a stat value
        /// </summary>
        /// <param name="statType">Type of stat to modify</param>
        /// <param name="amount">Amount to change</param>
        public void ModifyStat(StatType statType, float amount)
        {
            var stat = GetStat(statType);
            if (stat != null)
            {
                float oldValue = stat.CurrentValue;
                stat.Modify(amount);
                OnStatChanged?.Invoke(statType, stat.CurrentValue);
                
                // Check death conditions
                CheckDeathConditions();
            }
        }
        
        /// <summary>
        /// Set a stat value directly
        /// </summary>
        /// <param name="statType">Type of stat to set</param>
        /// <param name="value">New value</param>
        public void SetStat(StatType statType, float value)
        {
            var stat = GetStat(statType);
            if (stat != null)
            {
                float oldValue = stat.CurrentValue;
                stat.SetValue(value);
                OnStatChanged?.Invoke(statType, stat.CurrentValue);
                
                // Check death conditions
                CheckDeathConditions();
            }
        }
        
        /// <summary>
        /// Update stats with time-based variations
        /// </summary>
        /// <param name="deltaTime">Time passed</param>
        public void UpdateStats(float deltaTime)
        {
            // Update timers
            foodTimer += deltaTime;
            waterTimer += deltaTime;
            spaceTimer += deltaTime;
            
            // Apply food variation (every 10 seconds)
            var foodStat = GetStat(StatType.Food);
            if (foodStat != null && foodTimer >= 10f)
            {
                foodStat.ApplyVariation();
                OnStatChanged?.Invoke(StatType.Food, foodStat.CurrentValue);
                foodTimer = 0f;
            }
            
            // Apply water variation (every 5 seconds)
            var waterStat = GetStat(StatType.Water);
            if (waterStat != null && waterTimer >= 5f)
            {
                waterStat.ApplyVariation();
                OnStatChanged?.Invoke(StatType.Water, waterStat.CurrentValue);
                waterTimer = 0f;
            }
            
            // Space calculation happens externally via CalculateSpaceStat()
            
            // Check death conditions after any changes
            CheckDeathConditions();
        }
        
        /// <summary>
        /// Calculate space stat based on nearby entities
        /// </summary>
        /// <param name="position">Position to check around</param>
        /// <param name="nearbyEntities">List of nearby entities</param>
        public void CalculateSpaceStat(Vector3 position, List<BaseNPC> nearbyEntities)
        {
            var spaceStat = GetStat(StatType.Space);
            if (spaceStat == null) return;
            
            spaceTimer += Time.deltaTime;
            if (spaceTimer >= 20f)
            {
                int nearbyCount = nearbyEntities.Count;
                float currentValue = spaceStat.CurrentValue;
                
                if (nearbyCount > 10)
                {
                    // Too crowded - reduce space
                    spaceStat.Modify(-1f);
                }
                else if (nearbyCount < 2)
                {
                    // Too isolated - increase space
                    spaceStat.Modify(1f);
                }
                
                if (spaceStat.CurrentValue != currentValue)
                {
                    OnStatChanged?.Invoke(StatType.Space, spaceStat.CurrentValue);
                }
                
                spaceTimer = 0f;
            }
        }
        
        /// <summary>
        /// Check if death conditions are met
        /// </summary>
        private void CheckDeathConditions()
        {
            var badStats = entityStats.Where(s => s.State == StatState.Bad).Select(s => s.Type).ToList();
            
            if (badStats.Count >= badStatsForDeath)
            {
                OnDeathConditionMet?.Invoke(badStats);
            }
        }
        
        /// <summary>
        /// Get all stats in bad state
        /// </summary>
        /// <returns>List of stat types that are in bad state</returns>
        public List<StatType> GetBadStats()
        {
            return entityStats.Where(s => s.State == StatState.Bad).Select(s => s.Type).ToList();
        }
        
        /// <summary>
        /// Get all stats
        /// </summary>
        /// <returns>List of all stats</returns>
        public List<Stat> GetAllStats()
        {
            return new List<Stat>(entityStats);
        }
        
        // Legacy methods for backward compatibility
        /// <summary>
        /// Take damage (legacy - now affects Health stat)
        /// </summary>
        /// <param name="damage">Amount of damage</param>
        public void TakeDamage(float damage)
        {
            ModifyStat(StatType.Health, -damage);
            // Also update legacy health for compatibility
            currentHealth = GetStat(StatType.Health)?.CurrentValue ?? currentHealth;
        }
        
        /// <summary>
        /// Heal (legacy - now affects Health stat)
        /// </summary>
        /// <param name="healAmount">Amount to heal</param>
        public void Heal(float healAmount)
        {
            ModifyStat(StatType.Health, healAmount);
            // Also update legacy health for compatibility
            currentHealth = GetStat(StatType.Health)?.CurrentValue ?? currentHealth;
        }
        
        /// <summary>
        /// Age the NPC
        /// </summary>
        /// <param name="deltaTime">Time passed</param>
        public void Age(float deltaTime)
        {
            currentAge += deltaTime;
            UpdateStats(deltaTime);
        }
        
        /// <summary>
        /// Reset stats to default values
        /// </summary>
        public void Reset()
        {
            currentAge = 0f;
            InitializeStats();
            // Also reset legacy health for compatibility
            currentHealth = GetStat(StatType.Health)?.CurrentValue ?? maxHealth;
        }
    }
}

