using UnityEngine;
using System.Linq;
using TerrariumEngine.AI.DepositSystem;
using TerrariumEngine.AI.QueenAI;
using TerrariumEngine.SpawnSystem;

namespace TerrariumEngine.AI.WorkerAI
{
    /// <summary>
    /// Worker behavior for consuming food and water at deposits
    /// </summary>
    public class WorkerConsumptionBehavior : AIBehaviorBase
    {
        [Header("Consumption Settings")]
        [SerializeField] private bool canConsumeFood = true;
        [SerializeField] private bool canConsumeWater = true;
        [SerializeField] private float consumptionThreshold = 0.3f; // Consume when below this level
        
        [Header("Debug")]
        [SerializeField] private bool showConsumptionDebug = true;
        
        // Consumption state
        private BaseConsumable targetConsumable = null;
        private Deposit targetDeposit = null;
        private ConsumptionState currentState = ConsumptionState.Idle;
        private float consumptionStartTime = 0f;
        
        // References
        private QueenOrganizationSystem queenSystem;
        
        // Events
        public System.Action<BaseConsumable, float> OnResourceConsumed;
        public System.Action<DepositType> OnConsumptionStarted;
        public System.Action<DepositType> OnConsumptionCompleted;
        
        // Properties
        public BaseConsumable TargetConsumable => targetConsumable;
        public Deposit TargetDeposit => targetDeposit;
        public ConsumptionState CurrentState => currentState;
        public bool IsConsuming => currentState == ConsumptionState.Consuming;
        
        public enum ConsumptionState
        {
            Idle,
            SeekingConsumable,
            MovingToConsumable,
            Consuming
        }
        
        protected override void InitializeBehavior()
        {
            // Set priority from constants
            priority = constants.consumptionPriority;
            
            // Find queen system
            var queen = FindFirstObjectByType<QueenNPC>();
            if (queen != null)
            {
                queenSystem = queen.GetComponent<QueenOrganizationSystem>();
            }
            
            if (npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Consumption behavior initialized");
            }
        }
        
        protected override void UpdateBehavior()
        {
            if (!IsNPCAlive()) return;
            
            // Update consumption state machine
            UpdateConsumptionState();
        }
        
        /// <summary>
        /// Update the consumption state machine
        /// </summary>
        private void UpdateConsumptionState()
        {
            switch (currentState)
            {
                case ConsumptionState.Idle:
                    HandleIdleState();
                    break;
                    
                case ConsumptionState.SeekingConsumable:
                    HandleSeekingConsumableState();
                    break;
                    
                case ConsumptionState.MovingToConsumable:
                    HandleMovingToConsumableState();
                    break;
                    
                case ConsumptionState.Consuming:
                    HandleConsumingState();
                    break;
            }
        }
        
        /// <summary>
        /// Handle idle state
        /// </summary>
        private void HandleIdleState()
        {
            // Check if we need to consume resources
            if (ShouldSeekConsumable())
            {
                ChangeState(ConsumptionState.SeekingConsumable);
            }
        }
        
        /// <summary>
        /// Handle seeking consumable state
        /// </summary>
        private void HandleSeekingConsumableState()
        {
            // Find the best consumable to consume
            BaseConsumable bestConsumable = FindBestConsumable();
            
            if (bestConsumable != null)
            {
                targetConsumable = bestConsumable;
                targetDeposit = GetDepositForConsumable(bestConsumable);
                ChangeState(ConsumptionState.MovingToConsumable);
            }
            else
            {
                ChangeState(ConsumptionState.Idle);
            }
        }
        
        /// <summary>
        /// Handle moving to consumable state
        /// </summary>
        private void HandleMovingToConsumableState()
        {
            if (targetConsumable == null)
            {
                ChangeState(ConsumptionState.Idle);
                return;
            }
            
            // Move towards the consumable
            if (MoveTowards(targetConsumable.transform.position, 1f))
            {
                ChangeState(ConsumptionState.Consuming);
            }
        }
        
