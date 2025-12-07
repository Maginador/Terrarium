using UnityEngine;
using TerrariumEngine;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Base class for all NPCs in the terrarium system
    /// </summary>
    public abstract class BaseNPC : MonoBehaviour, IDebuggable
    {
        [Header("NPC Settings")]
        [SerializeField] protected NPCStats stats = new NPCStats();
        [SerializeField] protected string npcName = "NPC";
        
        [Header("Debug")]
        [SerializeField] protected bool showDebugInfo = true;
        
        public string DebugName => $"{GetType().Name}_{npcName}";
        public bool IsDebugEnabled { get; set; } = true;
        
        protected TerrariumManager terrariumManager;
        protected Rigidbody rb;
        protected Collider npcCollider;
        protected float lastActionTime = 0f;
        
        public NPCStats Stats => stats;
        public bool IsAlive => stats.IsAlive;
        public Vector3 Position => transform.position;
        
        public System.Action<BaseNPC> OnDeath;
        public System.Action<BaseNPC> OnSpawn;
        
        protected virtual void Awake()
        {
            terrariumManager = FindFirstObjectByType<TerrariumManager>();
            if (terrariumManager == null)
            {
                Debug.LogError($"{DebugName}: TerrariumManager not found!");
            }
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            
            npcCollider = GetComponent<Collider>();
            if (npcCollider == null)
            {
                npcCollider = gameObject.AddComponent<CapsuleCollider>();
            }
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        protected virtual void Start()
        {
            stats.Reset();
            stats.InitializeStats();
            
            // Subscribe to death condition events
            stats.OnDeathConditionMet += HandleDeathCondition;
            
            OnSpawn?.Invoke(this);
        }
        
        protected virtual void Update()
        {
            if (!IsAlive)
            {
                HandleDeath();
                return;
            }
            
            // Age the NPC (this also updates stats)
            stats.Age(Time.deltaTime);
            
            // Calculate space stat based on nearby entities
            CalculateSpaceStat();
            
            // Update behavior
            UpdateBehavior();
        }
        
        protected virtual void OnDestroy()
        {
            // Unsubscribe from events
            if (stats != null)
            {
                stats.OnDeathConditionMet -= HandleDeathCondition;
            }
            
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Main behavior update - override in derived classes
        /// </summary>
        protected abstract void UpdateBehavior();
        
        /// <summary>
        /// Move the NPC in a direction
        /// </summary>
        /// <param name="direction">Direction to move</param>
        public virtual void Move(Vector3 direction)
        {
            if (!IsAlive) return;
            
            direction.y = 0; // Keep movement on horizontal plane
            direction = direction.normalized;
            
            if (direction.magnitude > 0.1f)
            {
                // Move using rigidbody
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(direction.x * stats.moveSpeed, rb.linearVelocity.y, direction.z * stats.moveSpeed);
                }
                else
                {
                    transform.Translate(direction * stats.moveSpeed * Time.deltaTime, Space.World);
                }
                
                // Rotate to face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, stats.rotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Check if the NPC can perform an action (cooldown check)
        /// </summary>
        /// <returns>True if action can be performed</returns>
        protected virtual bool CanPerformAction()
        {
            return Time.time - lastActionTime >= stats.actionCooldown;
        }
        
        /// <summary>
        /// Mark that an action was performed (for cooldown)
        /// </summary>
        protected virtual void MarkActionPerformed()
        {
            lastActionTime = Time.time;
        }
        
        /// <summary>
        /// Check if a position is clear (no obstacles)
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if position is clear</returns>
        protected virtual bool IsPositionClear(Vector3 position)
        {
            if (terrariumManager == null) return true;
            
            Vector3Int gridPos = terrariumManager.WorldToGridPosition(position);
            return !terrariumManager.HasSandBlock(gridPos);
        }
        
        /// <summary>
        /// Get a random direction for movement
        /// </summary>
        /// <returns>Random normalized direction</returns>
        protected virtual Vector3 GetRandomDirection()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
        
        /// <summary>
        /// Handle NPC death
        /// </summary>
        protected virtual void HandleDeath()
        {
            OnDeath?.Invoke(this);
            
            if (IsDebugEnabled)
            {
                var healthStat = stats.GetStat(StatType.Health);
                string healthInfo = healthStat != null ? $"{healthStat.CurrentValue:F0}" : $"{stats.currentHealth:F0}";
                Debug.Log($"{DebugName}: Died (Health: {healthInfo}, Age: {stats.currentAge:F1}s)");
            }
            
            // Destroy the NPC
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Handle death condition from stats system
        /// </summary>
        /// <param name="badStats">List of stats that are in bad state</param>
        protected virtual void HandleDeathCondition(System.Collections.Generic.List<StatType> badStats)
        {
            if (IsDebugEnabled)
            {
                string badStatsString = string.Join(", ", badStats);
                Debug.Log($"{DebugName}: Death condition met! Bad stats: {badStatsString}");
            }
            
            // Trigger death
            HandleDeath();
        }
        
        /// <summary>
        /// Calculate space stat based on nearby entities
        /// </summary>
        protected virtual void CalculateSpaceStat()
        {
            if (terrariumManager == null) return;
            
            // Find nearby NPCs within 10 units
            var nearbyNPCs = new System.Collections.Generic.List<BaseNPC>();
            var allNPCs = FindObjectsByType<BaseNPC>(FindObjectsSortMode.None);
            
            foreach (var npc in allNPCs)
            {
                if (npc != this && Vector3.Distance(transform.position, npc.transform.position) <= 10f)
                {
                    nearbyNPCs.Add(npc);
                }
            }
            
            // Update space stat
            stats.CalculateSpaceStat(transform.position, nearbyNPCs);
        }
        
        /// <summary>
        /// Modify a stat value
        /// </summary>
        /// <param name="statType">Type of stat to modify</param>
        /// <param name="amount">Amount to change</param>
        public virtual void ModifyStat(StatType statType, float amount)
        {
            stats.ModifyStat(statType, amount);
        }
        
        /// <summary>
        /// Set a stat value directly
        /// </summary>
        /// <param name="statType">Type of stat to set</param>
        /// <param name="value">New value</param>
        public virtual void SetStat(StatType statType, float value)
        {
            stats.SetStat(statType, value);
        }
        
        /// <summary>
        /// Get a stat value
        /// </summary>
        /// <param name="statType">Type of stat to get</param>
        /// <returns>Stat instance or null if not found</returns>
        public virtual Stat GetStat(StatType statType)
        {
            return stats.GetStat(statType);
        }
        
        /// <summary>
        /// Get stat percentage (0-1)
        /// </summary>
        /// <param name="statType">Type of stat to get</param>
        /// <returns>Stat percentage or 0 if not found</returns>
        public virtual float GetStatPercentage(StatType statType)
        {
            var stat = GetStat(statType);
            return stat?.GetPercentage() ?? 0f;
        }
        
        /// <summary>
        /// Check if NPC needs a specific resource
        /// </summary>
        /// <param name="statType">Type of stat to check</param>
        /// <param name="threshold">Threshold percentage (0-1)</param>
        /// <returns>True if NPC needs this resource</returns>
        public virtual bool NeedsResource(StatType statType, float threshold = 0.5f)
        {
            return GetStatPercentage(statType) < threshold;
        }
        
        /// <summary>
        /// Take damage
        /// </summary>
        /// <param name="damage">Amount of damage</param>
        public virtual void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            
            stats.TakeDamage(damage);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Took {damage} damage. Health: {stats.currentHealth}/{stats.maxHealth}");
            }
        }
        
        /// <summary>
        /// Heal the NPC
        /// </summary>
        /// <param name="healAmount">Amount to heal</param>
        public virtual void Heal(float healAmount)
        {
            if (!IsAlive) return;
            
            stats.Heal(healAmount);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Healed {healAmount}. Health: {stats.currentHealth}/{stats.maxHealth}");
            }
        }
        
        public virtual void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        protected virtual void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw NPC info above the NPC
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                // NPC Name
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), $"{npcName}");
                yOffset += labelHeight;
                
                // Age
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), $"Age: {stats.currentAge:F1}s");
                yOffset += labelHeight;
                
                // Display all stats
                var allStats = stats.GetAllStats();
                foreach (var stat in allStats)
                {
                    string stateColor = stat.State == StatState.Good ? "green" : "red";
                    string stateText = stat.State == StatState.Good ? "✓" : "✗";
                    
                    GUI.color = stat.State == StatState.Good ? Color.green : Color.red;
                    GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), 
                        $"{stat.DisplayName}: {stat.CurrentValue:F0} {stateText}");
                    yOffset += labelHeight;
                }
                
                // Reset GUI color
                GUI.color = Color.white;
                
                // Show bad stats count if any
                var badStats = stats.GetBadStats();
                if (badStats.Count > 0)
                {
                    GUI.color = Color.red;
                    GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + yOffset, 120, labelHeight), 
                        $"Bad Stats: {badStats.Count}/{stats.badStatsForDeath}");
                    GUI.color = Color.white;
                }
            }
        }
    }
}

