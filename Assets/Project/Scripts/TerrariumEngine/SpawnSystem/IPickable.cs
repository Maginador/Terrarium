using UnityEngine;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Interface for items that can be picked up by entities
    /// </summary>
    public interface IPickable
    {
        /// <summary>
        /// Name of the item
        /// </summary>
        string ItemName { get; }
        
        /// <summary>
        /// Whether this item can be picked up
        /// </summary>
        bool CanBePicked { get; }
        
        /// <summary>
        /// Weight of the item (affects if entities can pick it up)
        /// </summary>
        float Weight { get; }
        
        /// <summary>
        /// Size category of the item (affects picking ability)
        /// </summary>
        ItemSize Size { get; }
        
        /// <summary>
        /// Transform to use for positioning when picked up
        /// </summary>
        Transform PickerTransform { get; }
        
        /// <summary>
        /// Try to pick up this item
        /// </summary>
        /// <param name="picker">Entity trying to pick up the item</param>
        /// <returns>True if successfully picked up</returns>
        bool TryPickup(IPicker picker);
        
        /// <summary>
        /// Called when item is picked up
        /// </summary>
        /// <param name="picker">Entity that picked up the item</param>
        void OnPickedUp(IPicker picker);
        
        /// <summary>
        /// Called when item is dropped
        /// </summary>
        /// <param name="picker">Entity that dropped the item</param>
        void OnDropped(IPicker picker);
        
        /// <summary>
        /// Check if a specific entity can pick up this item
        /// </summary>
        /// <param name="picker">Entity to check</param>
        /// <returns>True if entity can pick up this item</returns>
        bool CanBePickedBy(IPicker picker);
    }
    
    /// <summary>
    /// Interface for entities that can pick up items
    /// </summary>
    public interface IPicker
    {
        /// <summary>
        /// Maximum weight this entity can carry
        /// </summary>
        float MaxCarryWeight { get; }
        
        /// <summary>
        /// Current weight being carried
        /// </summary>
        float CurrentCarryWeight { get; }
        
        /// <summary>
        /// Maximum size of items this entity can pick up
        /// </summary>
        ItemSize MaxPickupSize { get; }
        
        /// <summary>
        /// Try to pick up an item
        /// </summary>
        /// <param name="item">Item to pick up</param>
        /// <returns>True if successfully picked up</returns>
        bool TryPickupItem(IPickable item);
        
        /// <summary>
        /// Drop an item
        /// </summary>
        /// <param name="item">Item to drop</param>
        /// <returns>True if successfully dropped</returns>
        bool DropItem(IPickable item);
        
        /// <summary>
        /// Get the transform of this picker (for positioning picked items)
        /// </summary>
        Transform PickerTransform { get; }
    }
    
    /// <summary>
    /// Size categories for items
    /// </summary>
    public enum ItemSize
    {
        Tiny,    // Can be picked by any entity
        Small,   // Can be picked by most entities
        Medium,  // Can be picked by larger entities
        Large,   // Can only be picked by large entities
        Huge     // Cannot be picked up
    }
}
