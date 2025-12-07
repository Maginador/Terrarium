using UnityEngine;
using TerrariumEngine.AI;
using TerrariumEngine.SpawnSystem.PointOfInterestSystem;

namespace TerrariumEngine.SpawnSystem
{
    /// <summary>
    /// Water item that can be consumed by NPCs to satisfy thirst
    /// Also emits Water POI signals for AI detection
    /// </summary>
    public class WaterItem : BaseConsumable, IPointOfInterest
    {
        [Header("Water Specific Settings")]
        [SerializeField] private WaterType waterType = WaterType.Fresh;
        [SerializeField] private float thirstSatisfaction = 25f; // How much thirst this satisfies
        [SerializeField] private float hydrationBoost = 10f; // Hydration boost when consumed
        [SerializeField] private float evaporationRate = 0.05f; // How fast water evaporates (per second)
        [SerializeField] private bool canEvaporate = true;
        [SerializeField] private float minEvaporationAmount = 0.1f; // Minimum amount before evaporation stops
        
        [Header("Water Quality")]
        [SerializeField] private WaterQuality quality = WaterQuality.Clean;
        [SerializeField] private float qualityMultiplier = 1f; // Multiplier for nutrition based on quality
        [SerializeField] private float contaminationLevel = 0f; // How contaminated the water is
        [SerializeField] private float maxContamination = 1f; // Maximum contamination level
        
        [Header("Water Source")]
        [SerializeField] private bool isStaticSource = false; // If true, water refills over time
        [SerializeField] private float refillRate = 0.1f; // How fast static sources refill
        [SerializeField] private float refillDelay = 5f; // Delay before refill starts after consumption
        
        [Header("POI Settings")]
        [SerializeField] private float poiRadius = 20f; // Detection radius for AI
        [SerializeField] private bool emitPOI = true; // Whether to emit POI signals
        
        // Properties
        public WaterType WaterType => waterType;
        public float ThirstSatisfaction => thirstSatisfaction * qualityMultiplier;
        public float HydrationBoost => hydrationBoost * qualityMultiplier;
        public float EvaporationRate => evaporationRate;
        public bool CanEvaporate => canEvaporate;
        public WaterQuality Quality => quality;
        public float QualityMultiplier => qualityMultiplier;
        public float ContaminationLevel => contaminationLevel;
        public float MaxContamination => maxContamination;
        public bool IsStaticSource => isStaticSource;
        public float RefillRate => refillRate;
        
        // Water-specific events
        public System.Action<WaterItem, float> OnContaminationChanged;
        public System.Action<WaterItem> OnContaminated;
        public System.Action<WaterItem> OnRefilled;
        
        private float lastEvaporationUpdate = 0f;
        private float lastRefillUpdate = 0f;
        private float timeSinceLastConsumption = 0f;
        
        public float ContaminationPercentage => contaminationLevel / maxContamination;
        public bool IsContaminated => contaminationLevel >= maxContamination;
        public bool IsClean => contaminationLevel <= 0.1f;
        public bool IsRefilling => isStaticSource && timeSinceLastConsumption >= refillDelay && currentAmount < maxAmount;
        
        // IPointOfInterest Properties
        public POIType POIType => POIType.Water;
        public POIIntensity Intensity => GetPOIIntensity();
        public Vector3 Position => transform.position;
        public float Radius => poiRadius;
        public float Strength => GetPOIStrength();
        public bool IsActive => emitPOI && !IsEmpty;
        public string SourceName => $"{waterType} Water";
        
        // POI Events
        public System.Action<IPointOfInterest> OnPOIActivated { get; set; }
        public System.Action<IPointOfInterest> OnPOIDeactivated { get; set; }
        public System.Action<IPointOfInterest, POIIntensity> OnIntensityChanged { get; set; }
        
        protected override void InitializeConsumable()
        {
            // Set base nutrition value based on thirst satisfaction
            nutritionValue = thirstSatisfaction;
            
            // Apply quality multiplier
            ApplyQualityMultiplier();
            
            // Set water-specific rot settings (contamination)
            rotRate = 0.05f; // Water rots slower than food
            maxRot = 1f;
            canRot = true;
            destroyWhenRotten = false; // Water doesn't destroy when contaminated, just becomes less effective
            
            // Set water-specific lifetime
            maxLifetime = 600f; // 10 minutes default
            
            // Initialize timing
            lastEvaporationUpdate = Time.time;
            lastRefillUpdate = Time.time;
            timeSinceLastConsumption = 0f;
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Initialized as {waterType} water with {quality} quality");
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            // Update evaporation
            if (canEvaporate && !IsEmpty)
            {
                UpdateEvaporation();
            }
            
            // Update refill for static sources
            if (isStaticSource && IsRefilling)
            {
                UpdateRefill();
            }
            
            // Update time since last consumption
            timeSinceLastConsumption += Time.deltaTime;
        }
        