        /// <summary>
        /// Handle consuming state
        /// </summary>
        private void HandleConsumingState()
        {
            if (targetConsumable == null)
            {
                ChangeState(ConsumptionState.Idle);
                return;
            }
            
            // Check if we're still in range
            float distance = Vector3.Distance(transform.position, targetConsumable.transform.position);
            if (distance > constants.consumptionRange)
            {
                ChangeState(ConsumptionState.MovingToConsumable);
                return;
            }
            
            // Check if consumable is still valid
            if (!targetConsumable.CanBeConsumedBy(npc))
            {
                ChangeState(ConsumptionState.SeekingConsumable);
                return;
            }
            
            // Start consumption if not already started
            if (consumptionStartTime == 0f)
            {
                consumptionStartTime = Time.time;
                DepositType depositType = GetDepositTypeFromConsumable(targetConsumable);
                OnConsumptionStarted?.Invoke(depositType);
                
                if (showConsumptionDebug && npc.IsDebugEnabled)
                {
                    Debug.Log($"{npc.DebugName}: Started consuming {targetConsumable.GetType().Name}");
                }
            }
            
            // Consume the resource
            ConsumeResource();
        }
        
        /// <summary>
        /// Consume the target resource
        /// </summary>
        private void ConsumeResource()
        {
            if (targetConsumable == null) return;
            
            // Calculate consumption amount based on time
            float consumptionRate = GetConsumptionRate(targetConsumable);
            float consumptionAmount = consumptionRate * Time.deltaTime;
            
            // Consume the resource
            float actualConsumed = targetConsumable.Consume(npc, consumptionAmount);
            
            if (actualConsumed > 0)
            {
                OnResourceConsumed?.Invoke(targetConsumable, actualConsumed);
                
                if (showConsumptionDebug && npc.IsDebugEnabled)
                {
                    Debug.Log($"{npc.DebugName}: Consumed {actualConsumed} {targetConsumable.GetType().Name}");
                }
            }
            
            // Check if we should stop consuming
            if (ShouldStopConsuming())
            {
                CompleteConsumption();
            }
        }
        
        /// <summary>
        /// Complete consumption
        /// </summary>
        private void CompleteConsumption()
        {
            DepositType depositType = GetDepositTypeFromConsumable(targetConsumable);
            OnConsumptionCompleted?.Invoke(depositType);
            
            if (showConsumptionDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Completed consuming {targetConsumable.GetType().Name}");
            }
            
            // Reset state
            targetConsumable = null;
            targetDeposit = null;
            consumptionStartTime = 0f;
            ChangeState(ConsumptionState.Idle);
        }
        
        /// <summary>
        /// Change consumption state
        /// </summary>
        /// <param name="newState">New state to change to</param>
        private void ChangeState(ConsumptionState newState)
        {
            if (currentState == newState) return;
            
            ConsumptionState oldState = currentState;
            currentState = newState;
            
            if (showConsumptionDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Consumption state changed from {oldState} to {newState}");
            }
        }
        
