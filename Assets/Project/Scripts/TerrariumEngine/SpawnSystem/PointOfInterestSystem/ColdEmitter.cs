using UnityEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Emits Cool POI signals for testing AI temperature-seeking behavior
    /// </summary>
    public class ColdEmitter : BasePOIEmitter
    {
        [Header("Cold Settings")]
        [SerializeField] private float coldLevel = 0.8f; // How cold this area is (0-1)
        [SerializeField] private float temperatureReduction = 20f; // Degrees to reduce temperature
        [SerializeField] private bool affectsEnvironment = true;
        [SerializeField] private float environmentBonus = 10f; // Bonus to environment stat
        
        [Header("Visual Effects")]
        [SerializeField] private Color coldColor = Color.cyan;
        
        // Properties
        public float ColdLevel => coldLevel;
        public float TemperatureReduction => temperatureReduction;
        public bool AffectsEnvironment => affectsEnvironment;
        public float EnvironmentBonus => environmentBonus;
        
        // Internal state
        private Renderer coldRenderer;
        private Light coldLight;
        private ParticleSystem frostParticles;
        
        protected override void InitializePOI()
        {
            // Set POI type
            poiType = POIType.Cool;
            sourceName = "Cold Zone";
            
            // Set default values
            intensity = POIIntensity.Medium;
            radius = 10f;
            strength = coldLevel;
            
            // Get or create visual components
            SetupVisualEffects();
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Cold emitter initialized with level {coldLevel}");
            }
        }
        
        protected override void UpdatePOIBehavior()
        {
            // Apply cold effects to nearby entities
            ApplyColdEffects();
        }
        
        /// <summary>
        /// Setup visual effects for the cold zone
        /// </summary>
        private void SetupVisualEffects()
        {
            // Get or add renderer
            coldRenderer = GetComponent<Renderer>();
            if (coldRenderer == null)
            {
                // Create a simple icosahedron to represent the cold zone
                GameObject coldObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                coldObject.transform.SetParent(transform);
                coldObject.transform.localPosition = Vector3.zero;
                coldObject.transform.localScale = Vector3.one * 0.8f;
                coldRenderer = coldObject.GetComponent<Renderer>();
                
                // Remove collider to avoid interference
                Collider coldCollider = coldObject.GetComponent<Collider>();
                if (coldCollider != null)
                {
                    Destroy(coldCollider);
                }
            }
            
            // Set up material
            if (coldRenderer != null)
            {
                Material coldMat = new Material(Shader.Find("Standard"));
                coldMat.color = coldColor;
                coldMat.SetFloat("_Mode", 3); // Transparent
                coldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                coldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                coldMat.SetInt("_ZWrite", 0);
                coldMat.DisableKeyword("_ALPHATEST_ON");
                coldMat.EnableKeyword("_ALPHABLEND_ON");
                coldMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                coldMat.renderQueue = 3000;
                coldRenderer.material = coldMat;
            }
            
            // Add cold light
            coldLight = GetComponent<Light>();
            if (coldLight == null)
            {
                coldLight = gameObject.AddComponent<Light>();
            }
            
            coldLight.type = LightType.Point;
            coldLight.color = coldColor;
            coldLight.intensity = 1.5f;
            coldLight.range = radius;
            
            // Add frost particle effect
            SetupFrostParticles();
        }
        
        /// <summary>
        /// Setup frost particle effects
        /// </summary>
        private void SetupFrostParticles()
        {
            frostParticles = GetComponent<ParticleSystem>();
            if (frostParticles == null)
            {
                frostParticles = gameObject.AddComponent<ParticleSystem>();
            }
            
            var main = frostParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.1f;
            main.startColor = coldColor;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = frostParticles.emission;
            emission.rateOverTime = 20f;
            
            var shape = frostParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.5f;
            
            var velocityOverLifetime = frostParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.5f);
            
            var sizeOverLifetime = frostParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.3f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        }
        
        /// <summary>
        /// Apply cold effects to nearby entities
        /// </summary>
        private void ApplyColdEffects()
        {
            if (!isActive) return;
            
            // Find all NPCs within cold radius
            var nearbyNPCs = FindObjectsByType<TerrariumEngine.AI.BaseNPC>(FindObjectsSortMode.None);
            
            foreach (var npc in nearbyNPCs)
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                
                if (distance <= radius)
                {
                    // Calculate effect strength based on distance
                    float effectStrength = 1f - (distance / radius);
                    
                    // Reduce temperature (if we add temperature stat later)
                    // npc.ModifyStat(StatType.Temperature, -temperatureReduction * effectStrength * Time.deltaTime);
                    
                    // Improve environment stat
                    if (affectsEnvironment)
                    {
                        float envBonus = environmentBonus * effectStrength * Time.deltaTime;
                        npc.ModifyStat(TerrariumEngine.AI.StatType.Environment, envBonus);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the cold level
        /// </summary>
        /// <param name="level">Cold level (0-1)</param>
        public void SetColdLevel(float level)
        {
            coldLevel = Mathf.Clamp01(level);
            strength = coldLevel;
            
            // Update intensity based on cold level
            if (coldLevel >= 0.8f)
                intensity = POIIntensity.VeryHigh;
            else if (coldLevel >= 0.6f)
                intensity = POIIntensity.High;
            else if (coldLevel >= 0.4f)
                intensity = POIIntensity.Medium;
            else if (coldLevel >= 0.2f)
                intensity = POIIntensity.Low;
            else
                intensity = POIIntensity.VeryLow;
        }
        
        /// <summary>
        /// Set temperature reduction amount
        /// </summary>
        /// <param name="reduction">Temperature reduction per second</param>
        public void SetTemperatureReduction(float reduction)
        {
            temperatureReduction = Mathf.Max(0f, reduction);
        }
        
        /// <summary>
        /// Toggle environment effects
        /// </summary>
        /// <param name="enabled">Whether environment effects should be enabled</param>
        public void SetEnvironmentEffectsEnabled(bool enabled)
        {
            affectsEnvironment = enabled;
        }
        
        protected override Color GetPOIColor()
        {
            return coldColor;
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw additional cold info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
            if (screenPos.z > 0)
            {
                float yOffset = 80f; // Offset below main POI info
                float labelHeight = 16f;
                
                GUI.color = Color.cyan;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Cold Level: {coldLevel:P0}");
                yOffset += labelHeight;
                
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Temp Reduction: -{temperatureReduction:F1}°");
                yOffset += labelHeight;
                
                if (affectsEnvironment)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Environment: +{environmentBonus:F1}/s");
                }
                
                GUI.color = Color.white;
            }
        }
    }
}
