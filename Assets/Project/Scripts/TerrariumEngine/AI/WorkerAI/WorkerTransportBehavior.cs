using UnityEngine;
using System.Linq;
using TerrariumEngine.AI.DepositSystem;
using TerrariumEngine.AI.QueenAI;
using TerrariumEngine.SpawnSystem;
using TerrariumEngine.SpawnSystem.PointOfInterestSystem;

namespace TerrariumEngine.AI.WorkerAI
{
    /// <summary>
    /// Worker behavior for picking up, transporting, and depositing food/water
    /// </summary>
    public class WorkerTransportBehavior : AIBehaviorBase
    {
        [Header("Transport Settings")]
        [SerializeField] private bool canPickupFood = true;
        [SerializeField] private bool canPickupWater = true;
        [SerializeField] private bool canTransportToDeposits = true;
        
        [Header("Debug")]
        [SerializeField] private bool showTransportDebug = true;
        
        // Transport state
        private IPickable carriedItem = null;
        private Deposit targetDeposit = null;
        private Vector3 targetPosition = Vector3.zero;
        private TransportState currentState = TransportState.Idle;
        
        // References
        private NPCPicker picker;
        private QueenOrganizationSystem queenSystem;
        
        // Events
        public System.Action<IPickable> OnItemPickedUp;
        public System.Action<IPickable> OnItemDropped;
        public System.Action<DepositType> OnItemDeposited;
        
        // Properties
        public IPickable CarriedItem => carriedItem;
        public Deposit TargetDeposit => targetDeposit;
        public TransportState CurrentState => currentState;
        public bool IsCarryingItem => carriedItem != null;
        
        public enum TransportState
        {
            Idle,
            SeekingPickup,
            MovingToPickup,
            PickingUp,
            SeekingDeposit,
            MovingToDeposit,
            Depositing,
            DeliveringToQueen
        }
        
        protected override void InitializeBehavior()
        {
            // Set priority from constants
            priority = constants.transportPriority;
            
            // Get required components
            picker = GetComponent<NPCPicker>();
            if (picker == null)
            {
                picker = gameObject.AddComponent<NPCPicker>();
            }
            
            // Find queen system
            var queen = FindFirstObjectByType<QueenNPC>();
            if (queen != null)
            {
                queenSystem = queen.GetComponent<QueenOrganizationSystem>();
            }
            
            if (npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Transport behavior initialized");
            }
        }
        
        protected override void UpdateBehavior()
        {
            if (!IsNPCAlive()) return;
            
            // Update transport state machine
            UpdateTransportState();
        }
        
        /// <summary>
        /// Update the transport state machine
        /// </summary>
        private void UpdateTransportState()
        {
            switch (currentState)
            {
                case TransportState.Idle:
                    HandleIdleState();
                    break;
                    
                case TransportState.SeekingPickup:
                    HandleSeekingPickupState();
                    break;
                    
                case TransportState.MovingToPickup:
                    HandleMovingToPickupState();
                    break;
                    
                case TransportState.PickingUp:
                    HandlePickingUpState();
                    break;
                    
                case TransportState.SeekingDeposit:
                    HandleSeekingDepositState();
                    break;
                    
                case TransportState.MovingToDeposit:
                    HandleMovingToDepositState();
                    break;
                    
                case TransportState.Depositing:
                    HandleDepositingState();
                    break;
                    
                case TransportState.DeliveringToQueen:
                    HandleDeliveringToQueenState();
                    break;
            }
        }
        
        /// <summary>
        /// Handle idle state
        /// </summary>
        private void HandleIdleState()
        {
            // Check if we should start seeking pickup
            if (ShouldSeekPickup())
            {
                ChangeState(TransportState.SeekingPickup);
            }
        }
        
        /// <summary>
        /// Handle seeking pickup state
        /// </summary>
        private void HandleSeekingPickupState()
        {
            // Find the best item to pick up
            IPickable bestItem = FindBestPickupItem();
            
            if (bestItem != null)
            {
                targetPosition = bestItem.PickerTransform.position;
                ChangeState(TransportState.MovingToPickup);
            }
            else
            {
                ChangeState(TransportState.Idle);
            }
        }
        
