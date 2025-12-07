using UnityEngine;
using TerrariumEngine.AI;
using TerrariumEngine.SpawnSystem.PointOfInterestSystem;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Food item that can be consumed by NPCs to satisfy hunger
    /// Also emits Food POI signals for AI detection
    /// </summary>
    public class FoodItem : BaseConsumable, IPointOfInterest
    {
        [Header("Food Specific Settings")]
        [SerializeField] private FoodType foodType = FoodType.Generic;
        [SerializeField] private float hungerSatisfaction = 20f; // How much hunger this satisfies
        [SerializeField] private float energyBoost = 5f; // Energy boost when consumed
        
        [Header("Food Quality")]
        [SerializeField] private FoodQuality quality = FoodQuality.Normal;
        [SerializeField] private float qualityMultiplier = 1f; // Multiplier for nutrition based on quality
        
        [Header("POI Settings")]
        [SerializeField] private float poiRadius = 15f; // Detection radius for AI
        [SerializeField] private bool emitPOI = true; // Whether to emit POI signals
        
        // Properties
        public FoodType FoodType => foodType;
        public float HungerSatisfaction => hungerSatisfaction * qualityMultiplier;
        public float EnergyBoost => energyBoost * qualityMultiplier;
        public FoodQuality Quality => quality;
        public float QualityMultiplier => qualityMultiplier;
        
        // IPointOfInterest Properties
        public POIType POIType => POIType.Food;
        public POIIntensity Intensity => GetPOIIntensity();
        public Vector3 Position => transform.position;
        public float Radius => poiRadius;
        public float Strength => GetPOIStrength();
        public bool IsActive => emitPOI && !IsEmpty && !IsRotten;
        public string SourceName => $"{foodType} Food";
        
        // POI Events
        public System.Action<IPointOfInterest> OnPOIActivated { get; set; }
        public System.Action<IPointOfInterest> OnPOIDeactivated { get; set; }
        public System.Action<IPointOfInterest, POIIntensity> OnIntensityChanged { get; set; }
        
        protected override void InitializeConsumable()
        {
            // Set base nutrition value based on hunger satisfaction
            nutritionValue = hungerSatisfaction;
            
            // Apply quality multiplier
            ApplyQualityMultiplier();
            
            // Set food-specific rot settings
            rotRate = 0.1f; // Food rots faster than water
            maxRot = 1f;
            canRot = true;
            destroyWhenRotten = true;
            
            // Set food-specific lifetime
            maxLifetime = 300f; // 5 minutes default
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Initialized as {foodType} food with {quality} quality");
            }
        }
        
        protected override void HandleRot()
        {
            base.HandleRot();
            
            // Food-specific rot handling
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Food has rotted!");
            }
            
            // Notify POI deactivation when food rots
            if (emitPOI)
            {
                OnPOIDeactivated?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Apply quality multiplier to nutrition values
        /// </summary>
        private void ApplyQualityMultiplier()
        {
            switch (quality)
            {
                case FoodQuality.Poor:
                    qualityMultiplier = 0.5f;
                    break;
                case FoodQuality.Normal:
                    qualityMultiplier = 1f;
                    break;
                case FoodQuality.Good:
                    qualityMultiplier = 1.5f;
                    break;
                case FoodQuality.Excellent:
                    qualityMultiplier = 2f;
                    break;
            }
            
            // Update nutrition values
            nutritionValue = hungerSatisfaction * qualityMultiplier;
            healthBonus = healthBonus * qualityMultiplier;
            stressReduction = stressReduction * qualityMultiplier;
        }
        
        protected override void ApplyConsumptionEffects(BaseNPC consumer, float amount)
        {
            base.ApplyConsumptionEffects(consumer, amount);
            
            // Apply hunger satisfaction
            if (hungerSatisfaction > 0f)
            {
                float actualSatisfaction = HungerSatisfaction * amount;
                consumer.ModifyStat(StatType.Food, actualSatisfaction);
            }
            
            // Apply energy boost (if we add energy stat later)
            // consumer.ModifyStat(StatType.Energy, energyBoost * amount);
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Satisfied {HungerSatisfaction * amount} hunger for {consumer.DebugName}");
            }
        }
        
        public override bool CanBeConsumedBy(BaseNPC consumer)
        {
            if (!base.CanBeConsumedBy(consumer))
                return false;
            
            // Can't consume rotten food
            if (IsRotten)
                return false;
            
            return true;
        }
        
        public override float GetConsumptionPriority(BaseNPC consumer)
        {
            if (!CanBeConsumedBy(consumer))
                return 0f;
            
            // Get hunger stat from consumer
            var hungerStat = consumer.GetStat(StatType.Food);
            if (hungerStat == null)
                return base.GetConsumptionPriority(consumer);
            
            // Higher priority if more hungry
            float hungerNeed = 1f - hungerStat.GetPercentage();
            
            // Factor in food quality and rot
            float qualityFactor = qualityMultiplier;
            float rotFactor = 1f - RotPercentage;
            
            return nutritionValue * hungerNeed * qualityFactor * rotFactor;
        }
        
        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
            
            if (itemRenderer == null) return;
            
            // Update color based on rot
            if (canRot && !IsFresh)
            {
                Color rotColor = Color.Lerp(fullColor, Color.gray, RotPercentage);
                itemRenderer.material.color = rotColor;
            }
        }
        
        /// <summary>
        /// Get food information for NPCs
        /// </summary>
        /// <returns>Food-specific information</returns>
        public FoodInfo GetFoodInfo()
        {
            return new FoodInfo
            {
                baseInfo = GetConsumableInfo(),
                foodType = foodType,
                hungerSatisfaction = HungerSatisfaction,
                energyBoost = energyBoost,
                quality = quality,
                isRotten = IsRotten,
                isFresh = IsFresh
            };
        }
        
        protected override void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw food info above the item
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                // Food name and type
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"{itemName} ({foodType})");
                yOffset += labelHeight;
                
                // Amount
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Amount: {currentAmount:F1}/{maxAmount:F1}");
                yOffset += labelHeight;
                
                // Quality and spoilage
                string qualityColor = quality == FoodQuality.Excellent ? "green" : 
                                    quality == FoodQuality.Good ? "cyan" : 
                                    quality == FoodQuality.Poor ? "red" : "white";
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Quality: {quality} (x{qualityMultiplier:F1})");
                yOffset += labelHeight;
                
                if (canRot)
                {
                    GUI.color = IsRotten ? Color.red : IsFresh ? Color.green : Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Rot: {RotPercentage:P0}");
                    GUI.color = Color.white;
                }
                
                // Nutrition
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset + labelHeight, 160, labelHeight), 
                    $"Hunger: {HungerSatisfaction:F1}");
            }
        }
        
        #region IPointOfInterest Implementation
        
        /// <summary>
        /// Get POI intensity based on food quality and freshness
        /// </summary>
        /// <returns>POI intensity</returns>
        private POIIntensity GetPOIIntensity()
        {
            if (!IsActive) return POIIntensity.VeryLow;
            
            // Base intensity on quality
            float qualityFactor = (int)quality / 4f; // 0-1 based on quality
            float freshnessFactor = 1f - RotPercentage; // 1 when fresh, 0 when rotten
            
            float combinedFactor = qualityFactor * freshnessFactor;
            
            if (combinedFactor >= 0.8f)
                return POIIntensity.VeryHigh;
            else if (combinedFactor >= 0.6f)
                return POIIntensity.High;
            else if (combinedFactor >= 0.4f)
                return POIIntensity.Medium;
            else if (combinedFactor >= 0.2f)
                return POIIntensity.Low;
            else
                return POIIntensity.VeryLow;
        }
        
        /// <summary>
        /// Get POI strength based on amount and quality
        /// </summary>
        /// <returns>POI strength (0-1)</returns>
        private float GetPOIStrength()
        {
            if (!IsActive) return 0f;
            
            float amountFactor = AmountPercentage;
            float qualityFactor = (int)quality / 4f;
            float freshnessFactor = 1f - RotPercentage;
            
            return amountFactor * qualityFactor * freshnessFactor;
        }
        
        public POIInfo GetPOIInfo()
        {
            return new POIInfo
            {
                type = POIType,
                intensity = Intensity,
                position = Position,
                radius = Radius,
                strength = Strength,
                sourceName = SourceName,
                isActive = IsActive,
                lifetime = -1f, // Food items are permanent until consumed/rotten
                age = currentLifetime
            };
        }
        
        public float GetSignalStrengthAtDistance(float distance)
        {
            if (!IsActive || distance > Radius)
            {
                return 0f;
            }
            
            // Signal strength decreases with distance
            float distanceFactor = 1f - (distance / Radius);
            return Strength * distanceFactor * (int)Intensity / 5f; // Normalize intensity to 0-1
        }
        
        public bool IsWithinInfluence(Vector3 position)
        {
            if (!IsActive)
            {
                return false;
            }
            
            float distance = Vector3.Distance(Position, position);
            return distance <= Radius;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Types of food available
    /// </summary>
    public enum FoodType
    {
        Generic,
        Fruit,
        Vegetable,
        Meat,
        Grain,
        Nectar,
        Seeds,
        Berries
    }
    
    /// <summary>
    /// Quality levels for food
    /// </summary>
    public enum FoodQuality
    {
        Poor,
        Normal,
        Good,
        Excellent
    }
    
    /// <summary>
    /// Food-specific information for NPCs
    /// </summary>
    [System.Serializable]
    public struct FoodInfo
    {
        public ConsumableInfo baseInfo;
        public FoodType foodType;
        public float hungerSatisfaction;
        public float energyBoost;
        public FoodQuality quality;
        public bool isRotten;
        public bool isFresh;
    }
}