        /// <summary>
        /// Check if worker should seek consumable
        /// </summary>
        /// <returns>True if should seek consumable</returns>
        private bool ShouldSeekConsumable()
        {
            // Check if we need food
            if (canConsumeFood && NeedsResource(StatType.Food, consumptionThreshold))
            {
                return true;
            }
            
            // Check if we need water
            if (canConsumeWater && NeedsResource(StatType.Water, consumptionThreshold))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Find the best consumable to consume
        /// </summary>
        /// <returns>Best consumable, or null if none found</returns>
        private BaseConsumable FindBestConsumable()
        {
            BaseConsumable bestConsumable = null;
            float bestPriority = float.MinValue;
            
            // Find all consumables
            var allConsumables = FindObjectsByType<BaseConsumable>(FindObjectsSortMode.None)
                .Where(c => c.CanBeConsumedBy(npc))
                .ToList();
            
            foreach (var consumable in allConsumables)
            {
                // Check if it's a type we can consume
                if (!CanConsumeType(consumable)) continue;
                
                // Check if it's in a deposit area
                Deposit deposit = GetDepositForConsumable(consumable);
                if (deposit == null) continue;
                
                // Calculate priority based on need and distance
                float priority = CalculateConsumablePriority(consumable, deposit);
                
                if (priority > bestPriority)
                {
                    bestConsumable = consumable;
                    bestPriority = priority;
                }
            }
            
            return bestConsumable;
        }
        
        /// <summary>
        /// Check if we can consume this type of consumable
        /// </summary>
        /// <param name="consumable">Consumable to check</param>
        /// <returns>True if we can consume this type</returns>
        private bool CanConsumeType(BaseConsumable consumable)
        {
            if (consumable is FoodItem && canConsumeFood) return true;
            if (consumable is WaterItem && canConsumeWater) return true;
            return false;
        }
        
        /// <summary>
        /// Get the deposit for a consumable
        /// </summary>
        /// <param name="consumable">Consumable to check</param>
        /// <returns>Deposit for this consumable, or null if none found</returns>
        private Deposit GetDepositForConsumable(BaseConsumable consumable)
        {
            if (queenSystem == null) return null;
            
            if (consumable is FoodItem)
            {
                return queenSystem.GetDeposit(DepositType.Food);
            }
            else if (consumable is WaterItem)
            {
                return queenSystem.GetDeposit(DepositType.Water);
            }
            
            return null;
        }
        
        /// <summary>
        /// Calculate priority for a consumable
        /// </summary>
        /// <param name="consumable">Consumable to evaluate</param>
        /// <param name="deposit">Deposit containing the consumable</param>
        /// <returns>Priority value (higher is better)</returns>
        private float CalculateConsumablePriority(BaseConsumable consumable, Deposit deposit)
        {
            float priority = 0f;
            
            // Base priority on need
            if (consumable is FoodItem && NeedsResource(StatType.Food, consumptionThreshold))
            {
                priority += 100f - (GetStatPercentage(StatType.Food) * 100f);
            }
            
            if (consumable is WaterItem && NeedsResource(StatType.Water, consumptionThreshold))
            {
                priority += 100f - (GetStatPercentage(StatType.Water) * 100f);
            }
            
            // Reduce priority based on distance
            float distance = Vector3.Distance(transform.position, consumable.transform.position);
            priority -= distance * 0.1f;
            
            return priority;
        }
        
        /// <summary>
        /// Get consumption rate for a consumable
        /// </summary>
        /// <param name="consumable">Consumable to get rate for</param>
        /// <returns>Consumption rate per second</returns>
        private float GetConsumptionRate(BaseConsumable consumable)
        {
            if (consumable is FoodItem)
            {
                return constants.foodConsumptionRate;
            }
            else if (consumable is WaterItem)
            {
                return constants.waterConsumptionRate;
            }
            
            return 1f; // Default rate
        }
        
        /// <summary>
        /// Check if we should stop consuming
        /// </summary>
        /// <returns>True if should stop consuming</returns>
        private bool ShouldStopConsuming()
        {
            if (targetConsumable == null) return true;
            
            // Stop if consumable is empty
            if (targetConsumable.IsEmpty) return true;
            
            // Stop if we no longer need this resource
            if (targetConsumable is FoodItem && !NeedsResource(StatType.Food, consumptionThreshold))
            {
                return true;
            }
            
            if (targetConsumable is WaterItem && !NeedsResource(StatType.Water, consumptionThreshold))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get deposit type from consumable
        /// </summary>
        /// <param name="consumable">Consumable to check</param>
        /// <returns>Deposit type</returns>
        private DepositType GetDepositTypeFromConsumable(BaseConsumable consumable)
        {
            if (consumable is FoodItem) return DepositType.Food;
            if (consumable is WaterItem) return DepositType.Water;
            return DepositType.Food; // Default
        }
        
        public override bool CanActivate()
        {
            // Can activate if we need resources and can consume them
            return ShouldSeekConsumable();
        }
        
        public override string GetStatus()
        {
            string status = $"State: {currentState}";
            if (targetConsumable != null)
            {
                status += $", Target: {targetConsumable.GetType().Name}";
            }
            return status;
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showConsumptionDebug || !npc.IsDebugEnabled) return;
            
            // Draw consumption-specific info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 9f);
            if (screenPos.z > 0)
            {
                GUI.color = Color.green;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y, 160, 16), 
                    $"Consumption: {currentState}");
                
                if (targetConsumable != null)
                {
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + 16, 160, 16), 
                        $"Target: {targetConsumable.GetType().Name}");
                }
                
                GUI.color = Color.white;
            }
        }
    }
}
