using UnityEngine;
using System.Linq;
using TerrariumEngine;

namespace TerrariumEngine.AI.WorkerAI
{
    /// <summary>
    /// Worker fallback behavior for random movement and sand block destruction
    /// </summary>
    public class WorkerFallbackBehavior : AIBehaviorBase
    {
        [Header("Fallback Settings")]
        [SerializeField] private bool canMoveRandomly = true;
        [SerializeField] private bool canBreakSandBlocks = true;
        [SerializeField] private float randomMoveRadius = 10f;
        
        [Header("Debug")]
        [SerializeField] private bool showFallbackDebug = true;
        
        // Fallback state
        private Vector3 randomTargetPosition = Vector3.zero;
        private float lastFallbackCheck = 0f;
        private FallbackState currentState = FallbackState.Idle;
        private Vector3Int targetSandBlock = Vector3Int.zero;
        
        // References
        private TerrariumManager terrariumManager;
        
        // Events
        public System.Action<Vector3Int> OnSandBlockBroken;
        public System.Action<Vector3> OnRandomMoveStarted;
        
        // Properties
        public FallbackState CurrentState => currentState;
        public Vector3 RandomTargetPosition => randomTargetPosition;
        public Vector3Int TargetSandBlock => targetSandBlock;
        
        public enum FallbackState
        {
            Idle,
            MovingRandomly,
            BreakingSandBlock
        }
        
        protected override void InitializeBehavior()
        {
            // Set priority from constants
            priority = constants.fallbackPriority;
            
            // Get terrarium manager
            terrariumManager = FindFirstObjectByType<TerrariumManager>();
            if (terrariumManager == null)
            {
                Debug.LogError($"{npc.DebugName}: TerrariumManager not found!");
                enabled = false;
            }
            
            if (npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Fallback behavior initialized");
            }
        }
        
        protected override void UpdateBehavior()
        {
            if (!IsNPCAlive()) return;
            
            // Check for fallback actions at intervals
            if (Time.time - lastFallbackCheck >= constants.fallbackCheckInterval)
            {
                CheckForFallbackActions();
                lastFallbackCheck = Time.time;
            }
            
            // Update fallback state machine
            UpdateFallbackState();
        }
        
        /// <summary>
        /// Update the fallback state machine
        /// </summary>
        private void UpdateFallbackState()
        {
            switch (currentState)
            {
                case FallbackState.Idle:
                    HandleIdleState();
                    break;
                    
                case FallbackState.MovingRandomly:
                    HandleMovingRandomlyState();
                    break;
                    
                case FallbackState.BreakingSandBlock:
                    HandleBreakingSandBlockState();
                    break;
            }
        }
        
        /// <summary>
        /// Handle idle state
        /// </summary>
        private void HandleIdleState()
        {
            // Stay idle until next check
        }
        
        /// <summary>
        /// Handle moving randomly state
        /// </summary>
        private void HandleMovingRandomlyState()
        {
            if (randomTargetPosition == Vector3.zero)
            {
                ChangeState(FallbackState.Idle);
                return;
            }
            
            // Move towards random target
            if (MoveTowards(randomTargetPosition, 0.8f)) // Slightly slower for fallback
            {
                // Reached target, go back to idle
                randomTargetPosition = Vector3.zero;
                ChangeState(FallbackState.Idle);
            }
        }
        
        /// <summary>
        /// Handle breaking sand block state
        /// </summary>
        private void HandleBreakingSandBlockState()
        {
            if (targetSandBlock == Vector3Int.zero)
            {
                ChangeState(FallbackState.Idle);
                return;
            }
            
            // Move towards sand block
            Vector3 blockWorldPos = terrariumManager.GridToWorldPosition(targetSandBlock);
            
            if (MoveTowards(blockWorldPos, 0.8f))
            {
                // Break the sand block
                BreakSandBlock(targetSandBlock);
                targetSandBlock = Vector3Int.zero;
                ChangeState(FallbackState.Idle);
            }
        }
        
