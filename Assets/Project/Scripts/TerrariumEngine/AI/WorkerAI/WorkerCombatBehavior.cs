using UnityEngine;
using System.Linq;
using TerrariumEngine.AI.DepositSystem;

namespace TerrariumEngine.AI.WorkerAI
{
    /// <summary>
    /// Worker behavior for combat when stress levels are low
    /// </summary>
    public class WorkerCombatBehavior : AIBehaviorBase
    {
        [Header("Combat Settings")]
        [SerializeField] private bool canAttack = true;
        [SerializeField] private bool canBeAttacked = true;
        
        [Header("Debug")]
        [SerializeField] private bool showCombatDebug = true;
        
        // Combat state
        private WorkerNPC targetWorker = null;
        private float lastAttackTime = 0f;
        private bool isInCombat = false;
        
        // References
        private WorkerNPC workerNPC;
        
        // Events
        public System.Action<WorkerNPC, WorkerNPC> OnWorkerAttacked;
        public System.Action<WorkerNPC> OnWorkerDied;
        
        protected override void InitializeBehavior()
        {
            // Set priority from constants
            priority = constants.combatPriority;
            
            // Get worker NPC reference
            workerNPC = GetComponent<WorkerNPC>();
            if (workerNPC == null)
            {
                Debug.LogError($"{npc.DebugName}: WorkerCombatBehavior requires WorkerNPC component!");
                enabled = false;
            }
            
            if (npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Combat behavior initialized");
            }
        }
        
        protected override void UpdateBehavior()
        {
            if (!IsNPCAlive()) return;
            
            // Check if we should enter combat
            if (ShouldEnterCombat())
            {
                if (!isInCombat)
                {
                    EnterCombat();
                }
                
                // Find and attack target
                if (targetWorker == null)
                {
                    FindCombatTarget();
                }
                
                if (targetWorker != null)
                {
                    AttackTarget();
                }
            }
            else
            {
                if (isInCombat)
                {
                    ExitCombat();
                }
            }
        }
        
        /// <summary>
        /// Check if worker should enter combat
        /// </summary>
        /// <returns>True if should enter combat</returns>
        private bool ShouldEnterCombat()
        {
            if (!canAttack) return false;
            
            // Check stress level
            float stressLevel = GetStatPercentage(StatType.Stress);
            return stressLevel <= constants.combatStressThreshold;
        }
        
        /// <summary>
        /// Enter combat state
        /// </summary>
        private void EnterCombat()
        {
            isInCombat = true;
            
            if (showCombatDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Entered combat state");
            }
        }
        
        /// <summary>
        /// Exit combat state
        /// </summary>
        private void ExitCombat()
        {
            isInCombat = false;
            targetWorker = null;
            
            if (showCombatDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Exited combat state");
            }
        }
        
        /// <summary>
        /// Find a combat target
        /// </summary>
        private void FindCombatTarget()
        {
            // Find all other workers within combat range
            var allWorkers = FindObjectsByType<WorkerNPC>(FindObjectsSortMode.None)
                .Where(w => w != workerNPC && w.IsAlive && w.GetComponent<WorkerCombatBehavior>()?.canBeAttacked == true)
                .ToList();
            
            WorkerNPC closestWorker = null;
            float closestDistance = float.MaxValue;
            
            foreach (var worker in allWorkers)
            {
                float distance = Vector3.Distance(transform.position, worker.transform.position);
                
                if (distance <= constants.combatRange && distance < closestDistance)
                {
                    closestWorker = worker;
                    closestDistance = distance;
                }
            }
            
            targetWorker = closestWorker;
            
            if (targetWorker != null && showCombatDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Found combat target: {targetWorker.DebugName}");
            }
        }
        
        /// <summary>
        /// Attack the current target
        /// </summary>
        private void AttackTarget()
        {
            if (targetWorker == null || !targetWorker.IsAlive) return;
            
            // Check if target is still in range
            float distance = Vector3.Distance(transform.position, targetWorker.transform.position);
            if (distance > constants.combatRange)
            {
                targetWorker = null;
                return;
            }
            
            // Check attack cooldown
            if (Time.time - lastAttackTime < 1f) return; // 1 second cooldown between attacks
            
            // Move towards target
            if (MoveTowards(targetWorker.transform.position, 1.2f)) // Slightly faster when attacking
            {
                // Perform attack
                PerformAttack(targetWorker);
                lastAttackTime = Time.time;
            }
        }
        
