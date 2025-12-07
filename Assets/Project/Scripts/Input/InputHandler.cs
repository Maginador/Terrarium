using UnityEngine;
using UnityEngine.InputSystem;
using TerrariumEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TerrariumEngine.Input
{
    /// <summary>
    /// Handles input for the terrarium system, including mouse interactions
    /// </summary>
    public class InputHandler : MonoBehaviour, IDebuggable
    {
        [Header("Input Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask sandBlockLayerMask = -1;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private bool ignoreGlassBlocks = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        public string DebugName => "InputHandler";
        public bool IsDebugEnabled { get; set; } = true;
        
        private TerrariumManager _terrariumManager;
        
        public System.Action<Vector3> OnWorldPositionClicked;
        public System.Action<Vector3Int> OnSandBlockClicked;
        
        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    playerCamera = FindFirstObjectByType<Camera>();
                }
            }
            
            _terrariumManager = FindFirstObjectByType<TerrariumManager>();
            if (_terrariumManager == null)
            {
                Debug.LogError("InputHandler: TerrariumManager not found!");
            }
            
            // Initialize input actions
            if (inputActions == null)
            {
                Debug.LogWarning("InputHandler: No Input Actions asset assigned. Mouse input will not work.");
            }
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void OnEnable()
        {
            if (inputActions != null)
            {
                inputActions.Enable();
                
                // Find the Attack action in the Player action map
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    var attackAction = playerMap.FindAction("Attack");
                    if (attackAction != null)
                    {
                        attackAction.performed += OnAttackPerformed;
                    }
                    else
                    {
                        Debug.LogWarning("InputHandler: Attack action not found in Player action map.");
                    }
                }
                else
                {
                    Debug.LogWarning("InputHandler: Player action map not found.");
                }
            }
        }
        
        private void OnDisable()
        {
            if (inputActions != null)
            {
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    var attackAction = playerMap.FindAction("Attack");
                    if (attackAction != null)
                    {
                        attackAction.performed -= OnAttackPerformed;
                    }
                }
                
                inputActions.Disable();
            }
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Handle attack input (left mouse button)
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            HandleLeftClick();
        }
        
        /// <summary>
        /// Handle left mouse button click
        /// </summary>
        private void HandleLeftClick()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = playerCamera.ScreenPointToRay(mousePosition);
            
            // Get all hits along the ray
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, sandBlockLayerMask);
            
            // Sort hits by distance
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            
            // Find the first non-glass hit
            RaycastHit? targetHit = null;
            foreach (RaycastHit hit in hits)
            {
                // Check if this is a glass block that should be ignored
                if (ignoreGlassBlocks)
                {
                    ClickThroughHandler clickThrough = hit.collider.GetComponent<ClickThroughHandler>();
                    if (clickThrough != null && clickThrough.ShouldIgnoreClicks())
                    {
                        continue; // Skip this glass block
                    }
                }
                
                // This is a valid target
                targetHit = hit;
                break;
            }
            
            if (targetHit.HasValue)
            {
                RaycastHit hit = targetHit.Value;
                
                // Check if we hit a sand block
                SandBlock sandBlock = hit.collider.GetComponent<SandBlock>();
                if (sandBlock != null)
                {
                    // Destroy the sand block
                    Vector3Int gridPos = _terrariumManager.WorldToGridPosition(hit.point);
                    if (_terrariumManager.DestroySandBlock(gridPos))
                    {
                        OnSandBlockClicked?.Invoke(gridPos);
                        
                        if (IsDebugEnabled)
                        {
                            Debug.Log($"InputHandler: Destroyed sand block at {gridPos}");
                        }
                    }
                }
                else
                {
                    // Hit something else, report world position
                    OnWorldPositionClicked?.Invoke(hit.point);
                    
                    if (IsDebugEnabled)
                    {
                        Debug.Log($"InputHandler: Clicked world position {hit.point}");
                    }
                }
            }
            else
            {
                // Raycast didn't hit anything, but we can still report the world position
                // by projecting the ray onto a plane (e.g., ground plane at y=0)
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Vector3 worldPos = GetWorldPositionFromScreen(mousePos);
                OnWorldPositionClicked?.Invoke(worldPos);
                
                if (IsDebugEnabled)
                {
                    Debug.Log($"InputHandler: Clicked empty space at {worldPos}");
                }
            }
        }
        
        /// <summary>
        /// Get world position from screen position by projecting onto a plane
        /// </summary>
        /// <param name="screenPosition">Screen position</param>
        /// <returns>World position</returns>
        private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
        {
            Ray ray = playerCamera.ScreenPointToRay(screenPosition);
            
            // Project onto a plane at y=0 (ground level)
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            // Fallback: return a position in front of the camera
            return ray.GetPoint(10f);
        }
        
        /// <summary>
        /// Get the world position under the mouse cursor
        /// </summary>
        /// <returns>World position or Vector3.zero if no valid position</returns>
        public Vector3 GetMouseWorldPosition()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            return GetWorldPositionFromScreen(mousePosition);
        }
        
        /// <summary>
        /// Get the grid position under the mouse cursor
        /// </summary>
        /// <returns>Grid position</returns>
        public Vector3Int GetMouseGridPosition()
        {
            Vector3 worldPos = GetMouseWorldPosition();
            return _terrariumManager.WorldToGridPosition(worldPos);
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            GUILayout.BeginArea(new Rect(10, 380, 300, 100));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Input Info", GUI.skin.box);
            GUILayout.Space(5);
            
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3Int mouseGridPos = GetMouseGridPosition();
            
            GUILayout.Label($"Mouse World: {mouseWorldPos}", GUI.skin.label);
            GUILayout.Label($"Mouse Grid: {mouseGridPos}", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
