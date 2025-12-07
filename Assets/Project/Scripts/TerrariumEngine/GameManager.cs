using UnityEngine;
using TerrariumEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TerrariumEngine
{
    /// <summary>
    /// Main game manager that coordinates all terrarium systems
    /// </summary>
    public class GameManager : MonoBehaviour, IDebuggable
    {
        [Header("Game Settings")]
        [SerializeField] private GameObject queenPrefab;
        [SerializeField] private GameObject workerPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        public string DebugName => "GameManager";
        public bool IsDebugEnabled { get; set; } = true;
        
        private TerrariumManager terrariumManager;
        private TimeManager timeManager;
        private Input.InputHandler inputHandler;
        private QueenNPC queen;
        
        public TerrariumManager TerrariumManager => terrariumManager;
        public TimeManager TimeManager => timeManager;
        public Input.InputHandler InputHandler => inputHandler;
        public QueenNPC Queen => queen;
        
        private void Awake()
        {
            // Find or create core managers
            terrariumManager = FindFirstObjectByType<TerrariumManager>();
            if (terrariumManager == null)
            {
                GameObject terrariumObj = new GameObject("TerrariumManager");
                terrariumManager = terrariumObj.AddComponent<TerrariumManager>();
            }
            
            timeManager = FindFirstObjectByType<TimeManager>();
            if (timeManager == null)
            {
                GameObject timeObj = new GameObject("TimeManager");
                timeManager = timeObj.AddComponent<TimeManager>();
            }
            
            inputHandler = FindFirstObjectByType<Input.InputHandler>();
            if (inputHandler == null)
            {
                GameObject inputObj = new GameObject("InputHandler");
                inputHandler = inputObj.AddComponent<Input.InputHandler>();
            }
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void Start()
        {
            InitializeGame();
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Initialize the game
        /// </summary>
        private void InitializeGame()
        {
            SpawnQueen();
            
            if (IsDebugEnabled)
            {
                Debug.Log("GameManager: Game initialized successfully");
            }
        }
        
        /// <summary>
        /// Spawn the queen NPC
        /// </summary>
        private void SpawnQueen()
        {
            if (queenPrefab == null)
            {
                // Create a simple queen prefab
                queenPrefab = CreateQueenPrefab();
            }
            
            // Get the proper spawn position on top of sand blocks at terrarium center
            Vector3 spawnPosition = CalculateQueenSpawnPosition();
            
            GameObject queenObj = Instantiate(queenPrefab, spawnPosition, Quaternion.identity);
            queen = queenObj.GetComponent<QueenNPC>();
            
            if (queen == null)
            {
                queen = queenObj.AddComponent<QueenNPC>();
            }
            
            // Ensure Queen has proper physics setup
            SetupQueenPhysics(queenObj);
            
            // Set worker prefab for queen
            if (workerPrefab == null)
            {
                workerPrefab = CreateWorkerPrefab();
            }
            
            // Set the worker prefab using the public method
            queen.SetWorkerPrefab(workerPrefab);
            
            queen.name = "Queen";
            
            if (IsDebugEnabled)
            {
                Debug.Log($"GameManager: Queen spawned at {spawnPosition}");
            }
        }
        
        /// <summary>
        /// Calculate the optimal spawn position for the Queen on top of the terrarium
        /// </summary>
        /// <returns>World position for Queen spawn</returns>
        private Vector3 CalculateQueenSpawnPosition()
        {
            // First try to get center position from terrarium
            Vector3 centerPosition = terrariumManager.GetCenterPosition();
            
            if (centerPosition != Vector3.zero)
            {
                return centerPosition;
            }
            
            // If no terrain found, calculate based on terrarium bounds
            Vector3 terrariumSize = terrariumManager.TerrariumSize;
            Vector3 terrariumCenter = terrariumManager.transform.position + terrariumSize * 0.5f;
            
            // Calculate expected terrain height (30% of terrarium height)
            float expectedTerrainHeight = terrariumSize.y * 0.3f;
            Vector3 fallbackPosition = new Vector3(
                terrariumCenter.x,
                terrariumManager.transform.position.y + expectedTerrainHeight + 1f, // 1 unit above terrain
                terrariumCenter.z
            );
            
            if (IsDebugEnabled)
            {
                Debug.LogWarning($"GameManager: No terrain found, using calculated fallback position: {fallbackPosition}");
            }
            
            return fallbackPosition;
        }
        
        /// <summary>
        /// Setup physics for the Queen to ensure proper positioning
        /// </summary>
        /// <param name="queenObj">Queen GameObject</param>
        private void SetupQueenPhysics(GameObject queenObj)
        {
            Rigidbody rb = queenObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Ensure Queen is properly positioned on terrain
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                // Small delay to let physics settle
                StartCoroutine(StabilizeQueenPosition(queenObj));
            }
        }
        
        /// <summary>
        /// Coroutine to stabilize Queen position after spawn
        /// </summary>
        /// <param name="queenObj">Queen GameObject</param>
        /// <returns>IEnumerator for coroutine</returns>
        private System.Collections.IEnumerator StabilizeQueenPosition(GameObject queenObj)
        {
            yield return new WaitForFixedUpdate();
            
            if (queenObj != null)
            {
                Rigidbody rb = queenObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        
        /// <summary>
        /// Create a simple queen prefab
        /// </summary>
        /// <returns>Queen prefab GameObject</returns>
        private GameObject CreateQueenPrefab()
        {
            GameObject queen = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            queen.name = "QueenPrefab";
            queen.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            
            // Set queen color
            Renderer renderer = queen.GetComponent<Renderer>();
            renderer.material.color = Color.red;
            
            // Add rigidbody
            Rigidbody rb = queen.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.linearDamping = 2f;
            rb.angularDamping = 5f;
            
            return queen;
        }
        
        /// <summary>
        /// Create a simple worker prefab
        /// </summary>
        /// <returns>Worker prefab GameObject</returns>
        private GameObject CreateWorkerPrefab()
        {
            GameObject worker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            worker.name = "WorkerPrefab";
            worker.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            
            // Set worker color
            Renderer renderer = worker.GetComponent<Renderer>();
            renderer.material.color = Color.blue;
            
            // Add rigidbody
            Rigidbody rb = worker.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 1f;
            rb.angularDamping = 3f;
            
            return worker;
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            GUILayout.BeginArea(new Rect(10, 500, 300, 100));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Game Status", GUI.skin.box);
            GUILayout.Space(5);
            
            GUILayout.Label($"Queen: {(queen != null ? "Alive" : "Dead")}", GUI.skin.label);
            if (queen != null)
            {
                GUILayout.Label($"Workers: {queen.GetWorkerCount()}", GUI.skin.label);
            }
            GUILayout.Label($"Time Scale: {Time.timeScale:F1}x", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
