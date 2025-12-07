using UnityEngine;

namespace TerrariumEngine.SpawnSystem.PointOfInterestSystem
{
    /// <summary>
    /// Emits Danger POI signals for testing AI avoidance behavior
    /// </summary>
    public class DangerEmitter : BasePOIEmitter
    {
        [Header("Danger Settings")]
        [SerializeField] private float dangerLevel = 0.8f; // How dangerous this area is (0-1)
        [SerializeField] private bool causesDamage = true;
        [SerializeField] private float damagePerSecond = 5f;
        [SerializeField] private bool causesStress = true;
        [SerializeField] private float stressPerSecond = 10f;
        
        [Header("Visual Effects")]
        [SerializeField] private bool showWarningEffect = true;
        [SerializeField] private Color warningColor = Color.red;
        [SerializeField] private float pulseSpeed = 2f;
        
        // Properties
        public float DangerLevel => dangerLevel;
        public bool CausesDamage => causesDamage;
        public float DamagePerSecond => damagePerSecond;
        public bool CausesStress => causesStress;
        public float StressPerSecond => stressPerSecond;
        
        // Internal state
        private float pulseTime = 0f;
        private Renderer dangerRenderer;
        private Light dangerLight;
        
        protected override void InitializePOI()
        {
            // Set POI type
            poiType = POIType.Danger;
            sourceName = "Danger Zone";
            
            // Set default values
            intensity = POIIntensity.High;
            radius = 8f;
            strength = dangerLevel;
            
            // Get or create visual components
            SetupVisualEffects();
            
            if (IsDebugEnabled)
            {
                Debug.Log($"{DebugName}: Danger emitter initialized with level {dangerLevel}");
            }
        }
        
        protected override void UpdatePOIBehavior()
        {
            // Update visual effects
            if (showWarningEffect)
            {
                UpdateWarningEffects();
            }
            
            // Apply damage/stress to nearby entities
            ApplyDangerEffects();
        }
        
        /// <summary>
        /// Setup visual effects for the danger zone
        /// </summary>
        private void SetupVisualEffects()
        {
            // Get or add renderer
            dangerRenderer = GetComponent<Renderer>();
            if (dangerRenderer == null)
            {
                // Create a simple sphere to represent the danger zone
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(transform);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale = Vector3.one * 0.5f;
                dangerRenderer = sphere.GetComponent<Renderer>();
                
                // Remove collider to avoid interference
                Collider sphereCollider = sphere.GetComponent<Collider>();
                if (sphereCollider != null)
                {
                    Destroy(sphereCollider);
                }
            }
            
            // Set up material
            if (dangerRenderer != null)
            {
                Material dangerMat = new Material(Shader.Find("Standard"));
                dangerMat.color = warningColor;
                dangerMat.SetFloat("_Mode", 3); // Transparent
                dangerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                dangerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                dangerMat.SetInt("_ZWrite", 0);
                dangerMat.DisableKeyword("_ALPHATEST_ON");
                dangerMat.EnableKeyword("_ALPHABLEND_ON");
                dangerMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                dangerMat.renderQueue = 3000;
                dangerRenderer.material = dangerMat;
            }
            
            // Add warning light
            dangerLight = GetComponent<Light>();
            if (dangerLight == null)
            {
                dangerLight = gameObject.AddComponent<Light>();
            }
            
            dangerLight.type = LightType.Point;
            dangerLight.color = warningColor;
            dangerLight.intensity = 2f;
            dangerLight.range = radius;
        }
        
        /// <summary>
        /// Update warning visual effects
        /// </summary>
        private void UpdateWarningEffects()
        {
            pulseTime += Time.deltaTime * pulseSpeed;
            
            // Pulsing effect
            float pulse = (Mathf.Sin(pulseTime) + 1f) * 0.5f; // 0 to 1
            float alpha = Mathf.Lerp(0.3f, 0.8f, pulse);
            
            if (dangerRenderer != null && dangerRenderer.material != null)
            {
                Color currentColor = dangerRenderer.material.color;
                currentColor.a = alpha;
                dangerRenderer.material.color = currentColor;
            }
            
            // Light intensity
            if (dangerLight != null)
            {
                dangerLight.intensity = Mathf.Lerp(1f, 3f, pulse);
            }
        }
        
        /// <summary>
        /// Apply danger effects to nearby entities
        /// </summary>
        private void ApplyDangerEffects()
        {
            if (!isActive) return;
            
            // Find all NPCs within danger radius
            var nearbyNPCs = FindObjectsByType<TerrariumEngine.AI.BaseNPC>(FindObjectsSortMode.None);
            
            foreach (var npc in nearbyNPCs)
            {
                float distance = Vector3.Distance(transform.position, npc.transform.position);
                
                if (distance <= radius)
                {
                    // Calculate effect strength based on distance
                    float effectStrength = 1f - (distance / radius);
                    
                    // Apply damage
                    if (causesDamage)
                    {
                        float damage = damagePerSecond * effectStrength * Time.deltaTime;
                        npc.TakeDamage(damage);
                    }
                    
                    // Apply stress
                    if (causesStress)
                    {
                        float stress = stressPerSecond * effectStrength * Time.deltaTime;
                        npc.ModifyStat(TerrariumEngine.AI.StatType.Stress, stress);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the danger level
        /// </summary>
        /// <param name="level">Danger level (0-1)</param>
        public void SetDangerLevel(float level)
        {
            dangerLevel = Mathf.Clamp01(level);
            strength = dangerLevel;
            
            // Update intensity based on danger level
            if (dangerLevel >= 0.8f)
                intensity = POIIntensity.VeryHigh;
            else if (dangerLevel >= 0.6f)
                intensity = POIIntensity.High;
            else if (dangerLevel >= 0.4f)
                intensity = POIIntensity.Medium;
            else if (dangerLevel >= 0.2f)
                intensity = POIIntensity.Low;
            else
                intensity = POIIntensity.VeryLow;
        }
        
        /// <summary>
        /// Toggle damage effects
        /// </summary>
        /// <param name="enabled">Whether damage should be enabled</param>
        public void SetDamageEnabled(bool enabled)
        {
            causesDamage = enabled;
        }
        
        /// <summary>
        /// Toggle stress effects
        /// </summary>
        /// <param name="enabled">Whether stress should be enabled</param>
        public void SetStressEnabled(bool enabled)
        {
            causesStress = enabled;
        }
        
        protected override Color GetPOIColor()
        {
            return warningColor;
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw additional danger info
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
            if (screenPos.z > 0)
            {
                float yOffset = 80f; // Offset below main POI info
                float labelHeight = 16f;
                
                GUI.color = Color.red;
                GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                    $"Danger Level: {dangerLevel:P0}");
                yOffset += labelHeight;
                
                if (causesDamage)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Damage: {damagePerSecond:F1}/s");
                    yOffset += labelHeight;
                }
                
                if (causesStress)
                {
                    GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y + yOffset, 160, labelHeight), 
                        $"Stress: {stressPerSecond:F1}/s");
                }
                
                GUI.color = Color.white;
            }
        }
    }
}

