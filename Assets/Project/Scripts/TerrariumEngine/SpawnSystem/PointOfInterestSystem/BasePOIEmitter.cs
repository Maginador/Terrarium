using UnityEngine;
using TerrariumEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Base class for Point of Interest emitters
    /// Provides common functionality for all POI types
    /// </summary>
    public abstract class BasePOIEmitter : MonoBehaviour, IPointOfInterest, IDebuggable
    {
        [Header("POI Settings")]
        [SerializeField] protected POIType poiType = POIType.Food;
        [SerializeField] protected POIIntensity intensity = POIIntensity.Medium;
        [SerializeField] protected float radius = 10f;
        [SerializeField] protected float strength = 1f;
        [SerializeField] protected bool isActive = true;
        [SerializeField] protected string sourceName = "POI Source";
        
        [Header("Lifetime Settings")]
        [SerializeField] protected float lifetime = -1f; // -1 for permanent
        [SerializeField] protected bool destroyWhenExpired = true;
        
        [Header("Visual Settings")]
        [SerializeField] protected bool showVisualIndicator = true;
        [SerializeField] protected Color indicatorColor = Color.yellow;
        [SerializeField] protected float indicatorHeight = 2f;
        
        [Header("Debug")]
        [SerializeField] protected bool showDebugInfo = true;
        
        public string DebugName => $"{GetType().Name}_{sourceName}";
        public bool IsDebugEnabled { get; set; } = true;
        
        // IPointOfInterest Properties
        public POIType POIType => poiType;
        public POIIntensity Intensity => intensity;
        public Vector3 Position => transform.position;
        public float Radius => radius;
        public float Strength => strength;
        public bool IsActive => isActive;
        public string SourceName => sourceName;
        
        // Internal state
        protected float currentAge = 0f;
        protected bool isInitialized = false;
        
        // Events
        public System.Action<IPointOfInterest> OnPOIActivated { get; set; }
        public System.Action<IPointOfInterest> OnPOIDeactivated { get; set; }
        public System.Action<IPointOfInterest, POIIntensity> OnIntensityChanged { get; set; }
        
        protected virtual void Awake()
        {
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
        }
        
        protected virtual void Start()
        {
            InitializePOI();
            isInitialized = true;
        }
        
        protected virtual void Update()
        {
            if (!isInitialized) return;
            
            // Update age
            currentAge += Time.deltaTime;
            
            // Check if expired
            if (lifetime > 0 && currentAge >= lifetime)
            {
                HandleExpiration();
            }
            
            // Update POI-specific behavior
            UpdatePOIBehavior();
        }
        
        protected virtual void OnDestroy()
        {
            // Deactivate POI before destruction
            if (isActive)
            {
                SetActive(false);
            }
            
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Initialize POI-specific settings - override in derived classes
        /// </summary>
        protected abstract void InitializePOI();
        
        /// <summary>
        /// Update POI-specific behavior - override in derived classes
        /// </summary>
        protected virtual void UpdatePOIBehavior()
        {
            // Override in derived classes for specific behavior
        }
        
        /// <summary>
        /// Handle POI expiration
        /// </summary>
        protected virtual void HandleExpiration()
        {
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: POI expired after {currentAge:F1} seconds");
            }
            
            if (destroyWhenExpired)
            {
                Destroy(gameObject);
            }
            else
            {
                SetActive(false);
            }
        }
        
        /// <summary>
        /// Set the POI active state
        /// </summary>
        /// <param name="active">Whether the POI should be active</param>
        public virtual void SetActive(bool active)
        {
            if (isActive == active) return;
            
            isActive = active;
            
            if (active)
            {
                OnPOIActivated?.Invoke(this);
                if (IsDebugEnabled)
                {
                    Debug.Log($"{DebugName}: POI activated");
                }
            }
            else
            {
                OnPOIDeactivated?.Invoke(this);
                if (IsDebugEnabled)
                {
                    Debug.Log($"{DebugName}: POI deactivated");
                }
            }
        }
        
        /// <summary>
        /// Set the POI intensity
        /// </summary>
        /// <param name="newIntensity">New intensity level</param>
        public virtual void SetIntensity(POIIntensity newIntensity)
        {
            if (intensity == newIntensity) return;
            
            POIIntensity oldIntensity = intensity;
            intensity = newIntensity;
            
            OnIntensityChanged?.Invoke(this, oldIntensity);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Intensity changed from {oldIntensity} to {newIntensity}");
            }
        }
        
        /// <summary>
        /// Set the POI radius
        /// </summary>
        /// <param name="newRadius">New radius</param>
        public virtual void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0f, newRadius);
        }
        
        /// <summary>
        /// Set the POI strength
        /// </summary>
        /// <param name="newStrength">New strength (0-1)</param>
        public virtual void SetStrength(float newStrength)
        {
            strength = Mathf.Clamp01(newStrength);
        }
        
        public POIInfo GetPOIInfo()
        {
            return new POIInfo
            {
                type = poiType,
                intensity = intensity,
                position = Position,
                radius = radius,
                strength = strength,
                sourceName = sourceName,
                isActive = isActive,
                lifetime = lifetime,
                age = currentAge
            };
        }
        
        public float GetSignalStrengthAtDistance(float distance)
        {
            if (!isActive || distance > radius)
            {
                return 0f;
            }
            
            // Signal strength decreases with distance
            float distanceFactor = 1f - (distance / radius);
            return strength * distanceFactor * (int)intensity / 5f; // Normalize intensity to 0-1
        }
        
        public bool IsWithinInfluence(Vector3 position)
        {
            if (!isActive)
            {
                return false;
            }
            
            float distance = Vector3.Distance(Position, position);
            return distance <= radius;
        }
        
        public virtual void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        protected virtual void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw POI info above the emitter
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * indicatorHeight);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                // POI type and name
                GUI.color = GetPOIColor();
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"{poiType} - {sourceName}");
                yOffset += labelHeight;
                
                // Intensity and strength
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Intensity: {intensity} | Strength: {strength:F2}");
                yOffset += labelHeight;
                
                // Radius and age
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Radius: {radius:F1}m | Age: {currentAge:F1}s");
                yOffset += labelHeight;
                
                // Lifetime info
                if (lifetime > 0)
                {
                    float lifetimePercent = currentAge / lifetime;
                    GUI.color = lifetimePercent > 0.8f ? Color.red : lifetimePercent > 0.6f ? Color.yellow : Color.white;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Lifetime: {lifetimePercent:P0}");
                    GUI.color = Color.white;
                }
                
                // Active status
                GUI.color = isActive ? Color.green : Color.red;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset + labelHeight, 160, labelHeight), 
                    isActive ? "ACTIVE" : "INACTIVE");
                GUI.color = Color.white;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!IsDebugEnabled) return;
            
            // Draw POI radius
            Gizmos.color = GetPOIColor();
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Gizmos.DrawSphere(transform.position, radius);
            
            // Draw POI center
            Gizmos.color = GetPOIColor();
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw intensity indicator
            if (showVisualIndicator)
            {
                Gizmos.color = indicatorColor;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * indicatorHeight);
            }
        }
        
        /// <summary>
        /// Get color for this POI type - override in derived classes
        /// </summary>
        /// <returns>Color for this POI type</returns>
        protected virtual Color GetPOIColor()
        {
            switch (poiType)
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

