using UnityEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Emits Safety POI signals for testing AI seeking behavior
    /// </summary>
    public class SafetyEmitter : BasePOIEmitter
    {
        [Header("Safety Settings")]
        [SerializeField] private float safetyLevel = 0.9f; // How safe this area is (0-1)
        [SerializeField] private bool reducesStress = true;
        [SerializeField] private float stressReductionPerSecond = 5f;
        [SerializeField] private bool providesHealing = true;
        [SerializeField] private float healingPerSecond = 2f;
        [SerializeField] private bool providesProtection = true;
        [SerializeField] private float protectionMultiplier = 0.5f; // Reduces incoming damage
        
        [Header("Visual Effects")]
        [SerializeField] private Color safetyColor = Color.white;
        [SerializeField] private float glowIntensity = 1.5f;
        
        // Properties
        public float SafetyLevel => safetyLevel;
        public bool ReducesStress => reducesStress;
        public float StressReductionPerSecond => stressReductionPerSecond;
        public bool ProvidesHealing => providesHealing;
        public float HealingPerSecond => healingPerSecond;
        public bool ProvidesProtection => providesProtection;
        public float ProtectionMultiplier => protectionMultiplier;
        
        // Internal state
        private Renderer safetyRenderer;
        private Light safetyLight;
        
        protected override void InitializePOI()
        {
            // Set POI type
            poiType = POIType.Safety;
            sourceName = "Safe Zone";
            
            // Set default values
            intensity = POIIntensity.High;
            radius = 12f;
            strength = safetyLevel;
            
            // Get or create visual components
            SetupVisualEffects();
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Safety emitter initialized with level {safetyLevel}");
            }
        }
        
        protected override void UpdatePOIBehavior()
        {
            // Apply safety effects to nearby entities
            ApplySafetyEffects();
        }
        
        /// <summary>
        /// Setup visual effects for the safety zone
        /// </summary>
        private void SetupVisualEffects()
        {
            // Get or add renderer
            safetyRenderer = GetComponent<Renderer>();
            if (safetyRenderer == null)
            {
                // Create a simple cylinder to represent the safety zone
                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.transform.SetParent(transform);
                cylinder.transform.localPosition = Vector3.zero;
                cylinder.transform.localScale = new Vector3(2f, 0.1f, 2f); // Flat cylinder
                safetyRenderer = cylinder.GetComponent<Renderer>();
                
                // Remove collider to avoid interference
                Collider cylinderCollider = cylinder.GetComponent<Collider>();
                if (cylinderCollider != null)
                {
                    Destroy(cylinderCollider);
                }
            }
            
            // Set up material
            if (safetyRenderer != null)
            {
                Material safetyMat = new Material(Shader.Find("Standard"));
                safetyMat.color = safetyColor;
                safetyMat.SetFloat("_Mode", 3); // Transparent
                safetyMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                safetyMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                safetyMat.SetInt("_ZWrite", 0);
                safetyMat.DisableKeyword("_ALPHATEST_ON");
                safetyMat.EnableKeyword("_ALPHABLEND_ON");
                safetyMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                safetyMat.renderQueue = 3000;
                safetyRenderer.material = safetyMat;
            }
            
            // Add safety light
            safetyLight = GetComponent<Light>();
            if (safetyLight == null)
            {
                safetyLight = gameObject.AddComponent<Light>();
            }
            
            safetyLight.type = LightType.Point;
            safetyLight.color = safetyColor;
            safetyLight.intensity = glowIntensity;
            safetyLight.range = radius;
        }
        
        /// <summary>
        /// Apply safety effects to nearby entities
        /// </summary>
        private void ApplySafetyEffects()
        {
            if (!isActive) return;
            
            // Find all NPCs within safety radius
            var nearbyNPCs = FindObjectsByType<TerrariumEngine.AI.BaseNPC>(FindObjectsSortMode.None);
            
            foreach (var npc in nearbyNPCs)
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                
                if (distance <= radius)
                {
                    // Calculate effect strength based on distance
                    float effectStrength = 1f - (distance / radius);
                    
                    // Reduce stress
                    if (reducesStress)
                    {
                        float stressReduction = stressReductionPerSecond * effectStrength * Time.deltaTime;
                        npc.ModifyStat(TerrariumEngine.AI.StatType.Stress, -stressReduction);
                    }
                    
                    // Provide healing
                    if (providesHealing)
                    {
                        float healing = healingPerSecond * effectStrength * Time.deltaTime;
                        npc.Heal(healing);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the safety level
        /// </summary>
        /// <param name="level">Safety level (0-1)</param>
        public void SetSafetyLevel(float level)
        {
            safetyLevel = Mathf.Clamp01(level);
            strength = safetyLevel;
            
            // Update intensity based on safety level
            if (safetyLevel >= 0.8f)
                intensity = POIIntensity.VeryHigh;
            else if (safetyLevel >= 0.6f)
                intensity = POIIntensity.High;
            else if (safetyLevel >= 0.4f)
                intensity = POIIntensity.Medium;
            else if (safetyLevel >= 0.2f)
                intensity = POIIntensity.Low;
            else
                intensity = POIIntensity.VeryLow;
        }
        
        /// <summary>
        /// Toggle stress reduction
        /// </summary>
        /// <param name="enabled">Whether stress reduction should be enabled</param>
        public void SetStressReductionEnabled(bool enabled)
        {
            reducesStress = enabled;
        }
        
        /// <summary>
        /// Toggle healing
        /// </summary>
        /// <param name="enabled">Whether healing should be enabled</param>
        public void SetHealingEnabled(bool enabled)
        {
            providesHealing = enabled;
        }
        
        /// <summary>
        /// Toggle protection
        /// </summary>
        /// <param name="enabled">Whether protection should be enabled</param>
        public void SetProtectionEnabled(bool enabled)
        {
            providesProtection = enabled;
        }
        
        protected override Color GetPOIColor()
        {
            return safetyColor;
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw additional safety info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
            if (screenPos.z > 0)
            {
                float yOffset = 80f; // Offset below main POI info
                float labelHeight = 16f;
                
                GUI.color = Color.green;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Safety Level: {safetyLevel:P0}");
                yOffset += labelHeight;
                
                if (reducesStress)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Stress -{stressReductionPerSecond:F1}/s");
                    yOffset += labelHeight;
                }
                
                if (providesHealing)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Healing: +{healingPerSecond:F1}/s");
                    yOffset += labelHeight;
                }
                
                if (providesProtection)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Protection: {protectionMultiplier:P0}");
                }
                
                GUI.color = Color.white;
            }
        }
    }
}