        /// <summary>
        /// Check for fallback actions
        /// </summary>
        private void CheckForFallbackActions()
        {
            // Decide what to do based on probabilities
            float randomValue = Random.Range(0f, 1f);
            
            if (canBreakSandBlocks && randomValue < constants.randomSandBreakChance)
            {
                // Try to break a sand block
                Vector3Int sandBlock = FindSandBlockToBreak();
                if (sandBlock != Vector3Int.zero)
                {
                    targetSandBlock = sandBlock;
                    ChangeState(FallbackState.BreakingSandBlock);
                    return;
                }
            }
            
            if (canMoveRandomly && randomValue < 0.5f) // 50% chance to move randomly
            {
                // Move to a random position
                Vector3 randomPos = GetRandomPosition();
                if (randomPos != Vector3.zero)
                {
                    randomTargetPosition = randomPos;
                    ChangeState(FallbackState.MovingRandomly);
                    OnRandomMoveStarted?.Invoke(randomPos);
                }
            }
        }
        
        /// <summary>
        /// Find a sand block to break
        /// </summary>
        /// <returns>Grid position of sand block to break, or Vector3Int.zero if none found</returns>
        private Vector3Int FindSandBlockToBreak()
        {
            if (terrariumManager == null) return Vector3Int.zero;
            
            // First, check for existing holes (areas with broken blocks)
            Vector3Int existingHole = FindExistingHole();
            if (existingHole != Vector3Int.zero)
            {
                // Check if we should pick this hole (80% chance)
                if (Random.Range(0f, 1f) < constants.existingHoleBreakChance)
                {
                    return existingHole;
                }
            }
            
            // Find a random sand block to break
            Vector3Int blockCount = terrariumManager.GetBlockCount();
            int attempts = 0;
            const int maxAttempts = 20;
            
            while (attempts < maxAttempts)
            {
                int randomX = Random.Range(0, blockCount.x);
                int randomZ = Random.Range(0, blockCount.z);
                
                // Find the highest sand block at this position
                for (int y = blockCount.y - 1; y >= 0; y--)
                {
                    Vector3Int gridPos = new Vector3Int(randomX, y, randomZ);
                    
                    if (terrariumManager.HasSandBlock(gridPos))
                    {
                        return gridPos;
                    }
                }
                
                attempts++;
            }
            
            return Vector3Int.zero;
        }
        
        /// <summary>
        /// Find an existing hole (area with broken blocks)
        /// </summary>
        /// <returns>Grid position near an existing hole, or Vector3Int.zero if none found</returns>
        private Vector3Int FindExistingHole()
        {
            if (terrariumManager == null) return Vector3Int.zero;
            
            Vector3Int blockCount = terrariumManager.GetBlockCount();
            float searchRange = constants.holeSearchRange * terrariumManager.LevelOfQuality;
            
            // Search around current position
            Vector3Int currentGridPos = terrariumManager.WorldToGridPosition(transform.position);
            
            for (int x = Mathf.Max(0, currentGridPos.x - (int)searchRange); 
                 x <= Mathf.Min(blockCount.x - 1, currentGridPos.x + (int)searchRange); x++)
            {
                for (int z = Mathf.Max(0, currentGridPos.z - (int)searchRange); 
                     z <= Mathf.Min(blockCount.z - 1, currentGridPos.z + (int)searchRange); z++)
                {
                    // Check if this area has a hole (missing sand blocks)
                    if (HasHoleAt(x, z))
                    {
                        // Find a sand block near this hole
                        for (int y = 0; y < blockCount.y; y++)
                        {
                            Vector3Int gridPos = new Vector3Int(x, y, z);
                            if (terrariumManager.HasSandBlock(gridPos))
                            {
                                return gridPos;
                            }
                        }
                    }
                }
            }
            
            return Vector3Int.zero;
        }
        
        /// <summary>
        /// Check if there's a hole at the specified grid position
        /// </summary>
        /// <param name="x">X grid coordinate</param>
        /// <param name="z">Z grid coordinate</param>
        /// <returns>True if there's a hole at this position</returns>
        private bool HasHoleAt(int x, int z)
        {
            if (terrariumManager == null) return false;
            
            Vector3Int blockCount = terrariumManager.GetBlockCount();
            
            // Check if there are missing sand blocks in the lower levels
            bool hasSandAbove = false;
            bool hasHoleBelow = false;
            
            for (int y = 0; y < blockCount.y; y++)
            {
                Vector3Int gridPos = new Vector3Int(x, y, z);
                
                if (terrariumManager.HasSandBlock(gridPos))
                {
                    hasSandAbove = true;
                }
                else if (hasSandAbove)
                {
                    hasHoleBelow = true;
                    break;
                }
            }
            
            return hasHoleBelow;
        }
        
