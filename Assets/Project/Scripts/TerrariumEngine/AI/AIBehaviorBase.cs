using UnityEngine;
using TerrariumEngine.SpawnSystem.PointOfInterestSystem;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Base class for all AI behaviors
    /// Provides common functionality and interface for behavior states
    /// </summary>
    public abstract class AIBehaviorBase : MonoBehaviour
    {
        [Header("Behavior Settings")]
        [SerializeField] protected int priority = 50;
        [SerializeField] protected bool isEnabled = true;
        [SerializeField] protected float updateInterval = 0.5f;
        
        [Header("Debug")]
        [SerializeField] protected bool showDebugInfo = true;
        
        // Properties
        public int Priority => priority;
        public bool IsEnabled => isEnabled;
        public bool IsActive { get; protected set; } = false;
        public float LastUpdateTime { get; protected set; } = 0f;
        
        // References
        protected BaseNPC npc;
        protected AIConstants constants;
        
        // Events
        public System.Action<AIBehaviorBase> OnBehaviorActivated;
        public System.Action<AIBehaviorBase> OnBehaviorDeactivated;
        public System.Action<AIBehaviorBase, string> OnBehaviorStateChanged;
        
        protected virtual void Awake()
        {
            npc = GetComponent<BaseNPC>();
            constants = AIConstants.Instance;
            
            if (npc == null)
            {
                Debug.LogError($"{GetType().Name}: BaseNPC component not found!");
                enabled = false;
            }
        }
        
        protected virtual void Start()
        {
            InitializeBehavior();
        }
        
        protected virtual void Update()
        {
            if (!isEnabled || !IsActive) return;
            
            // Update at specified intervals
            if (Time.time - LastUpdateTime >= updateInterval)
            {
                UpdateBehavior();
                LastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// Initialize behavior-specific settings
        /// </summary>
        protected abstract void InitializeBehavior();
        
        /// <summary>
        /// Update behavior logic
        /// </summary>
        protected abstract void UpdateBehavior();
        
        /// <summary>
        /// Check if this behavior can be activated
        /// </summary>
        /// <returns>True if behavior can be activated</returns>
        public abstract bool CanActivate();
        
        /// <summary>
        /// Activate this behavior
        /// </summary>
        public virtual void Activate()
        {
            if (!CanActivate() || IsActive) return;
            
            IsActive = true;
            OnBehaviorActivated?.Invoke(this);
            OnBehaviorStateChanged?.Invoke(this, "Activated");
            
            if (showDebugInfo && constants.showBehaviorTransitions)
            {
                Debug.Log($"{npc.DebugName}: {GetType().Name} activated");
            }
        }
        
        /// <summary>
        /// Deactivate this behavior
        /// </summary>
        public virtual void Deactivate()
        {
            if (!IsActive) return;
            
            IsActive = false;
            OnBehaviorDeactivated?.Invoke(this);
            OnBehaviorStateChanged?.Invoke(this, "Deactivated");
            
            if (showDebugInfo && constants.showBehaviorTransitions)
            {
                Debug.Log($"{npc.DebugName}: {GetType().Name} deactivated");
            }
        }
        
        /// <summary>
        /// Get behavior status for debugging
        /// </summary>
        /// <returns>Status string</returns>
        public virtual string GetStatus()
        {
            return $"Priority: {priority}, Active: {IsActive}, Enabled: {isEnabled}";
        }
        
        /// <summary>
        /// Check if behavior should be interrupted by higher priority behavior
        /// </summary>
        /// <param name="otherBehavior">Other behavior to compare with</param>
        /// <returns>True if this behavior should be interrupted</returns>
        public virtual bool ShouldInterrupt(AIBehaviorBase otherBehavior)
        {
            return otherBehavior.Priority > Priority;
        }
        
        /// <summary>
        /// Get distance to a target position
        /// </summary>
        /// <param name="targetPosition">Target position</param>
        /// <returns>Distance to target</returns>
        protected float GetDistanceTo(Vector3 targetPosition)
        {
            return Vector3.Distance(transform.position, targetPosition);
        }
        
        /// <summary>
        /// Move towards a target position
        /// </summary>
        /// <param name="targetPosition">Target position</param>
        /// <param name="speedMultiplier">Speed multiplier (default 1)</param>
        /// <returns>True if reached target</returns>
        protected bool MoveTowards(Vector3 targetPosition, float speedMultiplier = 1f)
        {
            if (npc == null) return false;
            
            float distance = GetDistanceTo(targetPosition);
            
            if (distance <= constants.stoppingDistance)
            {
                return true; // Reached target
            }
            
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; // Keep movement on horizontal plane
            
            // Apply speed multiplier
            float currentSpeed = constants.baseMovementSpeed * speedMultiplier;
            
            // Move using BaseNPC's movement system
            npc.Move(direction * currentSpeed);
            
            return false;
        }
        
        /// <summary>
        /// Look at a target position
        /// </summary>
        /// <param name="targetPosition">Target position</param>
        protected void LookAt(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0; // Keep rotation on horizontal plane
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    constants.rotationSpeed * Time.deltaTime
                );
            }
        }
        
        /// <summary>
        /// Check if NPC is alive and can perform actions
        /// </summary>
        /// <returns>True if NPC is alive</returns>
        protected bool IsNPCAlive()
        {
            return npc != null && npc.IsAlive;
        }
        
        /// <summary>
        /// Get NPC's current stat value
        /// </summary>
        /// <param name="statType">Type of stat to get</param>
        /// <returns>Current stat value</returns>
        protected float GetStatValue(StatType statType)
        {
            if (npc == null) return 0f;
            
            var stat = npc.GetStat(statType);
            return stat != null ? stat.CurrentValue : 0f;
        }
        
        /// <summary>
        /// Get NPC's stat percentage (0-1)
        /// </summary>
        /// <param name="statType">Type of stat to get</param>
        /// <returns>Stat percentage</returns>
        protected float GetStatPercentage(StatType statType)
        {
            if (npc == null) return 0f;
            
            var stat = npc.GetStat(statType);
            if (stat == null) return 0f;
            
            return stat.GetPercentage();
        }
        
        /// <summary>
        /// Check if NPC needs a specific resource
        /// </summary>
        /// <param name="statType">Type of stat to check</param>
        /// <param name="threshold">Threshold percentage (0-1)</param>
        /// <returns>True if NPC needs this resource</returns>
        protected bool NeedsResource(StatType statType, float threshold = 0.5f)
        {
            return GetStatPercentage(statType) < threshold;
        }
        
        protected virtual void OnGUI()
        {
            if (!showDebugInfo || !constants.showAIDebug) return;
            
            // Draw behavior status
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 6f);
            if (screenPos.z > 0)
            {
                GUI.color = IsActive ? Color.green : Color.gray;
                GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y, 120, 16), 
                    $"{GetType().Name}: {GetStatus()}");
                GUI.color = Color.white;
            }
        }
    }
}
