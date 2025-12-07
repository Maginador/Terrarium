using System.Linq;
using UnityEngine;
using TerrariumEngine.AI;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Component that allows NPCs to pick up items
    /// Implements the IPicker interface for the picking system
    /// </summary>
    public class NPCPicker : MonoBehaviour, IPicker
    {
        [Header("Picking Settings")]
        [SerializeField] private float maxCarryWeight = 5f;
        [SerializeField] private ItemSize maxPickupSize = ItemSize.Medium;
        [SerializeField] private Transform pickupAttachmentPoint;
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private float pickupCooldown = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // IPicker Properties
        public float MaxCarryWeight => maxCarryWeight;
        public float CurrentCarryWeight { get; private set; } = 0f;
        public ItemSize MaxPickupSize => maxPickupSize;
        public Transform PickerTransform => pickupAttachmentPoint != null ? pickupAttachmentPoint : transform;
        
        // Picking state
        private IPickable carriedItem = null;
        private float lastPickupTime = 0f;
        
        // References
        private BaseNPC npc;
        private Collider npcCollider;
        
        // Events
        public System.Action<IPickable> OnItemPickedUp;
        public System.Action<IPickable> OnItemDropped;
        
        private void Awake()
        {
            npc = GetComponent<BaseNPC>();
            npcCollider = GetComponent<Collider>();
            
            // Create pickup attachment point if none exists
            if (pickupAttachmentPoint == null)
            {
                GameObject attachPoint = new GameObject("PickupAttachmentPoint");
                attachPoint.transform.SetParent(transform);
                attachPoint.transform.localPosition = Vector3.up * 1f; // Above the NPC
                pickupAttachmentPoint = attachPoint.transform;
            }
        }
        
        private void Update()
        {
            // Update carried weight
            UpdateCarriedWeight();
        }
        
        /// <summary>
        /// Update the current carried weight
        /// </summary>
        private void UpdateCarriedWeight()
        {
            CurrentCarryWeight = carriedItem != null ? carriedItem.Weight : 0f;
        }
        
        /// <summary>
        /// Try to pick up an item
        /// </summary>
        /// <param name="item">Item to pick up</param>
        /// <returns>True if successfully picked up</returns>
        public bool TryPickupItem(IPickable item)
        {
            if (item == null)
            {
                return false;
            }
            
            // Check cooldown
            if (Time.time - lastPickupTime < pickupCooldown)
            {
                return false;
            }
            
            // Check if already carrying something
            if (carriedItem != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"{npc.DebugName}: Already carrying {carriedItem.ItemName}");
                }
                return false;
            }
            
            // Check distance
            float distance = Vector3.Distance(transform.position, item.PickerTransform.position);
            if (distance > pickupRange)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"{npc.DebugName}: Too far from {item.ItemName} ({distance:F1}m > {pickupRange}m)");
                }
                return false;
            }
            
            // Try to pick up the item
            bool success = item.TryPickup(this);
            
            if (success)
            {
                carriedItem = item;
                lastPickupTime = Time.time;
                OnItemPickedUp?.Invoke(item);
                
                if (showDebugInfo)
                {
                    Debug.Log($"{npc.DebugName}: Picked up {item.ItemName} (Weight: {item.Weight}kg)");
                }
            }
            
            return success;
        }
        
        /// <summary>
        /// Drop the currently carried item
        /// </summary>
        /// <param name="item">Item to drop (optional, drops current item if null)</param>
        /// <returns>True if successfully dropped</returns>
        public bool DropItem(IPickable item = null)
        {
            if (carriedItem == null)
            {
                return false;
            }
            
            // If specific item requested, check if it matches
            if (item != null && carriedItem != item)
            {
                return false;
            }
            
            // Drop the item
            carriedItem.OnDropped(this);
            OnItemDropped?.Invoke(carriedItem);
            
            if (showDebugInfo)
            {
                Debug.Log($"{npc.DebugName}: Dropped {carriedItem.ItemName}");
            }
            
            carriedItem = null;
            return true;
        }
        
        /// <summary>
        /// Drop the currently carried item at a specific position
        /// </summary>
        /// <param name="position">Position to drop the item at</param>
        /// <returns>True if successfully dropped</returns>
        public bool DropItemAt(Vector3 position)
        {
            if (carriedItem == null)
            {
                return false;
            }
            
            // Move the item to the specified position
            if (carriedItem is MonoBehaviour itemMono)
            {
                itemMono.transform.position = position;
            }
            
            // Drop the item
            carriedItem.OnDropped(this);
            OnItemDropped?.Invoke(carriedItem);
            
            if (showDebugInfo)
            {
                Debug.Log($"{npc.DebugName}: Dropped {carriedItem.ItemName} at {position}");
            }
            
            carriedItem = null;
            return true;
        }
        
        /// <summary>
        /// Check if the picker can perform a pickup/drop action based on cooldown.
        /// </summary>
        private bool CanPerformAction()
        {
            return Time.time - lastPickupTime >= pickupCooldown;
        }
        
        /// <summary>
        /// Find and try to pick up the nearest pickable item
        /// </summary>
        /// <returns>True if an item was picked up</returns>
        public bool TryPickupNearestItem()
        {
            // Find all pickable items in range
            var pickableItems = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb is IPickable)
                .Cast<IPickable>()
                .Where(item => item.CanBePickedBy(this))
                .OrderBy(item => Vector3.Distance(transform.position, item.PickerTransform.position))
                .ToList();
            
            if (pickableItems.Count == 0)
            {
                return false;
            }
            
            // Try to pick up the nearest item
            return TryPickupItem(pickableItems[0]);
        }
        
        /// <summary>
        /// Check if this picker can carry a specific item
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <returns>True if can carry</returns>
        public bool CanCarry(IPickable item)
        {
            if (item == null || carriedItem != null)
            {
                return false;
            }
            
            return item.CanBePickedBy(this);
        }
        
        /// <summary>
        /// Get the currently carried item
        /// </summary>
        /// <returns>Currently carried item or null</returns>
        public IPickable GetCarriedItem()
        {
            return carriedItem;
        }
        
        /// <summary>
        /// Check if this picker is carrying an item
        /// </summary>
        /// <returns>True if carrying an item</returns>
        public bool IsCarryingItem()
        {
            return carriedItem != null;
        }
        
        /// <summary>
        /// Get remaining carry capacity
        /// </summary>
        /// <returns>Remaining weight capacity</returns>
        public float GetRemainingCapacity()
        {
            return maxCarryWeight - CurrentCarryWeight;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            // Draw picker info above the NPC
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 4f);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                // Picker info
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Carry: {CurrentCarryWeight:F1}/{maxCarryWeight:F1}kg");
                yOffset += labelHeight;
                
                // Carried item
                if (carriedItem != null)
                {
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Carrying: {carriedItem.ItemName}");
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        "Not carrying anything");
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugInfo) return;
            
            // Draw pickup range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
            
            // Draw attachment point
            if (pickupAttachmentPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(pickupAttachmentPoint.position, 0.2f);
            }
        }
    }
}