        /// <summary>
        /// Break a sand block
        /// </summary>
        /// <param name="gridPos">Grid position of the sand block to break</param>
        private void BreakSandBlock(Vector3Int gridPos)
        {
            if (terrariumManager == null) return;
            
            if (terrariumManager.HasSandBlock(gridPos))
            {
                terrariumManager.DestroySandBlock(gridPos);
                OnSandBlockBroken?.Invoke(gridPos);
                
                if (showFallbackDebug && npc.IsDebugEnabled)
                {
                    Debug.Log($"{npc.DebugName}: Broke sand block at {gridPos}");
                }
            }
        }
        
        /// <summary>
        /// Get a random position to move to
        /// </summary>
        /// <returns>Random world position, or Vector3.zero if none found</returns>
        private Vector3 GetRandomPosition()
        {
            // Generate random position within move radius
            Vector2 randomCircle = Random.insideUnitCircle * randomMoveRadius;
            Vector3 randomPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Make sure the position is within terrarium bounds
            if (terrariumManager != null)
            {
                Vector3Int blockCount = terrariumManager.GetBlockCount();
                Vector3 worldSize = terrariumManager.GridToWorldPosition(blockCount);
                
                randomPos.x = Mathf.Clamp(randomPos.x, 0, worldSize.x);
                randomPos.z = Mathf.Clamp(randomPos.z, 0, worldSize.z);
                
                // Get the highest point at this position
                randomPos.y = terrariumManager.GetHighestBlockPosition(
                    terrariumManager.WorldToGridPosition(randomPos).x,
                    terrariumManager.WorldToGridPosition(randomPos).z
                ).y + 0.5f;
            }
            
            return randomPos;
        }
        
        /// <summary>
        /// Change fallback state
        /// </summary>
        /// <param name="newState">New state to change to</param>
        private void ChangeState(FallbackState newState)
        {
            if (currentState == newState) return;
            
            FallbackState oldState = currentState;
            currentState = newState;
            
            if (showFallbackDebug && npc.IsDebugEnabled)
            {
                Debug.Log($"{npc.DebugName}: Fallback state changed from {oldState} to {newState}");
            }
        }
        
        public override bool CanActivate()
        {
            // Can always activate as fallback behavior
            return true;
        }
        
        public override string GetStatus()
        {
            string status = $"State: {currentState}";
            if (currentState == FallbackState.MovingRandomly && randomTargetPosition != Vector3.zero)
            {
                status += $", Target: {randomTargetPosition}";
            }
            else if (currentState == FallbackState.BreakingSandBlock && targetSandBlock != Vector3Int.zero)
            {
                status += $", Block: {targetSandBlock}";
            }
            return status;
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showFallbackDebug || !npc.IsDebugEnabled) return;
            
            // Draw fallback-specific info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 10f);
            if (screenPos.z > 0)
            {
                GUI.color = Color.gray;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y, 160, 16), 
                    $"Fallback: {currentState}");
                
                if (currentState == FallbackState.MovingRandomly && randomTargetPosition != Vector3.zero)
                {
                    GUI.color = Color.cyan;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + 16, 160, 16), 
                        $"Moving to: {randomTargetPosition}");
                }
                else if (currentState == FallbackState.BreakingSandBlock && targetSandBlock != Vector3Int.zero)
                {
                    GUI.color = Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + 16, 160, 16), 
                        $"Breaking: {targetSandBlock}");
                }
                
                GUI.color = Color.white;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!npc.IsDebugEnabled) return;
            
            // Draw random move radius
            Gizmos.color = Color.gray;
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
            Gizmos.DrawSphere(transform.position, randomMoveRadius);
            
            // Draw random target position
            if (randomTargetPosition != Vector3.zero)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(randomTargetPosition, 0.5f);
                Gizmos.DrawLine(transform.position, randomTargetPosition);
            }
            
            // Draw target sand block
            if (targetSandBlock != Vector3Int.zero && terrariumManager != null)
            {
                Vector3 blockWorldPos = terrariumManager.GridToWorldPosition(targetSandBlock);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(blockWorldPos, Vector3.one * 0.8f);
                Gizmos.DrawLine(transform.position, blockWorldPos);
            }
        }
    }
}
