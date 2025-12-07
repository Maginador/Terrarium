using UnityEngine;
using UnityEngine.InputSystem;
using TerrariumEngine;

namespace TerrariumEngine.Input
{
    /// <summary>
    /// Camera controller for the terrarium system with WASD movement
    /// </summary>
    public class CameraController : MonoBehaviour, IDebuggable
    {
        [Header("Camera Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMoveSpeed = 20f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool invertY = false;
        
        [Header("Camera Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector3 minBounds = new Vector3(-50, 5, -50);
        [SerializeField] private Vector3 maxBounds = new Vector3(50, 50, 50);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        public string DebugName => "CameraController";
        public bool IsDebugEnabled { get; set; } = true;
        
        private InputActionAsset inputActions;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction leftMouseAction;
        private InputAction altKeyAction;
        
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool isSprinting;
        private bool isLeftMousePressed;
        private bool isAltPressed;
        
        private float xRotation = 0f;
        private float yRotation = 0f;
        
        private void Awake()
        {
            // Get input actions from InputHandler if available
            var inputHandler = FindFirstObjectByType<InputHandler>();
            if (inputHandler != null)
            {
                // Use reflection to get the inputActions field
                var inputActionsField = typeof(InputHandler).GetField("inputActions", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                inputActions = inputActionsField?.GetValue(inputHandler) as InputActionAsset;
            }
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void OnEnable()
        {
            if (inputActions != null)
            {
                inputActions.Enable();
                
                // Get actions from the Player action map
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    moveAction = playerMap.FindAction("Move");
                    lookAction = playerMap.FindAction("Look");
                    sprintAction = playerMap.FindAction("Sprint");
                    leftMouseAction = playerMap.FindAction("Attack"); // Left mouse button
                    
                    if (moveAction != null)
                    {
                        moveAction.performed += OnMovePerformed;
                        moveAction.canceled += OnMoveCanceled;
                    }
                    if (lookAction != null)
                    {
                        lookAction.performed += OnLookPerformed;
                        lookAction.canceled += OnLookCanceled;
                    }
                    if (sprintAction != null)
                    {
                        sprintAction.performed += OnSprintPerformed;
                        sprintAction.canceled += OnSprintCanceled;
                    }
                    if (leftMouseAction != null)
                    {
                        leftMouseAction.performed += OnLeftMousePerformed;
                        leftMouseAction.canceled += OnLeftMouseCanceled;
                    }
                }
                
                // Get Alt key action from the same action map
                if (playerMap != null)
                {
                    altKeyAction = playerMap.FindAction("Look");
                    if (altKeyAction != null)
                    {
                        altKeyAction.performed += OnAltKeyPerformed;
                        altKeyAction.canceled += OnAltKeyCanceled;
                    }
                }
            }
        }
        
        private void OnDisable()
        {
            if (inputActions != null)
            {
                if (moveAction != null)
                {
                    moveAction.performed -= OnMovePerformed;
                    moveAction.canceled -= OnMoveCanceled;
                }
                if (lookAction != null)
                {
                    lookAction.performed -= OnLookPerformed;
                    lookAction.canceled -= OnLookCanceled;
                }
                if (sprintAction != null)
                {
                    sprintAction.performed -= OnSprintPerformed;
                    sprintAction.canceled -= OnSprintCanceled;
                }
                if (leftMouseAction != null)
                {
                    leftMouseAction.performed -= OnLeftMousePerformed;
                    leftMouseAction.canceled -= OnLeftMouseCanceled;
                }
                if (altKeyAction != null)
                {
                    altKeyAction.performed -= OnAltKeyPerformed;
                    altKeyAction.canceled -= OnAltKeyCanceled;
                }
                
                inputActions.Disable();
            }
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        private void Update()
        {
            HandleMovement();
            HandleLook();
        }
        
        /// <summary>
        /// Handle camera movement
        /// </summary>
        private void HandleMovement()
        {
            if (moveInput.magnitude < 0.1f) return;
            
            // Calculate movement direction relative to camera
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            // Remove Y component to keep movement on horizontal plane
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // Calculate movement vector
            Vector3 movement = (forward * moveInput.y + right * moveInput.x) * Time.deltaTime;
            
            // Apply speed (fast or normal)
            float currentSpeed = isSprinting ? fastMoveSpeed : moveSpeed;
            movement *= currentSpeed;
            
            // Apply movement
            Vector3 newPosition = transform.position + movement;
            
            // Apply bounds if enabled
            if (useBounds)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
                newPosition.y = Mathf.Clamp(newPosition.y, minBounds.y, maxBounds.y);
                newPosition.z = Mathf.Clamp(newPosition.z, minBounds.z, maxBounds.z);
            }
            
            transform.position = newPosition;
        }
        
        /// <summary>
        /// Handle camera look/rotation
        /// </summary>
        private void HandleLook()
        {
            // Only rotate if left mouse button is pressed OR Alt key is pressed
            if (!isLeftMousePressed && !isAltPressed) return;
            
            if (lookInput.magnitude < 0.1f) return;
            
            // Apply mouse sensitivity
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;
            
            // Invert Y if needed
            if (invertY)
                mouseY = -mouseY;
            
            // Update rotation
            yRotation += mouseX;
            xRotation -= mouseY;
            
            // Clamp vertical rotation
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            
            // Apply rotation
            //transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
        
        /// <summary>
        /// Handle move input
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        
        /// <summary>
        /// Handle move input cancellation
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            moveInput = Vector2.zero;
        }
        
        /// <summary>
        /// Handle look input
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }
        
        /// <summary>
        /// Handle look input cancellation
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            lookInput = Vector2.zero;
        }
        
        /// <summary>
        /// Handle sprint input start
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            isSprinting = true;
        }
        
        /// <summary>
        /// Handle sprint input end
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            isSprinting = false;
        }
        
        /// <summary>
        /// Handle left mouse button press
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnLeftMousePerformed(InputAction.CallbackContext context)
        {
            isLeftMousePressed = true;
        }
        
        /// <summary>
        /// Handle left mouse button release
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnLeftMouseCanceled(InputAction.CallbackContext context)
        {
            isLeftMousePressed = false;
        }
        
        /// <summary>
        /// Handle Alt key press
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnAltKeyPerformed(InputAction.CallbackContext context)
        {
            isAltPressed = true;
        }
        
        /// <summary>
        /// Handle Alt key release
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnAltKeyCanceled(InputAction.CallbackContext context)
        {
            isAltPressed = false;
        }
        
        /// <summary>
        /// Set camera bounds
        /// </summary>
        /// <param name="min">Minimum bounds</param>
        /// <param name="max">Maximum bounds</param>
        public void SetBounds(Vector3 min, Vector3 max)
        {
            minBounds = min;
            maxBounds = max;
        }
        
        /// <summary>
        /// Set movement speed
        /// </summary>
        /// <param name="speed">Movement speed</param>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }
        
        /// <summary>
        /// Set fast movement speed
        /// </summary>
        /// <param name="speed">Fast movement speed</param>
        public void SetFastMoveSpeed(float speed)
        {
            fastMoveSpeed = speed;
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            GUILayout.BeginArea(new Rect(10, 600, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Camera Controls", GUI.skin.box);
            GUILayout.Space(5);
            
            GUILayout.Label($"Position: {transform.position}", GUI.skin.label);
            GUILayout.Label($"Rotation: {transform.rotation.eulerAngles}", GUI.skin.label);
            GUILayout.Label($"Speed: {(isSprinting ? "Fast" : "Normal")}", GUI.skin.label);
            GUILayout.Label($"Move Input: {moveInput}", GUI.skin.label);
            GUILayout.Label($"Left Mouse: {(isLeftMousePressed ? "Pressed" : "Released")}", GUI.skin.label);
            GUILayout.Label($"Alt Key: {(isAltPressed ? "Pressed" : "Released")}", GUI.skin.label);
            GUILayout.Label($"Can Rotate: {(isLeftMousePressed || isAltPressed ? "Yes" : "No")}", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}



