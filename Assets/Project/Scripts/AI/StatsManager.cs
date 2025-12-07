using UnityEngine;
using System.Collections.Generic;
using TerrariumEngine;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Manager for handling external stat modifications and environmental effects
    /// </summary>
    public class StatsManager : MonoBehaviour, IDebuggable
    {
        [Header("Environmental Settings")]
        [SerializeField] private float globalTemperature = 75f;
        [SerializeField] private float globalEnvironment = 80f;
        [SerializeField] private float temperatureVariation = 5f;
        [SerializeField] private float environmentVariation = 10f;
        
        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        public string DebugName => "StatsManager";
        public bool IsDebugEnabled { get; set; } = true;
        
        private float lastUpdateTime = 0f;
        private List<BaseNPC> allNPCs = new List<BaseNPC>();
        
        // Events
        public System.Action<float> OnGlobalTemperatureChanged;
        public System.Action<float> OnGlobalEnvironmentChanged;
        
        private void Start()
        {
            // Register with debug manager
            DebugManager.Instance.RegisterDebuggable(this);
            
            // Find all NPCs
            RefreshNPCList();
        }
        
        private void Update()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateEnvironmentalStats();
                lastUpdateTime = Time.time;
            }
        }
        
        private void OnDestroy()
        {
            DebugManager.Instance.UnregisterDebuggable(this);
        }
        
        /// <summary>
        /// Update environmental stats for all NPCs
        /// </summary>
        private void UpdateEnvironmentalStats()
        {
            // Apply slight random variations to global values
            globalTemperature += Random.Range(-temperatureVariation, temperatureVariation) * 0.1f;
            globalEnvironment += Random.Range(-environmentVariation, environmentVariation) * 0.1f;
            
            // Clamp values
            globalTemperature = Mathf.Clamp(globalTemperature, 0f, 100f);
            globalEnvironment = Mathf.Clamp(globalEnvironment, 0f, 100f);
            
            // Apply to all NPCs
            foreach (var npc in allNPCs)
            {
                if (npc != null && npc.IsAlive)
                {
                    // Apply temperature effects
                    ApplyTemperatureEffects(npc);
                    
                    // Apply environment effects
                    ApplyEnvironmentEffects(npc);
                    
                    // Apply stress effects based on other stats
                    ApplyStressEffects(npc);
                }
            }
            
            // Fire events
            OnGlobalTemperatureChanged?.Invoke(globalTemperature);
            OnGlobalEnvironmentChanged?.Invoke(globalEnvironment);
        }
        
        /// <summary>
        /// Apply temperature effects to an NPC
        /// </summary>
        /// <param name="npc">NPC to affect</param>
        private void ApplyTemperatureEffects(BaseNPC npc)
        {
            var tempStat = npc.GetStat(StatType.Temperature);
            if (tempStat != null)
            {
                // Gradually move temperature towards global temperature
                float currentTemp = tempStat.CurrentValue;
                float targetTemp = globalTemperature;
                float change = (targetTemp - currentTemp) * 0.1f; // 10% towards target
                
                npc.ModifyStat(StatType.Temperature, change);
            }
        }
        
        /// <summary>
        /// Apply environment effects to an NPC
        /// </summary>
        /// <param name="npc">NPC to affect</param>
        private void ApplyEnvironmentEffects(BaseNPC npc)
        {
            var envStat = npc.GetStat(StatType.Environment);
            if (envStat != null)
            {
                // Gradually move environment towards global environment
                float currentEnv = envStat.CurrentValue;
                float targetEnv = globalEnvironment;
                float change = (targetEnv - currentEnv) * 0.1f; // 10% towards target
                
                npc.ModifyStat(StatType.Environment, change);
            }
        }
        
        /// <summary>
        /// Apply stress effects based on other stats
        /// </summary>
        /// <param name="npc">NPC to affect</param>
        private void ApplyStressEffects(BaseNPC npc)
        {
            var stressStat = npc.GetStat(StatType.Stress);
            if (stressStat == null) return;
            
            float stressChange = 0f;
            
            // Check other stats and apply stress accordingly
            var foodStat = npc.GetStat(StatType.Food);
            if (foodStat != null && foodStat.State == StatState.Bad)
            {
                stressChange += 0.5f; // Increase stress if food is bad
            }
            
            var waterStat = npc.GetStat(StatType.Water);
            if (waterStat != null && waterStat.State == StatState.Bad)
            {
                stressChange += 0.5f; // Increase stress if water is bad
            }
            
            var spaceStat = npc.GetStat(StatType.Space);
            if (spaceStat != null && spaceStat.State == StatState.Bad)
            {
                stressChange += 0.3f; // Increase stress if space is bad
            }
            
            // If all stats are good, reduce stress slightly
            if (foodStat?.State == StatState.Good && 
                waterStat?.State == StatState.Good && 
                spaceStat?.State == StatState.Good)
            {
                stressChange -= 0.1f; // Reduce stress if everything is good
            }
            
            if (stressChange != 0f)
            {
                npc.ModifyStat(StatType.Stress, stressChange);
            }
        }
        
        /// <summary>
        /// Refresh the list of all NPCs
        /// </summary>
        public void RefreshNPCList()
        {
            allNPCs.Clear();
            allNPCs.AddRange(FindObjectsByType<BaseNPC>(FindObjectsSortMode.None));
        }
        
        /// <summary>
        /// Set global temperature
        /// </summary>
        /// <param name="temperature">New temperature value</param>
        public void SetGlobalTemperature(float temperature)
        {
            globalTemperature = Mathf.Clamp(temperature, 0f, 100f);
        }
        
        /// <summary>
        /// Set global environment
        /// </summary>
        /// <param name="environment">New environment value</param>
        public void SetGlobalEnvironment(float environment)
        {
            globalEnvironment = Mathf.Clamp(environment, 0f, 100f);
        }
        
        /// <summary>
        /// Get current global temperature
        /// </summary>
        /// <returns>Current global temperature</returns>
        public float GetGlobalTemperature()
        {
            return globalTemperature;
        }
        
        /// <summary>
        /// Get current global environment
        /// </summary>
        /// <returns>Current global environment</returns>
        public float GetGlobalEnvironment()
        {
            return globalEnvironment;
        }
        
        public void OnDebugStateChanged(bool enabled)
        {
            showDebugInfo = enabled;
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo || !IsDebugEnabled) return;
            
            // Draw stats manager info
            GUI.Box(new Rect(10, 10, 250, 120), "Stats Manager");
            
            GUI.Label(new Rect(20, 35, 230, 20), $"Global Temperature: {globalTemperature:F1}");
            GUI.Label(new Rect(20, 55, 230, 20), $"Global Environment: {globalEnvironment:F1}");
            GUI.Label(new Rect(20, 75, 230, 20), $"Active NPCs: {allNPCs.Count}");
            GUI.Label(new Rect(20, 95, 230, 20), $"Update Interval: {updateInterval:F1}s");
            
            // Buttons for testing
            if (GUI.Button(new Rect(20, 115, 100, 20), "Refresh NPCs"))
            {
                RefreshNPCList();
            }
            
            if (GUI.Button(new Rect(130, 115, 100, 20), "Test Death"))
            {
                TestDeathCondition();
            }
        }
        
        /// <summary>
        /// Test death condition by making multiple stats bad
        /// </summary>
        private void TestDeathCondition()
        {
            if (allNPCs.Count > 0)
            {
                var npc = allNPCs[0];
                if (npc != null)
                {
                    // Make food and water bad to trigger death
                    npc.SetStat(StatType.Food, 20f); // Below baseline
                    npc.SetStat(StatType.Water, 30f); // Below baseline
                    
                    if (IsDebugEnabled)
                    {
                        Debug.Log($"Testing death condition on {npc.DebugName}");
                    }
                }
            }
        }
    }
}