        /// <summary>
        /// Perform an attack on a target worker
        /// </summary>
        /// <param name="target">Target worker to attack</param>
        private void PerformAttack(WorkerNPC target)
        {
            if (target == null || !target.IsAlive) return;
            
            // Deal damage to target
            float damage = constants.combatDamage;
            target.TakeDamage(damage);
            
            // Fire attack event
            OnWorkerAttacked?.Invoke(workerNPC, target);
            
            if (showCombatDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Attacked {target.DebugName} for {damage} damage");
            }
            
            // Check if target died
            if (!target.IsAlive)
            {
                HandleTargetDeath(target);
            }
        }
        
        /// <summary>
        /// Handle target death
        /// </summary>
        /// <param name="deadWorker">Worker that died</param>
        private void HandleTargetDeath(WorkerNPC deadWorker)
        {
            // Reduce stress when a worker dies
            float stressReduction = constants.stressReductionOnDeath;
            npc.ModifyStat(StatType.Stress, -stressReduction);
            
            // Fire death event
            OnWorkerDied?.Invoke(deadWorker);
            
            // Clear target
            targetWorker = null;
            
            if (showCombatDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Target {deadWorker.DebugName} died. Stress reduced by {stressReduction}");
            }
        }
        
        /// <summary>
        /// Take damage from another worker
        /// </summary>
        /// <param name="damage">Amount of damage to take</param>
        public void TakeDamage(float damage)
        {
            if (!canBeAttacked || !IsNPCAlive()) return;
            
            // Reduce health
            npc.ModifyStat(StatType.Health, -damage);
            
            if (showCombatDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Took {damage} damage. Health: {GetStatValue(StatType.Health)}");
            }
            
            // Check if we died
            if (!IsNPCAlive())
            {
                HandleOwnDeath();
            }
        }
        
        /// <summary>
        /// Handle this worker's death
        /// </summary>
        private void HandleOwnDeath()
        {
            // Reduce stress for other workers
            var allWorkers = FindObjectsByType<WorkerNPC>(FindObjectsSortMode.None);
            foreach (var worker in allWorkers)
            {
                if (worker != workerNPC && worker.IsAlive)
                {
                    float stressReduction = constants.stressReductionOnDeath;
                    worker.ModifyStat(StatType.Stress, -stressReduction);
                }
            }
            
            if (showCombatDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Died. Other workers' stress reduced");
            }
        }
        
        /// <summary>
        /// Check if this worker is in combat
        /// </summary>
        /// <returns>True if in combat</returns>
        public bool IsInCombat()
        {
            return isInCombat;
        }
        
        /// <summary>
        /// Get current combat target
        /// </summary>
        /// <returns>Current target, or null if none</returns>
        public WorkerNPC GetCombatTarget()
        {
            return targetWorker;
        }
        
        public override bool CanActivate()
        {
            // Can activate if stress is low enough and we can attack
            return canAttack && ShouldEnterCombat();
        }
        
        public override string GetStatus()
        {
            string status = $"In Combat: {isInCombat}";
            if (targetWorker != null)
            {
                status += $", Target: {targetWorker.DebugName}";
            }
            return status;
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showCombatDebug || !npc.IsDebugEnabled) return;
            
            // Draw combat-specific info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 8f);
            if (screenPos.z > 0)
            {
                GUI.color = Color.red;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y, 160, 16), 
                    $"Combat: {(isInCombat ? "ACTIVE" : "inactive")}");
                
                if (targetWorker != null)
                {
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + 16, 160, 16), 
                        $"Target: {targetWorker.DebugName}");
                }
                
                GUI.color = Color.white;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!npc.IsDebugEnabled || !isInCombat) return;
            
            // Draw combat range
            Gizmos.color = Color.red;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawSphere(transform.position, constants.combatRange);
            
            // Draw line to target
            if (targetWorker != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, targetWorker.transform.position);
            }
        }
    }
}
