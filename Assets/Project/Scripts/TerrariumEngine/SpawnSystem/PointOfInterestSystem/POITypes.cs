using UnityEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Types of Points of Interest available in the system
    /// </summary>
    public enum POIType
    {
        Food,       // Food sources and consumables
        Water,      // Water sources and consumables
        Cool,       // Cold areas that provide cooling
        Warm,       // Hot areas that provide warmth
        Safety,     // Safe areas that provide protection
        Danger      // Dangerous areas that should be avoided
    }
    
    /// <summary>
    /// Intensity levels for POI signals
    /// </summary>
    public enum POIIntensity
    {
        VeryLow = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        VeryHigh = 5
    }
    
    /// <summary>
    /// Information about a Point of Interest
    /// </summary>
    [System.Serializable]
    public struct POIInfo
    {
        public POIType type;
        public POIIntensity intensity;
        public Vector3 position;
        public float radius;
        public float strength;
        public string sourceName;
        public bool isActive;
        public float lifetime; // -1 for permanent, >0 for temporary
        public float age; // Current age of the POI
        
        public bool IsPermanent => lifetime < 0;
        public bool IsExpired => lifetime > 0 && age >= lifetime;
        public float AgePercentage => lifetime > 0 ? age / lifetime : 0f;
    }
    
    /// <summary>
    /// POI detection result for entities
    /// </summary>
    [System.Serializable]
    public struct POIDetection
    {
        public POIInfo poiInfo;
        public float distance;
        public float signalStrength; // How strong the signal is at this distance
        public float priority; // Calculated priority for this entity
        public bool isReachable; // Whether the entity can reach this POI
        
        public float GetEffectiveStrength()
        {
            // Signal strength decreases with distance
            return poiInfo.strength * (1f - (distance / poiInfo.radius));
        }
    }
}

