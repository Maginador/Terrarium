using UnityEngine;

namespace TerrariumEngine
{
    /// <summary>
    /// Interface for objects that can be destroyed in the terrarium
    /// </summary>
    public interface IDestructible
    {
        /// <summary>
        /// Whether this object is currently destroyed
        /// </summary>
        bool IsDestroyed { get; }
        
        /// <summary>
        /// The world position of this destructible object
        /// </summary>
        Vector3 WorldPosition { get; }
        
        /// <summary>
        /// Destroy this object
        /// </summary>
        void Destroy();
        
        /// <summary>
        /// Restore this object (for pooling)
        /// </summary>
        void Restore();
        
        /// <summary>
        /// Event called when the object is destroyed
        /// </summary>
        System.Action<IDestructible> OnDestroyed { get; set; }
        
        /// <summary>
        /// Event called when the object is restored
        /// </summary>
        System.Action<IDestructible> OnRestored { get; set; }
    }
}

