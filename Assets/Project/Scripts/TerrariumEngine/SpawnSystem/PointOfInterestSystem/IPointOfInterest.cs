using UnityEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Interface for objects that can emit Point of Interest signals
    /// </summary>
    public interface IPointOfInterest
    {
        /// <summary>
        /// Type of POI this object emits
        /// </summary>
        POIType POIType { get; }
        
        /// <summary>
        /// Intensity of the POI signal
        /// </summary>
        POIIntensity Intensity { get; }
        
        /// <summary>
        /// Position of the POI
        /// </summary>
        Vector3 Position { get; }
        
        /// <summary>
        /// Radius of influence for this POI
        /// </summary>
        float Radius { get; }
        
        /// <summary>
        /// Strength of the POI signal (0-1)
        /// </summary>
        float Strength { get; }
        
        /// <summary>
        /// Whether this POI is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Name of the POI source
        /// </summary>
        string SourceName { get; }
        
        /// <summary>
        /// Get current POI information
        /// </summary>
        /// <returns>Current POI info</returns>
        POIInfo GetPOIInfo();
        
        /// <summary>
        /// Calculate signal strength at a given distance
        /// </summary>
        /// <param name="distance">Distance from POI</param>
        /// <returns>Signal strength at that distance</returns>
        float GetSignalStrengthAtDistance(float distance);
        
        /// <summary>
        /// Check if a position is within this POI's influence
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <returns>True if within influence</returns>
        bool IsWithinInfluence(Vector3 position);
        
        /// <summary>
        /// Events for POI state changes
        /// </summary>
        System.Action<IPointOfInterest> OnPOIActivated { get; set; }
        System.Action<IPointOfInterest> OnPOIDeactivated { get; set; }
        System.Action<IPointOfInterest, POIIntensity> OnIntensityChanged { get; set; }
    }
    
    /// <summary>
    /// Interface for entities that can detect and respond to POIs
    /// </summary>
    public interface IPOIDetector
    {
        /// <summary>
        /// Maximum detection range for POIs
        /// </summary>
        float DetectionRange { get; }
        
        /// <summary>
        /// Types of POIs this entity can detect
        /// </summary>
        POIType[] DetectableTypes { get; }
        
        /// <summary>
        /// Minimum signal strength required to detect a POI
        /// </summary>
        float MinDetectionThreshold { get; }
        
        /// <summary>
        /// Detect POIs in range
        /// </summary>
        /// <returns>List of detected POIs</returns>
        POIDetection[] DetectPOIs();
        
        /// <summary>
        /// Get the most important POI of a specific type
        /// </summary>
        /// <param name="type">Type of POI to find</param>
        /// <returns>Most important POI of that type, or null if none found</returns>
        POIDetection? GetMostImportantPOI(POIType type);
        
        /// <summary>
        /// Get all POIs of a specific type, sorted by priority
        /// </summary>
        /// <param name="type">Type of POI to find</param>
        /// <returns>Array of POIs sorted by priority (highest first)</returns>
        POIDetection[] GetPOIsOfType(POIType type);
        
        /// <summary>
        /// Calculate priority for a specific POI
        /// </summary>
        /// <param name="poi">POI to calculate priority for</param>
        /// <returns>Priority value (higher = more important)</returns>
        float CalculatePOIPriority(POIDetection poi);
        
        /// <summary>
        /// Events for POI detection
        /// </summary>
        System.Action<POIDetection> OnPOIDetected { get; set; }
        System.Action<POIDetection> OnPOILost { get; set; }
        System.Action<POIDetection, float> OnPOIPriorityChanged { get; set; }
    }
}

