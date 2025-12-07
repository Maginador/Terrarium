using UnityEngine;

namespace TerrariumEngine
{
    /// <summary>
    /// Interface for objects that can be debugged through the debug manager
    /// </summary>
    public interface IDebuggable
    {
        /// <summary>
        /// The debug name/identifier for this object
        /// </summary>
        string DebugName { get; }
        
        /// <summary>
        /// Whether this debug element is currently enabled
        /// </summary>
        bool IsDebugEnabled { get; set; }
        
        /// <summary>
        /// Called when debug state changes
        /// </summary>
        /// <param name="enabled">New debug state</param>
        void OnDebugStateChanged(bool enabled);
    }
}