        /// <summary>
        /// Update water evaporation over time
        /// </summary>
        private void UpdateEvaporation()
        {
            float deltaTime = Time.time - lastEvaporationUpdate;
            lastEvaporationUpdate = Time.time;
            
            // Only evaporate if above minimum amount
            if (currentAmount > minEvaporationAmount)
            {
                float evaporationAmount = evaporationRate * deltaTime;
                SetAmount(currentAmount - evaporationAmount);
            }
        }
        
        /// <summary>
        /// Update refill for static water sources
        /// </summary>
        private void UpdateRefill()
        {
            float deltaTime = Time.time - lastRefillUpdate;
            lastRefillUpdate = Time.time;
            
            float refillAmount = refillRate * deltaTime;
            SetAmount(currentAmount + refillAmount);
            
            if (IsFull)
            {
                OnRefilled?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Set the contamination level
        /// </summary>
        /// <param name="newContamination">New contamination level</param>
        public void SetContamination(float newContamination)
        {
            float oldContamination = contaminationLevel;
            contaminationLevel = Mathf.Clamp(newContamination, 0f, maxContamination);
            
            if (contaminationLevel != oldContamination)
            {
                OnContaminationChanged?.Invoke(this, contaminationLevel - oldContamination);
                UpdateVisuals();
                
                if (IsContaminated)
                {
                    OnContaminated?.Invoke(this);
                    HandleContamination();
                }
            }
        }
        
        /// <summary>
        /// Add contamination to the water
        /// </summary>
        /// <param name="amount">Amount of contamination to add</param>
        public void AddContamination(float amount)
        {
            SetContamination(contaminationLevel + amount);
        }
        
        protected override void HandleRot()
        {
            // Water uses rot as contamination
            // Reduce nutrition value when contaminated
            nutritionValue = thirstSatisfaction * 0.3f; // Only 30% nutrition when contaminated
            
            // Change color to indicate contamination
            if (itemRenderer != null)
            {
                itemRenderer.material.color = Color.brown;
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Water has become contaminated!");
            }
        }
        
        /// <summary>
        /// Handle contamination effects
        /// </summary>
        private void HandleContamination()
        {
            // Water-specific contamination handling
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Water contamination effects applied");
            }
        }
        
        /// <summary>
        /// Apply quality multiplier to nutrition values
        /// </summary>
        private void ApplyQualityMultiplier()
        {
            switch (quality)
            {
                case WaterQuality.Dirty:
                    qualityMultiplier = 0.3f;
                    break;
                case WaterQuality.Clean:
                    qualityMultiplier = 1f;
                    break;
                case WaterQuality.Pure:
                    qualityMultiplier = 1.5f;
                    break;
                case WaterQuality.Spring:
                    qualityMultiplier = 2f;
                    break;
            }
            
            // Update nutrition values
            nutritionValue = thirstSatisfaction * qualityMultiplier;
            healthBonus = healthBonus * qualityMultiplier;
            stressReduction = stressReduction * qualityMultiplier;
        }
        
        public override float Consume(BaseNPC consumer, float amount = -1f)
        {
            float consumedAmount = base.Consume(consumer, amount);
            
            if (consumedAmount > 0f)
            {
                // Reset refill timer for static sources
                timeSinceLastConsumption = 0f;
                lastRefillUpdate = Time.time;
            }
            
            return consumedAmount;
        }
        
        protected override void ApplyConsumptionEffects(BaseNPC consumer, float amount)
        {
            base.ApplyConsumptionEffects(consumer, amount);
            
            // Apply thirst satisfaction
            if (thirstSatisfaction > 0f)
            {
                float actualSatisfaction = ThirstSatisfaction * amount;
                consumer.ModifyStat(StatType.Water, actualSatisfaction);
            }
            
            // Apply hydration boost (if we add hydration stat later)
            // consumer.ModifyStat(StatType.Hydration, hydrationBoost * amount);
            
            // Apply contamination effects if water is contaminated (using rot as contamination)
            if (IsRotten)
            {
                float contaminationEffect = currentRot * amount;
                consumer.ModifyStat(StatType.Health, -contaminationEffect * 2f); // Contaminated water damages health
                consumer.ModifyStat(StatType.Stress, contaminationEffect); // Increases stress
            }
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Satisfied {ThirstSatisfaction * amount} thirst for {consumer.DebugName}");
                if (IsRotten)
                {
                    Debug.Log($"{DebugName}: Contaminated water caused {currentRot * amount} health damage");
                }
            }
        }
        
        public override bool CanBeConsumedBy(BaseNPC consumer)
        {
            if (!base.CanBeConsumedBy(consumer))
                return false;
            
            // Can consume contaminated water, but it has negative effects
            return true;
        }
        
