using UnityEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Emits Warm POI signals for testing AI temperature-seeking behavior
    /// </summary>
    public class WarmEmitter : BasePOIEmitter
    {
        [Header("Warm Settings")]
        [SerializeField] private float warmLevel = 0.8f; // How warm this area is (0-1)
        [SerializeField] private float temperatureIncrease = 25f; // Degrees to increase temperature
        [SerializeField] private bool affectsEnvironment = true;
        [SerializeField] private float environmentBonus = 15f; // Bonus to environment stat
        [SerializeField] private bool providesComfort = true;
        [SerializeField] private float comfortBonus = 5f; // Bonus to stress reduction
        
        [Header("Visual Effects")]
        [SerializeField] private Color warmColor = Color.red;
        [SerializeField] private bool showHeatEffect = true;
        [SerializeField] private float heatIntensity = 1.5f;
        
        // Properties
        public float WarmLevel => warmLevel;
        public float TemperatureIncrease => temperatureIncrease;
        public bool AffectsEnvironment => affectsEnvironment;
        public float EnvironmentBonus => environmentBonus;
        public bool ProvidesComfort => providesComfort;
        public float ComfortBonus => comfortBonus;
        
        // Internal state
        private Renderer warmRenderer;
        private Light warmLight;
        private ParticleSystem heatParticles;
        private float heatPulseTime = 0f;
        
        protected override void InitializePOI()
        {
            // Set POI type
            poiType = POIType.Warm;
            sourceName = "Warm Zone";
            
            // Set default values
            intensity = POIIntensity.Medium;
            radius = 10f;
            strength = warmLevel;
            
            // Get or create visual components
            SetupVisualEffects();
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Warm emitter initialized with level {warmLevel}");
            }
        }
        
        protected override void UpdatePOIBehavior()
        {
            // Update heat visual effects
            if (showHeatEffect)
            {
                UpdateHeatEffects();
            }
            
            // Apply warm effects to nearby entities
            ApplyWarmEffects();
        }
        
        /// <summary>
        /// Setup visual effects for the warm zone
        /// </summary>
        private void SetupVisualEffects()
        {
            // Get or add renderer
            warmRenderer = GetComponent<Renderer>();
            if (warmRenderer == null)
            {
                // Create a simple sphere to represent the warm zone
                GameObject warmObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                warmObject.transform.SetParent(transform);
                warmObject.transform.localPosition = Vector3.zero;
                warmObject.transform.localScale = Vector3.one * 0.6f;
                warmRenderer = warmObject.GetComponent<Renderer>();
                
                // Remove collider to avoid interference
                Collider warmCollider = warmObject.GetComponent<Collider>();
                if (warmCollider != null)
                {
                    Destroy(warmCollider);
                }
            }
            
            // Set up material
            if (warmRenderer != null)
            {
                Material warmMat = new Material(Shader.Find("Standard"));
                warmMat.color = warmColor;
                warmMat.SetFloat("_Mode", 3); // Transparent
                warmMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                warmMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                warmMat.SetInt("_ZWrite", 0);
                warmMat.DisableKeyword("_ALPHATEST_ON");
                warmMat.EnableKeyword("_ALPHABLEND_ON");
                warmMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                warmMat.renderQueue = 3000;
                warmRenderer.material = warmMat;
            }
            
            // Add warm light
            warmLight = GetComponent<Light>();
            if (warmLight == null)
            {
                warmLight = gameObject.AddComponent<Light>();
            }
            
            warmLight.type = LightType.Point;
            warmLight.color = warmColor;
            warmLight.intensity = heatIntensity;
            warmLight.range = radius;
            
            // Add heat particle effect
            if (showHeatEffect)
            {
                SetupHeatParticles();
            }
        }
        
        /// <summary>
        /// Setup heat particle effects
        /// </summary>
        private void SetupHeatParticles()
        {
            heatParticles = GetComponent<ParticleSystem>();
            if (heatParticles == null)
            {
                heatParticles = gameObject.AddComponent<ParticleSystem>();
            }
            
            var main = heatParticles.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 2f;
            main.startSize = 0.05f;
            main.startColor = warmColor;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = heatParticles.emission;
            emission.rateOverTime = 15f;
            
            var shape = heatParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.3f;
            
            var velocityOverLifetime = heatParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(2f);
            
            var sizeOverLifetime = heatParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.2f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }
        
        /// <summary>
        /// Update heat visual effects
        /// </summary>
        private void UpdateHeatEffects()
        {
            heatPulseTime += Time.deltaTime * 2f;
            
            // Pulsing heat effect
            float pulse = (Mathf.Sin(heatPulseTime) + 1f) * 0.5f; // 0 to 1
            float alpha = Mathf.Lerp(0.4f, 0.8f, pulse);
            
            if (warmRenderer != null && warmRenderer.material != null)
            {
                Color currentColor = warmRenderer.material.color;
                currentColor.a = alpha;
                warmRenderer.material.color = currentColor;
            }
            
            // Light intensity
            if (warmLight != null)
            {
                warmLight.intensity = Mathf.Lerp(heatIntensity * 0.5f, heatIntensity, pulse);
            }
        }
        
        /// <summary>
        /// Apply warm effects to nearby entities
        /// </summary>
        private void ApplyWarmEffects()
        {
            if (!isActive) return;
            
            // Find all NPCs within warm radius
            var nearbyNPCs = FindObjectsByType<TerrariumEngine.AI.BaseNPC>(FindObjectsSortMode.None);
            
            foreach (var npc in nearbyNPCs)
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                
                if (distance <= radius)
                {
                    // Calculate effect strength based on distance
                    float effectStrength = 1f - (distance / radius);
                    
                    // Increase temperature (if we add temperature stat later)
                    // npc.ModifyStat(StatType.Temperature, temperatureIncrease * effectStrength * Time.deltaTime);
                    
                    // Improve environment stat
                    if (affectsEnvironment)
                    {
                        float envBonus = environmentBonus * effectStrength * Time.deltaTime;
                        npc.ModifyStat(TerrariumEngine.AI.StatType.Environment, envBonus);
                    }
                    
                    // Provide comfort (reduce stress)
                    if (providesComfort)
                    {
                        float comfort = comfortBonus * effectStrength * Time.deltaTime;
                        npc.ModifyStat(TerrariumEngine.AI.StatType.Stress, -comfort);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the warm level
        /// </summary>
        /// <param name="level">Warm level (0-1)</param>
        public void SetWarmLevel(float level)
        {
            warmLevel = Mathf.Clamp01(level);
            strength = warmLevel;
            
            // Update intensity based on warm level
            if (warmLevel >= 0.8f)
                intensity = POIIntensity.VeryHigh;
            else if (warmLevel >= 0.6f)
                intensity = POIIntensity.High;
            else if (warmLevel >= 0.4f)
                intensity = POIIntensity.Medium;
            else if (warmLevel >= 0.2f)
                intensity = POIIntensity.Low;
            else
                intensity = POIIntensity.VeryLow;
        }
        
        /// <summary>
        /// Set temperature increase amount
        /// </summary>
        /// <param name="increase">Temperature increase per second</param>
        public void SetTemperatureIncrease(float increase)
        {
            temperatureIncrease = Mathf.Max(0f, increase);
        }
        
        /// <summary>
        /// Toggle environment effects
        /// </summary>
        /// <param name="enabled">Whether environment effects should be enabled</param>
        public void SetEnvironmentEffectsEnabled(bool enabled)
        {
            affectsEnvironment = enabled;
        }
        
        /// <summary>
        /// Toggle comfort effects
        /// </summary>
        /// <param name="enabled">Whether comfort effects should be enabled</param>
        public void SetComfortEnabled(bool enabled)
        {
            providesComfort = enabled;
        }
        
        protected override Color GetPOIColor()
        {
            return warmColor;
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw additional warm info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
            if (screenPos.z > 0)
            {
                float yOffset = 80f; // Offset below main POI info
                float labelHeight = 16f;
                
                GUI.color = Color.red;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Warm Level: {warmLevel:P0}");
                yOffset += labelHeight;
                
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Temp Increase: +{temperatureIncrease:F1}°");
                yOffset += labelHeight;
                
                if (affectsEnvironment)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Environment: +{environmentBonus:F1}/s");
                    yOffset += labelHeight;
                }
                
                if (providesComfort)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Comfort: +{comfortBonus:F1}/s");
                }
                
                GUI.color = Color.white;
            }
        }
    }
}
