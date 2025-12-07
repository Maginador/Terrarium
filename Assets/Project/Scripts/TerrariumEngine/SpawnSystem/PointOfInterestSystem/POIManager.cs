using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Manages all Points of Interest in the terrarium
    /// Provides centralized POI detection and management
    /// </summary>
    public class POIManager : MonoBehaviour, IDebuggable
    {
        [Header("POI Detection Settings")]
        [SerializeField] private float globalDetectionRange = 50f;
        [SerializeField] private float detectionUpdateInterval = 0.5f; // How often to update POI detection
        [SerializeField] private bool enablePOITracking = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showPOIGizmos = true;
        
        public string DebugName => "POIManager";
        public bool IsDebugEnabled { get; set; } = true;
        
        // POI tracking
        private List<IPointOfInterest> allPOIs = new List<IPointOfInterest>();
        private Dictionary<POIType, List<IPointOfInterest>> poisByType = new Dictionary<POIType, List<IPointOfInterest>>();
        private float lastDetectionUpdate = 0f;
        
        // Events
        public System.Action<IPointOfInterest> OnPOIRegistered;
        public System.Action<IPointOfInterest> OnPOIUnregistered;
        public System.Action<POIType, IPointOfInterest> OnPOIOfTypeChanged;
        
        // Singleton instance
        public static POIManager Instance { get; private set; }
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // Initialize POI type dictionary
            foreach (POIType type in System.Enum.GetValues(typeof(POIType)))
            {
                poisByType[type] = new List<IPointOfInterest>();
            }
            
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        private void Start()
        {
            // Find all existing POIs in the scene
            FindAndRegisterExistingPOIs();
        }
        
        private void Update()
        {
            if (!enablePOITracking) return;
            
            // Update POI detection at intervals
            if (Time.time - lastDetectionUpdate >= detectionUpdateInterval)
            {
                UpdatePOIDetection();
                lastDetectionUpdate = Time.time;
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Find and register all existing POIs in the scene
        /// </summary>
        private void FindAndRegisterExistingPOIs()
        {
            // Find all POI emitters
            var poiEmitters = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb is IPointOfInterest)
                .Cast<IPointOfInterest>()
                .ToList();
            
            foreach (var poi in poiEmitters)
            {
                RegisterPOI(poi);
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Found and registered {poiEmitters.Count} existing POIs");
            }
        }
        
        /// <summary>
        /// Register a new POI
        /// </summary>
        /// <param name="poi">POI to register</param>
        public void RegisterPOI(IPointOfInterest poi)
        {
            if (poi == null || allPOIs.Contains(poi)) return;
            
            allPOIs.Add(poi);
            poisByType[poi.POIType].Add(poi);
            
            OnPOIRegistered?.Invoke(poi);
            OnPOIOfTypeChanged?.Invoke(poi.POIType, poi);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Registered {poi.POIType} POI: {poi.SourceName}");
            }
        }
        
        /// <summary>
        /// Unregister a POI
        /// </summary>
        /// <param name="poi">POI to unregister</param>
        public void UnregisterPOI(IPointOfInterest poi)
        {
            if (poi == null || !allPOIs.Contains(poi)) return;
            
            allPOIs.Remove(poi);
            poisByType[poi.POIType].Remove(poi);
            
            OnPOIUnregistered?.Invoke(poi);
            OnPOIOfTypeChanged?.Invoke(poi.POIType, poi);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Unregistered {poi.POIType} POI: {poi.SourceName}");
            }
        }
        
        /// <summary>
        /// Update POI detection and cleanup inactive POIs
        /// </summary>
        private void UpdatePOIDetection()
        {
            // Clean up inactive or destroyed POIs
            var inactivePOIs = allPOIs.Where(poi => poi == null || !poi.IsActive).ToList();
            
            foreach (var poi in inactivePOIs)
            {
                UnregisterPOI(poi);
            }
        }
        
        /// <summary>
        /// Get all POIs of a specific type
        /// </summary>
        /// <param name="type">Type of POI to get</param>
        /// <returns>List of POIs of that type</returns>
        public List<IPointOfInterest> GetPOIsOfType(POIType type)
        {
            if (poisByType.ContainsKey(type))
            {
                return poisByType[type].Where(poi => poi != null && poi.IsActive).ToList();
            }
            return new List<IPointOfInterest>();
        }
        
        /// <summary>
        /// Get all active POIs
        /// </summary>
        /// <returns>List of all active POIs</returns>
        public List<IPointOfInterest> GetAllActivePOIs()
        {
            return allPOIs.Where(poi => poi != null && poi.IsActive).ToList();
        }
        
        /// <summary>
        /// Get POIs within range of a position
        /// </summary>
        /// <param name="position">Position to check from</param>
        /// <param name="range">Detection range</param>
        /// <returns>List of POIs within range</returns>
        public List<IPointOfInterest> GetPOIsInRange(Vector3 position, float range)
        {
            return allPOIs.Where(poi => poi != null && poi.IsActive && 
                Vector3.Distance(position, poi.Position) <= range).ToList();
        }
        
        /// <summary>
        /// Get POIs of a specific type within range
        /// </summary>
        /// <param name="position">Position to check from</param>
        /// <param name="range">Detection range</param>
        /// <param name="type">Type of POI to get</param>
        /// <returns>List of POIs of that type within range</returns>
        public List<IPointOfInterest> GetPOIsInRange(Vector3 position, float range, POIType type)
        {
            return GetPOIsOfType(type).Where(poi => 
                Vector3.Distance(position, poi.Position) <= range).ToList();
        }
        
        /// <summary>
        /// Get the closest POI of a specific type
        /// </summary>
        /// <param name="position">Position to check from</param>
        /// <param name="type">Type of POI to find</param>
        /// <param name="maxRange">Maximum range to search (0 = no limit)</param>
        /// <returns>Closest POI of that type, or null if none found</returns>
        public IPointOfInterest GetClosestPOI(Vector3 position, POIType type, float maxRange = 0f)
        {
            var poisOfType = GetPOIsOfType(type);
            
            if (poisOfType.Count == 0) return null;
            
            IPointOfInterest closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (var poi in poisOfType)
            {
                float distance = Vector3.Distance(position, poi.Position);
                
                if ((maxRange <= 0f || distance <= maxRange) && distance < closestDistance)
                {
                    closest = poi;
                    closestDistance = distance;
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// Get POI count by type
        /// </summary>
        /// <param name="type">Type to count</param>
        /// <returns>Number of active POIs of that type</returns>
        public int GetPOICount(POIType type)
        {
            return GetPOIsOfType(type).Count;
        }
        
        /// <summary>
        /// Get total POI count
        /// </summary>
        /// <returns>Total number of active POIs</returns>
        public int GetTotalPOICount()
        {
            return GetAllActivePOIs().Count;
        }
        
        /// <summary>
        /// Check if there are any POIs of a specific type
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if any POIs of that type exist</returns>
        public bool HasPOIOfType(POIType type)
        {
            return GetPOICount(type) > 0;
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            GUILayout.BeginArea(new Rect(10, 600, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("POI Manager", GUI.skin.box);
            GUILayout.Space(5);
            
            GUILayout.Label($"Total POIs: {GetTotalPOICount()}", GUI.skin.label);
            GUILayout.Space(5);
            
            // Show POI counts by type
            foreach (POIType type in System.Enum.GetValues(typeof(POIType)))
            {
                int count = GetPOICount(type);
                if (count > 0)
                {
                    GUI.color = GetPOITypeColor(type);
                    GUILayout.Label($"{type}: {count}", GUI.skin.label);
                    GUI.color = Color.white;
                }
            }
            
            GUILayout.Space(5);
            GUILayout.Label($"Detection Range: {globalDetectionRange:F1}m", GUI.skin.label);
            GUILayout.Label($"Update Interval: {detectionUpdateInterval:F1}s", GUI.skin.label);
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnDrawGizmos()
        {
            if (!showPOIGizmos || !IsDebugEnabled) return;
            
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, globalDetectionRange);
            
            // Draw all POI ranges
            foreach (var poi in GetAllActivePOIs())
            {
                if (poi == null) continue;
                
                Gizmos.color = GetPOITypeColor(poi.POIType);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.2f);
                Gizmos.DrawSphere(poi.Position, poi.Radius);
                
                // Draw POI center
                Gizmos.color = GetPOITypeColor(poi.POIType);
                Gizmos.DrawWireSphere(poi.Position, 0.5f);
            }
        }
        
        /// <summary>
        /// Get color for POI type
        /// </summary>
        /// <param name="type">POI type</param>
        /// <returns>Color for that POI type</returns>
        private Color GetPOITypeColor(POIType type)
        {
            switch (type)
            {
                case POIType.Food:
                    return Color.green;
                case POIType.Water:
                    return Color.blue;
                case POIType.Cool:
                    return Color.cyan;
                case POIType.Warm:
                    return Color.red;
                case POIType.Safety:
                    return Color.white;
                case POIType.Danger:
                    return Color.magenta;
                default:
                    return Color.yellow;
            }
        }
    }
}