        /// <summary>
        /// Handle moving to pickup state
        /// </summary>
        private void HandleMovingToPickupState()
        {
            if (MoveTowards(targetPosition, 1f))
            {
                ChangeState(TransportState.PickingUp);
            }
        }
        
        /// <summary>
        /// Handle picking up state
        /// </summary>
        private void HandlePickingUpState()
        {
            // Find the item we're trying to pick up
            IPickable nearbyItem = FindNearbyPickupItem();
            
            if (nearbyItem != null && picker.TryPickupItem(nearbyItem))
            {
                carriedItem = nearbyItem;
                OnItemPickedUp?.Invoke(carriedItem);
                ChangeState(TransportState.SeekingDeposit);
            }
            else
            {
                ChangeState(TransportState.SeekingPickup);
            }
        }
        
        /// <summary>
        /// Handle seeking deposit state
        /// </summary>
        private void HandleSeekingDepositState()
        {
            if (carriedItem == null)
            {
                ChangeState(TransportState.Idle);
                return;
            }
            
            // Determine target deposit
            targetDeposit = DetermineTargetDeposit();
            
            if (targetDeposit != null)
            {
                // Check if Queen is requesting this resource
                if (queenSystem != null && queenSystem.IsRequestingResource(GetDepositTypeFromItem(carriedItem)))
                {
                    ChangeState(TransportState.DeliveringToQueen);
                }
                else
                {
                    targetPosition = targetDeposit.GetClosestPointInArea(transform.position);
                    ChangeState(TransportState.MovingToDeposit);
                }
            }
            else
            {
                ChangeState(TransportState.Idle);
            }
        }
        
        /// <summary>
        /// Handle moving to deposit state
        /// </summary>
        private void HandleMovingToDepositState()
        {
            if (carriedItem == null)
            {
                ChangeState(TransportState.Idle);
                return;
            }
            
            float speedMultiplier = constants.carryingSpeedMultiplier;
            
            if (MoveTowards(targetPosition, speedMultiplier))
            {
                ChangeState(TransportState.Depositing);
            }
        }
        
        /// <summary>
        /// Handle depositing state
        /// </summary>
        private void HandleDepositingState()
        {
            if (carriedItem == null || targetDeposit == null)
            {
                ChangeState(TransportState.Idle);
                return;
            }
            
            // Check if we're in the deposit area
            if (targetDeposit.IsWithinDepositArea(transform.position))
            {
                // Drop the item
                if (picker.DropItem(carriedItem))
                {
                    DepositType depositType = GetDepositTypeFromItem(carriedItem);
                    OnItemDeposited?.Invoke(depositType);
                    OnItemDropped?.Invoke(carriedItem);
                    
                    carriedItem = null;
                    targetDeposit = null;
                    ChangeState(TransportState.Idle);
                }
            }
            else
            {
                // Move closer to deposit
                targetPosition = targetDeposit.GetClosestPointInArea(transform.position);
                ChangeState(TransportState.MovingToDeposit);
            }
        }
        
        /// <summary>
        /// Handle delivering to queen state
        /// </summary>
        private void HandleDeliveringToQueenState()
        {
            if (carriedItem == null || queenSystem == null)
            {
                ChangeState(TransportState.Idle);
                return;
            }
            
            // Move towards Queen
            Vector3 queenPosition = queenSystem.transform.position;
            float speedMultiplier = constants.carryingSpeedMultiplier;
            
            if (MoveTowards(queenPosition, speedMultiplier))
            {
                // Drop item near Queen
                Vector3 dropPosition = queenPosition + Vector3.right * 2f;
                if (picker.DropItem(carriedItem))
                {
                    // Move the item to the drop position
                    if (carriedItem is MonoBehaviour itemMono)
                    {
                        itemMono.transform.position = dropPosition;
                    }
                    
                    DepositType depositType = GetDepositTypeFromItem(carriedItem);
                    OnItemDropped?.Invoke(carriedItem);
                    
                    carriedItem = null;
                    targetDeposit = null;
                    ChangeState(TransportState.Idle);
                }
            }
        }
        