        public override float GetConsumptionPriority(BaseNPC consumer)
        {
            if (!CanBeConsumedBy(consumer))
                return 0f;
            
            // Get thirst stat from consumer
            var thirstStat = consumer.GetStat(StatType.Water);
            if (thirstStat == null)
                return base.GetConsumptionPriority(consumer);
            
            // Higher priority if more thirsty
            float thirstNeed = 1f - thirstStat.GetPercentage();
            
            // Factor in water quality and contamination (using rot as contamination)
            float qualityFactor = qualityMultiplier;
            float contaminationFactor = 1f - (RotPercentage * 0.5f); // Reduce priority for contaminated water
            
            return nutritionValue * thirstNeed * qualityFactor * contaminationFactor;
        }
        
        protected override void UpdateVisuals()
        {
            base.UpdateVisuals();
            
            if (itemRenderer == null) return;
            
            // Update color based on contamination (using rot as contamination)
            if (currentRot > 0f)
            {
                Color contaminationColor = Color.Lerp(fullColor, Color.brown, RotPercentage);
                itemRenderer.material.color = contaminationColor;
            }
            
            // Add transparency based on amount for water
            if (itemRenderer.material.HasProperty("_Color"))
            {
                Color currentColor = itemRenderer.material.color;
                currentColor.a = Mathf.Lerp(0.3f, 1f, AmountPercentage);
                itemRenderer.material.color = currentColor;
            }
        }
        
        /// <summary>
        /// Get water information for NPCs
        /// </summary>
        /// <returns>Water-specific information</returns>
        public WaterInfo GetWaterInfo()
        {
            return new WaterInfo
            {
                baseInfo = GetConsumableInfo(),
                waterType = waterType,
                thirstSatisfaction = ThirstSatisfaction,
                hydrationBoost = hydrationBoost,
                quality = quality,
                isContaminated = IsRotten, // Use rot as contamination
                isClean = IsFresh,
                isStaticSource = isStaticSource,
                isRefilling = IsRefilling
            };
        }
        
        protected override void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw water info above the item
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0)
            {
                float yOffset = 0f;
                float labelHeight = 16f;
                
                // Water name and type
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"{itemName} ({waterType})");
                yOffset += labelHeight;
                
                // Amount
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Amount: {currentAmount:F1}/{maxAmount:F1}");
                yOffset += labelHeight;
                
                // Quality and contamination
                string qualityColor = quality == WaterQuality.Spring ? "cyan" : 
                                    quality == WaterQuality.Pure ? "blue" : 
                                    quality == WaterQuality.Dirty ? "red" : "white";
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Quality: {quality} (x{qualityMultiplier:F1})");
                yOffset += labelHeight;
                
                if (currentRot > 0f)
                {
                    GUI.color = IsRotten ? Color.red : Color.yellow;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Contamination: {RotPercentage:P0}");
                    GUI.color = Color.white;
                }
                
                // Static source info
                if (isStaticSource)
                {
                    GUI.color = IsRefilling ? Color.green : Color.gray;
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset + labelHeight, 160, labelHeight), 
                        IsRefilling ? "Refilling..." : "Static Source");
                    GUI.color = Color.white;
                }
                
                // Thirst satisfaction
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset + (isStaticSource ? labelHeight * 2 : labelHeight), 160, labelHeight), 
                    $"Thirst: {ThirstSatisfaction:F1}");
            }
        }
        
        #region IPointOfInterest Implementation
        
        /// <summary>
        /// Get POI intensity based on water quality and contamination
        /// </summary>
        /// <returns>POI intensity</returns>
        private POIIntensity GetPOIIntensity()
        {
            if (!IsActive) return POIIntensity.VeryLow;
            
            // Base intensity on quality
            float qualityFactor = (int)quality / 4f; // 0-1 based on quality
            float contaminationFactor = 1f - ContaminationPercentage; // 1 when clean, 0 when contaminated
            
            float combinedFactor = qualityFactor * contaminationFactor;
            
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
        /// Get POI strength based on amount, quality, and contamination
        /// </summary>
        /// <returns>POI strength (0-1)</returns>
        private float GetPOIStrength()
        {
            if (!IsActive) return 0f;
            
            float amountFactor = AmountPercentage;
            float qualityFactor = (int)quality / 4f;
            float contaminationFactor = 1f - ContaminationPercentage;
            
            return amountFactor * qualityFactor * contaminationFactor;
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
                lifetime = -1f, // Water items are permanent until consumed/evaporated
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
    /// Types of water available
    /// </summary>
    public enum WaterType
    {
        Fresh,
        Salt,
        Spring,
        Rain,
        Puddle,
        Stream
    }
    
    /// <summary>
    /// Quality levels for water
    /// </summary>
    public enum WaterQuality
    {
        Dirty,
        Clean,
        Pure,
        Spring
    }
    
    /// <summary>
    /// Water-specific information for NPCs
    /// </summary>
    [System.Serializable]
    public struct WaterInfo
    {
        public ConsumableInfo baseInfo;
        public WaterType waterType;
        public float thirstSatisfaction;
        public float hydrationBoost;
        public WaterQuality quality;
        public bool isContaminated;
        public bool isClean;
        public bool isStaticSource;
        public bool isRefilling;
    }
}
