using UnityEngine;

namespace TerrariumEngine
{
    /// <summary>
    /// Component that makes objects ignore mouse clicks and allow them to pass through
    /// </summary>
    public class ClickThroughHandler : MonoBehaviour
    {
        [Header("Click Through Settings")]
        [SerializeField] private bool ignoreMouseClicks = true;
        [SerializeField] private bool ignorePhysics = true;
        
        private Collider _collider;
        
        private void Awake()
        {
            _collider = GetComponent<Collider>();
            
            if (_collider != null)
            {
                // Make collider a trigger so it doesn't block physics
                if (ignorePhysics)
                {
                    _collider.isTrigger = true;
                }
            }
        }
        
        /// <summary>
        /// Check if this object should ignore mouse clicks
        /// </summary>
        /// <returns>True if clicks should pass through</returns>
        public bool ShouldIgnoreClicks()
        {
            return ignoreMouseClicks;
        }
        
        /// <summary>
        /// Set whether this object should ignore mouse clicks
        /// </summary>
        /// <param name="ignore">True to ignore clicks</param>
        public void SetIgnoreClicks(bool ignore)
        {
            ignoreMouseClicks = ignore;
        }
        
        /// <summary>
        /// Set whether this object should ignore physics
        /// </summary>
        /// <param name="ignore">True to ignore physics</param>
        public void SetIgnorePhysics(bool ignore)
        {
            ignorePhysics = ignore;
            
            if (_collider != null)
            {
                _collider.isTrigger = ignore;
            }
        }
    }
}