        /// <summary>
        /// Change transport state
        /// </summary>
        /// <param name="newState">New state to change to</param>
        private void ChangeState(TransportState newState)
        {
            if (currentState == newState) return;
            
            TransportState oldState = currentState;
            currentState = newState;
            
            if (showTransportDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Transport state changed from {oldState} to {newState}");
            }
        }
        
        /// <summary>
        /// Check if worker should seek pickup
        /// </summary>
        /// <returns>True if should seek pickup</returns>
        private bool ShouldSeekPickup()
        {
            // Don't pick up if already carrying something
            if (IsCarryingItem) return false;
            
            // Check if we can pick up food or water
            return (canPickupFood || canPickupWater) && canTransportToDeposits;
        }
        
        /// <summary>
        /// Find the best item to pick up
        /// </summary>
        /// <returns>Best item to pick up, or null if none found</returns>
        private IPickable FindBestPickupItem()
        {
            IPickable bestItem = null;
            float bestDistance = float.MaxValue;
            
            // Find all pickable items
            var allPickables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb is IPickable)
                .Cast<IPickable>()
                .Where(item => item.CanBePickedBy(picker))
                .ToList();
            
            foreach (var item in allPickables)
            {
                // Check if it's a type we can pick up
                if (!CanPickupItemType(item)) continue;
                
                float distance = Vector3.Distance(transform.position, item.PickerTransform.position);
                
                if (distance < bestDistance)
                {
                    bestItem = item;
                    bestDistance = distance;
                }
            }
            
            return bestItem;
        }
        
        /// <summary>
        /// Find nearby pickup item
        /// </summary>
        /// <returns>Nearby item to pick up</returns>
        private IPickable FindNearbyPickupItem()
        {
            float pickupRange = constants.pickupRange;
            
            var nearbyItems = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb is IPickable)
                .Cast<IPickable>()
                .Where(item => item.CanBePickedBy(picker) && 
                    Vector3.Distance(transform.position, item.PickerTransform.position) <= pickupRange)
                .ToList();
            
            return nearbyItems.FirstOrDefault();
        }
        
        /// <summary>
        /// Check if we can pick up this item type
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <returns>True if we can pick up this item</returns>
        private bool CanPickupItemType(IPickable item)
        {
            if (item is FoodItem && canPickupFood) return true;
            if (item is WaterItem && canPickupWater) return true;
            return false;
        }
        
        /// <summary>
        /// Determine target deposit for carried item
        /// </summary>
        /// <returns>Target deposit, or null if none found</returns>
        private Deposit DetermineTargetDeposit()
        {
            if (carriedItem == null || queenSystem == null) return null;
            
            DepositType depositType = GetDepositTypeFromItem(carriedItem);
            return queenSystem.GetDeposit(depositType);
        }
        
        /// <summary>
        /// Get deposit type from carried item
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <returns>Deposit type</returns>
        private DepositType GetDepositTypeFromItem(IPickable item)
        {
            if (item is FoodItem) return DepositType.Food;
            if (item is WaterItem) return DepositType.Water;
            return DepositType.Food; // Default
        }
        
        public override bool CanActivate()
        {
            // Can activate if we can pick up items or if we're already carrying something
            return (canPickupFood || canPickupWater) && canTransportToDeposits;
        }
        
        public override string GetStatus()
        {
            return $"State: {currentState}, Carrying: {(IsCarryingItem ? carriedItem.ItemName : "None")}";
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showTransportDebug || !npc.IsDebugEnabled) return;
            
            // Draw transport-specific info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 7f);
            if (screenPos.z > 0)
            {
                GUI.color = Color.cyan;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y, 160, 16), 
                    $"Transport: {currentState}");
                
                if (IsCarryingItem)
                {
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + 16, 160, 16), 
                        $"Carrying: {carriedItem.ItemName}");
                }
                
                GUI.color = Color.white;
            }
        }
    }
}
