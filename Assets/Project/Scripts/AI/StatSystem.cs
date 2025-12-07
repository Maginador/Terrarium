using UnityEngine;
using System;

namespace TerrariumEngine.AI
{
    /// <summary>
    /// Types of stats that entities can have
    /// </summary>
    public enum StatType
    {
        Health,
        Food,
        Water,
        Stress,
        Environment,
        Temperature,
        Space
    }

    /// <summary>
    /// Represents the state of a stat
    /// </summary>
    public enum StatState
    {
        Good,    // Within baseline min/max range
        Bad      // Above or below baseline range
    }

    /// <summary>
    /// Definition of a stat with its baseline values and behavior
    /// </summary>
    [System.Serializable]
    public class StatDefinition
    {
        [Header("Stat Configuration")]
        public StatType statType;
        public string displayName;
        
        [Header("Baseline Values")]
        [Tooltip("Minimum value for good state")]
        public float baselineMin = 50f;
        [Tooltip("Maximum value for good state")]
        public float baselineMax = 100f;
        
        [Header("Absolute Limits")]
        [Tooltip("Absolute minimum value")]
        public float absoluteMin = 0f;
        [Tooltip("Absolute maximum value")]
        public float absoluteMax = 100f;
        
        [Header("Variation Settings")]
        [Tooltip("How much this stat changes per variation cycle (negative = decreases)")]
        public float variationAmount = -1f;
        [Tooltip("Time in seconds between variations (0 = no automatic variation)")]
        public float variationInterval = 0f;
        
        [Header("Special Behavior")]
        [Tooltip("Whether this stat is affected by external factors")]
        public bool affectedByExternal = false;
        
        /// <summary>
        /// Get the current state of a stat value
        /// </summary>
        /// <param name="value">Current stat value</param>
        /// <returns>Good if within baseline, Bad if outside</returns>
        public StatState GetState(float value)
        {
            return (value >= baselineMin && value <= baselineMax) ? StatState.Good : StatState.Bad;
        }
        
        /// <summary>
        /// Clamp a value to the absolute limits
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <returns>Clamped value</returns>
        public float ClampValue(float value)
        {
            return Mathf.Clamp(value, absoluteMin, absoluteMax);
        }
        
        /// <summary>
        /// Get a random starting value within the baseline range
        /// </summary>
        /// <returns>Random value between baseline min and max</returns>
        public float GetRandomStartValue()
        {
            return UnityEngine.Random.Range(baselineMin, baselineMax);
        }
    }

    /// <summary>
    /// Individual stat instance with current value and definition
    /// </summary>
    [System.Serializable]
    public class Stat
    {
        [SerializeField] private float currentValue;
        [SerializeField] private StatDefinition definition;
        
        public float CurrentValue => currentValue;
        public StatDefinition Definition => definition;
        public StatType Type => definition.statType;
        public StatState State => definition.GetState(currentValue);
        public string DisplayName => definition.displayName;
        
        public Stat(StatDefinition def, float startValue = -1f)
        {
            definition = def;
            currentValue = startValue < 0 ? def.GetRandomStartValue() : def.ClampValue(startValue);
        }
        
        /// <summary>
        /// Modify the stat value
        /// </summary>
        /// <param name="amount">Amount to change (can be negative)</param>
        public void Modify(float amount)
        {
            currentValue = definition.ClampValue(currentValue + amount);
        }
        
        /// <summary>
        /// Set the stat value directly
        /// </summary>
        /// <param name="value">New value</param>
        public void SetValue(float value)
        {
            currentValue = definition.ClampValue(value);
        }
        
        /// <summary>
        /// Apply the defined variation to this stat
        /// </summary>
        public void ApplyVariation()
        {
            if (definition.variationAmount != 0f)
            {
                Modify(definition.variationAmount);
            }
        }
        
        /// <summary>
        /// Get the percentage of the stat within its absolute range
        /// </summary>
        /// <returns>Percentage from 0 to 1</returns>
        public float GetPercentage()
        {
            float range = definition.absoluteMax - definition.absoluteMin;
            if (range <= 0) return 0f;
            return (currentValue - definition.absoluteMin) / range;
        }
    }
}

