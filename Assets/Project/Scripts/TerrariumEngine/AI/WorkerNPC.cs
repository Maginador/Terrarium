using UnityEngine;
using TerrariumEngine.AI.WorkerAI;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Worker NPC that performs various tasks like transport, combat, and consumption
    /// </summary>
    public class WorkerNPC : BaseNPC
    {
        [Header("Worker Specific Settings")]
        [SerializeField] private bool canPickupItems = true;
        [SerializeField] private bool canFight = true;
        [SerializeField] private bool canConsumeResources = true;
        [SerializeField] private bool canBreakSandBlocks = true;
        
        [Header("Worker Components")]
        [SerializeField] private WorkerTransportBehavior transportBehavior;
        [SerializeField] private WorkerCombatBehavior combatBehavior;
        [SerializeField] private WorkerConsumptionBehavior consumptionBehavior;
        [SerializeField] private WorkerFallbackBehavior fallbackBehavior;
        [SerializeField] private AIStateMachine stateMachine;
        
        // Properties
        public bool CanPickupItems => canPickupItems;
        public bool CanFight => canFight;
        public bool CanConsumeResources => canConsumeResources;
        public bool CanBreakSandBlocks => canBreakSandBlocks;
        
        public WorkerTransportBehavior TransportBehavior => transportBehavior;
        public WorkerCombatBehavior CombatBehavior => combatBehavior;
        public WorkerConsumptionBehavior ConsumptionBehavior => consumptionBehavior;
        public WorkerFallbackBehavior FallbackBehavior => fallbackBehavior;
        public AIStateMachine StateMachine => stateMachine;
        
        // References
        private AIConstants constants;
        
        protected override void Start()
        {
            base.Start();
            
            // Get constants
            constants = AIConstants.Instance;
            
            // Initialize AI components
            InitializeAIComponents();
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Worker initialized with AI behaviors");
            }
        }
        
        /// <summary>
        /// Initialize AI components
        /// </summary>
        private void InitializeAIComponents()
        {
            // Get or create state machine
            if (stateMachine == null)
            {
                stateMachine = GetComponent<AIStateMachine>();
                if (stateMachine == null)
                {
                    stateMachine = gameObject.AddComponent<AIStateMachine>();
                }
            }
            
            // Get or create transport behavior
            if (transportBehavior == null && canPickupItems)
            {
                transportBehavior = GetComponent<WorkerTransportBehavior>();
                if (transportBehavior == null)
                {
                    transportBehavior = gameObject.AddComponent<WorkerTransportBehavior>();
                }
            }
            
            // Get or create combat behavior
            if (combatBehavior == null && canFight)
            {
                combatBehavior = GetComponent<WorkerCombatBehavior>();
                if (combatBehavior == null)
                {
                    combatBehavior = gameObject.AddComponent<WorkerCombatBehavior>();
                }
            }
            
            // Get or create consumption behavior
            if (consumptionBehavior == null && canConsumeResources)
            {
                consumptionBehavior = GetComponent<WorkerConsumptionBehavior>();
                if (consumptionBehavior == null)
                {
                    consumptionBehavior = gameObject.AddComponent<WorkerConsumptionBehavior>();
                }
            }
            
            // Get or create fallback behavior
            if (fallbackBehavior == null && canBreakSandBlocks)
            {
                fallbackBehavior = GetComponent<WorkerFallbackBehavior>();
                if (fallbackBehavior == null)
                {
                    fallbackBehavior = gameObject.AddComponent<WorkerFallbackBehavior>();
                }
            }
        }
        
        protected override void UpdateBehavior()
        {
            
            // Update stress based on well-being
            UpdateStressLevel();
            
            // State machine handles behavior updates
        }
        
        /// <summary>
        /// Update stress level based on well-being
        /// </summary>
        private void UpdateStressLevel()
        {
            // Increase stress if well-fed (above 50% food and water)
            float foodPercentage = GetStatPercentage(StatType.Food);
            float waterPercentage = GetStatPercentage(StatType.Water);
            
            if (foodPercentage > constants.stressIncreaseThreshold && 
                waterPercentage > constants.stressIncreaseThreshold)
            {
                // Check if it's time to increase stress
                if (Time.time % constants.stressIncreaseInterval < Time.deltaTime)
                {
                    ModifyStat(StatType.Stress, constants.stressIncreaseAmount);
                    
                    if (IsDebugEnabled)
                    {
                        Debug.Log($"{DebugName}: Stress increased due to being well-fed");
                    }
                }
            }
        }
        
        /// <summary>
        /// Take damage from combat
        /// </summary>
        /// <param name="damage">Amount of damage to take</param>
        public override void TakeDamage(float damage)
        {
            if (combatBehavior != null)
            {
                combatBehavior.TakeDamage(damage);
            }
            else
            {
                // Direct damage if no combat behavior
                ModifyStat(StatType.Health, -damage);
            }
        }
        
        /// <summary>
        /// Check if worker is in combat
        /// </summary>
        /// <returns>True if in combat</returns>
        public bool IsInCombat()
        {
            return combatBehavior != null && combatBehavior.IsInCombat();
        }
        
        /// <summary>
        /// Get current combat target
        /// </summary>
        /// <returns>Current combat target, or null if none</returns>
        public WorkerNPC GetCombatTarget()
        {
            return combatBehavior != null ? combatBehavior.GetCombatTarget() : null;
        }
        
        /// <summary>
        /// Check if worker is carrying an item
        /// </summary>
        /// <returns>True if carrying an item</returns>
        public bool IsCarryingItem()
        {
            return transportBehavior != null && transportBehavior.IsCarryingItem;
        }
        
        /// <summary>
        /// Get the item being carried
        /// </summary>
        /// <returns>Carried item, or null if none</returns>
        public SpawnSystem.IPickable GetCarriedItem()
        {
            return transportBehavior != null ? transportBehavior.CarriedItem : null;
        }
        
        /// <summary>
        /// Check if worker is consuming a resource
        /// </summary>
        /// <returns>True if consuming a resource</returns>
        public bool IsConsumingResource()
        {
            return consumptionBehavior != null && consumptionBehavior.IsConsuming;
        }
        
        /// <summary>
        /// Get the current behavior state
        /// </summary>
        /// <returns>Current behavior state string</returns>
        public string GetCurrentBehaviorState()
        {
            if (stateMachine == null) return "No State Machine";
            
            var currentBehavior = stateMachine.CurrentBehavior;
            if (currentBehavior == null) return "Idle";
            
            return currentBehavior.GetType().Name;
        }
        
        /// <summary>
        /// Enable or disable a specific behavior
        /// </summary>
        /// <param name="behaviorType">Type of behavior to enable/disable</param>
        /// <param name="enabled">Whether to enable the behavior</param>
        public void SetBehaviorEnabled(System.Type behaviorType, bool enabled)
        {
            var behavior = GetComponent(behaviorType) as AIBehaviorBase;
            if (behavior != null)
            {
                behavior.enabled = enabled;
            }
        }
        
        /// <summary>
        /// Force activate a specific behavior
        /// </summary>
        /// <param name="behaviorType">Type of behavior to activate</param>
        public void ForceActivateBehavior(System.Type behaviorType)
        {
            if (stateMachine == null) return;
            
            var behavior = GetComponent(behaviorType) as AIBehaviorBase;
            if (behavior != null)
            {
                stateMachine.ForceActivateBehavior(behavior);
            }
        }
        
        
        private void OnDrawGizmos()
        {
            if (!IsDebugEnabled) return;
            
            // Draw worker-specific gizmos
            // The individual behaviors will draw their own gizmos
        }
    }
}
