using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// State machine for managing AI behaviors
    /// Handles behavior prioritization and transitions
    /// </summary>
    public class AIStateMachine : MonoBehaviour
    {
        [Header("State Machine Settings")]
        [SerializeField] private bool enableStateMachine = true;
        [SerializeField] private float behaviorCheckInterval = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Behavior management
        private List<AIBehaviorBase> behaviors = new List<AIBehaviorBase>();
        private AIBehaviorBase currentBehavior = null;
        private float lastBehaviorCheck = 0f;
        
        // References
        private BaseNPC npc;
        private AIConstants constants;
        
        // Events
        public System.Action<AIBehaviorBase, AIBehaviorBase> OnBehaviorChanged;
        public System.Action<AIBehaviorBase> OnBehaviorActivated;
        public System.Action<AIBehaviorBase> OnBehaviorDeactivated;
        
        // Properties
        public AIBehaviorBase CurrentBehavior => currentBehavior;
        public bool IsStateMachineActive => enableStateMachine;
        public int ActiveBehaviorCount => behaviors.Count(b => b.IsActive);
        
        private void Awake()
        {
            npc = GetComponent<BaseNPC>();
            constants = AIConstants.Instance;
            
            if (npc == null)
            {
                Debug.LogError($"{GetType().Name}: BaseNPC component not found!");
                enabled = false;
            }
        }
        
        private void Start()
        {
            // Find all AI behaviors on this NPC
            FindAndRegisterBehaviors();
        }
        
        private void Update()
        {
            if (!enableStateMachine || !npc.IsAlive) return;
            
            // Check for behavior changes at intervals
            if (Time.time - lastBehaviorCheck >= behaviorCheckInterval)
            {
                UpdateBehaviorState();
                lastBehaviorCheck = Time.time;
            }
        }
        
        /// <summary>
        /// Find and register all AI behaviors on this NPC
        /// </summary>
        private void FindAndRegisterBehaviors()
        {
            behaviors.Clear();
            
            // Find all AI behavior components
            var foundBehaviors = GetComponents<AIBehaviorBase>();
            
            foreach (var behavior in foundBehaviors)
            {
                RegisterBehavior(behavior);
            }
            
            // Sort behaviors by priority (highest first)
            behaviors = behaviors.OrderByDescending(b => b.Priority).ToList();
            
            if (showDebugInfo && constants.showAIDebug)
            {
                Debug.Log($"{npc.DebugName}: Registered {behaviors.Count} AI behaviors");
            }
        }
        
        /// <summary>
        /// Register a new behavior
        /// </summary>
        /// <param name="behavior">Behavior to register</param>
        public void RegisterBehavior(AIBehaviorBase behavior)
        {
            if (behavior == null || behaviors.Contains(behavior)) return;
            
            behaviors.Add(behavior);
            
            // Subscribe to behavior events
            behavior.OnBehaviorActivated += OnBehaviorActivatedHandler;
            behavior.OnBehaviorDeactivated += OnBehaviorDeactivatedHandler;
            
            // Sort behaviors by priority
            behaviors = behaviors.OrderByDescending(b => b.Priority).ToList();
            
            if (showDebugInfo && constants.showAIDebug)
            {
                Debug.Log($"{npc.DebugName}: Registered behavior {behavior.GetType().Name} with priority {behavior.Priority}");
            }
        }
        
        /// <summary>
        /// Unregister a behavior
        /// </summary>
        /// <param name="behavior">Behavior to unregister</param>
        public void UnregisterBehavior(AIBehaviorBase behavior)
        {
            if (behavior == null || !behaviors.Contains(behavior)) return;
            
            // Unsubscribe from events
            behavior.OnBehaviorActivated -= OnBehaviorActivatedHandler;
            behavior.OnBehaviorDeactivated -= OnBehaviorDeactivatedHandler;
            
            // Deactivate if it's the current behavior
            if (currentBehavior == behavior)
            {
                DeactivateCurrentBehavior();
            }
            
            behaviors.Remove(behavior);
            
            if (showDebugInfo && constants.showAIDebug)
            {
                Debug.Log($"{npc.DebugName}: Unregistered behavior {behavior.GetType().Name}");
            }
        }
        
        /// <summary>
        /// Update the current behavior state based on priorities
        /// </summary>
        private void UpdateBehaviorState()
        {
            // Find the highest priority behavior that can be activated
            AIBehaviorBase bestBehavior = null;
            
            foreach (var behavior in behaviors)
            {
                if (behavior.IsEnabled && behavior.CanActivate())
                {
                    bestBehavior = behavior;
                    break; // Since behaviors are sorted by priority, first match is best
                }
            }
            
            // Check if we need to change behavior
            if (bestBehavior != currentBehavior)
            {
                ChangeBehavior(bestBehavior);
            }
        }
        
        /// <summary>
        /// Change to a new behavior
        /// </summary>
        /// <param name="newBehavior">New behavior to activate</param>
        private void ChangeBehavior(AIBehaviorBase newBehavior)
        {
            AIBehaviorBase previousBehavior = currentBehavior;
            
            // Deactivate current behavior
            if (currentBehavior != null)
            {
                currentBehavior.Deactivate();
            }
            
            // Activate new behavior
            currentBehavior = newBehavior;
            if (currentBehavior != null)
            {
                currentBehavior.Activate();
            }
            
            // Fire events
            OnBehaviorChanged?.Invoke(previousBehavior, currentBehavior);
            
            if (showDebugInfo && constants.showBehaviorTransitions)
            {
                string fromName = previousBehavior != null ? previousBehavior.GetType().Name : "None";
                string toName = currentBehavior != null ? currentBehavior.GetType().Name : "None";
                Debug.Log($"{npc.DebugName}: Behavior changed from {fromName} to {toName}");
            }
        }
        
        /// <summary>
        /// Force activate a specific behavior
        /// </summary>
        /// <param name="behavior">Behavior to force activate</param>
        public void ForceActivateBehavior(AIBehaviorBase behavior)
        {
            if (behavior == null || !behaviors.Contains(behavior)) return;
            
            ChangeBehavior(behavior);
        }
        
        /// <summary>
        /// Force deactivate current behavior
        /// </summary>
        public void DeactivateCurrentBehavior()
        {
            ChangeBehavior(null);
        }
        
        /// <summary>
        /// Get a behavior of a specific type
        /// </summary>
        /// <typeparam name="T">Type of behavior to get</typeparam>
        /// <returns>Behavior of specified type, or null if not found</returns>
        public T GetBehavior<T>() where T : AIBehaviorBase
        {
            return behaviors.OfType<T>().FirstOrDefault();
        }
        
        /// <summary>
        /// Check if a behavior of a specific type is active
        /// </summary>
        /// <typeparam name="T">Type of behavior to check</typeparam>
        /// <returns>True if behavior is active</returns>
        public bool IsBehaviorActive<T>() where T : AIBehaviorBase
        {
            var behavior = GetBehavior<T>();
            return behavior != null && behavior.IsActive;
        }
        
        /// <summary>
        /// Get all behaviors of a specific type
        /// </summary>
        /// <typeparam name="T">Type of behaviors to get</typeparam>
        /// <returns>List of behaviors of specified type</returns>
        public List<T> GetBehaviors<T>() where T : AIBehaviorBase
        {
            return behaviors.OfType<T>().ToList();
        }
        
        /// <summary>
        /// Handle behavior activation
        /// </summary>
        /// <param name="behavior">Activated behavior</param>
        private void OnBehaviorActivatedHandler(AIBehaviorBase behavior)
        {
            OnBehaviorActivated?.Invoke(behavior);
        }
        
        /// <summary>
        /// Handle behavior deactivation
        /// </summary>
        /// <param name="behavior">Deactivated behavior</param>
        private void OnBehaviorDeactivatedHandler(AIBehaviorBase behavior)
        {
            OnBehaviorDeactivated?.Invoke(behavior);
        }
        
        /// <summary>
        /// Enable or disable the state machine
        /// </summary>
        /// <param name="enabled">Whether to enable the state machine</param>
        public void SetStateMachineEnabled(bool enabled)
        {
            enableStateMachine = enabled;
            
            if (!enabled && currentBehavior != null)
            {
                DeactivateCurrentBehavior();
            }
        }
        
        /// <summary>
        /// Get debug information about the state machine
        /// </summary>
        /// <returns>Debug information string</returns>
        public string GetDebugInfo()
        {
            string info = $"State Machine: {(enableStateMachine ? "Active" : "Inactive")}\n";
            info += $"Current Behavior: {(currentBehavior != null ? currentBehavior.GetType().Name : "None")}\n";
            info += $"Total Behaviors: {behaviors.Count}\n";
            info += $"Active Behaviors: {ActiveBehaviorCount}\n";
            
            info += "\nBehavior List:\n";
            foreach (var behavior in behaviors)
            {
                string status = behavior.IsActive ? "ACTIVE" : "inactive";
                info += $"  {behavior.GetType().Name}: {status} (Priority: {behavior.Priority})\n";
            }
            
            return info;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !constants.showAIDebug) return;
            
            // Draw state machine info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 5f);
            if (screenPos.z > 0)
            {
                GUI.color = Color.cyan;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y, 160, 16), 
                    $"State Machine: {(enableStateMachine ? "ON" : "OFF")}");
                
                if (currentBehavior != null)
                {
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + 16, 160, 16), 
                        $"Current: {currentBehavior.GetType().Name}");
                }
                
                GUI.color = Color.white;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from all behavior events
            foreach (var behavior in behaviors)
            {
                if (behavior != null)
                {
                    behavior.OnBehaviorActivated -= OnBehaviorActivatedHandler;
                    behavior.OnBehaviorDeactivated -= OnBehaviorDeactivatedHandler;
                }
            }
        }
    }
}

