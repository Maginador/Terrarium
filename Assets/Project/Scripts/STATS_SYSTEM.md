# 📊 Stats System Documentation

## Overview
The Terrarium Engine now includes a comprehensive stats system that manages entity health, needs, and environmental factors. All entities (NPCs) have 7 core stats that affect their survival and behavior.

## Core Stats

### 1. **Health** (0-100)
- **Baseline Range**: 50-100 (Good state)
- **Variation**: None (only affected by damage/healing)
- **Purpose**: Primary survival stat

### 2. **Food** (0-100)
- **Baseline Range**: 50-100 (Good state)
- **Variation**: -1 every 10 seconds
- **Purpose**: Represents hunger level

### 3. **Water** (0-100)
- **Baseline Range**: 50-100 (Good state)
- **Variation**: -1 every 5 seconds
- **Purpose**: Represents thirst level

### 4. **Stress** (0-100)
- **Baseline Range**: 50-100 (Good state)
- **Variation**: Affected by other stats
- **Purpose**: Mental well-being affected by other needs

### 5. **Environment** (0-100)
- **Baseline Range**: 50-100 (Good state)
- **Variation**: Affected by global environment
- **Purpose**: Environmental comfort level

### 6. **Temperature** (0-100)
- **Baseline Range**: 50-100 (Good state)
- **Variation**: Affected by global temperature
- **Purpose**: Thermal comfort level

### 7. **Space** (0-100)
- **Baseline Range**: 50-100 (Good state)
- **Variation**: Calculated every 20 seconds based on nearby entities
- **Purpose**: Crowding/isolation level

## Stat States

### Good State (✓)
- Stat value is within baseline range (50-100)
- Entity is comfortable with this need

### Bad State (✗)
- Stat value is below 50 or above 100
- Entity is struggling with this need

## Death Conditions

### Primary Death Condition
- **Trigger**: 2 or more stats in Bad state
- **Action**: Entity is destroyed immediately
- **Configurable**: `badStatsForDeath` in NPCStats (default: 2)

### Secondary Death Condition
- **Trigger**: Health reaches 0
- **Action**: Entity is destroyed immediately

## Automatic Variations

### Food Depletion
- **Frequency**: Every 10 seconds
- **Change**: -1 point
- **Purpose**: Simulates hunger over time

### Water Depletion
- **Frequency**: Every 5 seconds
- **Change**: -1 point
- **Purpose**: Simulates thirst over time

### Space Calculation
- **Frequency**: Every 20 seconds
- **Logic**: 
  - If >10 entities within 10 units: -1 (too crowded)
  - If <2 entities within 10 units: +1 (too isolated)
  - Otherwise: no change

## External Factors

### Temperature
- **Source**: Global temperature from StatsManager
- **Effect**: Gradually moves entity temperature towards global value
- **Variation**: ±5 degrees with slight random fluctuations

### Environment
- **Source**: Global environment from StatsManager
- **Effect**: Gradually moves entity environment towards global value
- **Variation**: ±10 points with slight random fluctuations

### Stress
- **Sources**: 
  - +0.5 if Food is bad
  - +0.5 if Water is bad
  - +0.3 if Space is bad
  - -0.1 if all basic needs are good

## Usage Examples

### Basic Stat Access
```csharp
// Get a specific stat
var healthStat = npc.GetStat(StatType.Health);
float currentHealth = healthStat.CurrentValue;
bool isHealthy = healthStat.State == StatState.Good;

// Modify a stat
npc.ModifyStat(StatType.Food, 10f); // Add 10 food
npc.SetStat(StatType.Water, 80f);   // Set water to 80

// Check all bad stats
var badStats = npc.Stats.GetBadStats();
if (badStats.Count >= 2)
{
    // Death condition met!
}
```

### Event Handling
```csharp
// Subscribe to stat changes
npc.Stats.OnStatChanged += (statType, newValue) => {
    Debug.Log($"{statType} changed to {newValue}");
};

// Subscribe to death conditions
npc.Stats.OnDeathConditionMet += (badStats) => {
    Debug.Log($"Death! Bad stats: {string.Join(", ", badStats)}");
};
```

### Custom Stat Modifications
```csharp
// Environmental effects
public void ApplyHeatWave(BaseNPC npc)
{
    npc.ModifyStat(StatType.Temperature, 20f);
    npc.ModifyStat(StatType.Water, -5f); // Heat causes dehydration
}

// Feeding system
public void FeedNPC(BaseNPC npc, float foodAmount)
{
    npc.ModifyStat(StatType.Food, foodAmount);
    npc.ModifyStat(StatType.Stress, -2f); // Eating reduces stress
}
```

## Configuration

### Stat Definitions
Each stat can be configured with:
- **baselineMin/Max**: Good state range
- **absoluteMin/Max**: Hard limits
- **variationAmount**: How much changes per cycle
- **variationInterval**: Time between variations
- **affectedByExternal**: Whether external factors apply

### Death Conditions
- **badStatsForDeath**: Number of bad stats required for death (default: 2)
- **maxLifespan**: Maximum age before death (default: 300 seconds)

## Debug Features

### Visual Debug Info
- All stats displayed above each NPC
- Green (✓) for good stats, Red (✗) for bad stats
- Bad stats counter showing progress towards death
- Stats Manager UI showing global values

### Console Logging
- Stat changes logged when debug enabled
- Death conditions logged with bad stats list
- Environmental changes logged

## Integration with Existing Systems

### Backward Compatibility
- Legacy `currentHealth` and `maxHealth` still work
- `TakeDamage()` and `Heal()` methods still function
- Existing NPC behavior preserved

### Time System Integration
- All variations respect game time scale
- Stats update based on scaled delta time
- Time acceleration affects stat depletion rates

### Debug System Integration
- Stats Manager implements `IDebuggable`
- Individual NPC stats shown in debug UI
- Global stats management through debug interface

## Performance Considerations

### Optimization Features
- Stats only update when necessary
- Space calculation uses efficient distance checks
- Event-driven updates minimize unnecessary processing
- Configurable update intervals for environmental effects

### Memory Management
- Stat definitions shared across all NPCs
- Efficient stat storage with minimal overhead
- Event subscription/unsubscription handled properly

## Future Enhancements

### Planned Features
- [ ] Stat-based behavior modifications
- [ ] Resource gathering to restore stats
- [ ] Seasonal environmental changes
- [ ] Stat-based reproduction requirements
- [ ] Advanced stress calculation algorithms
- [ ] Stat-based visual effects

### Extensibility
- Easy to add new stat types
- Customizable stat definitions per NPC type
- Pluggable environmental effect systems
- Modifiable death condition logic

## Troubleshooting

### Common Issues
1. **Stats not updating**: Check if `InitializeStats()` was called
2. **Death not triggering**: Verify `badStatsForDeath` setting
3. **Space stat not changing**: Ensure NPCs are within 10 units of each other
4. **Environmental effects not working**: Check StatsManager is present and active

### Debug Commands
- Use StatsManager "Test Death" button to trigger death condition
- Use StatsManager "Refresh NPCs" to update NPC list
- Enable debug mode to see detailed stat information

---

The stats system provides a robust foundation for complex entity behavior and survival mechanics while maintaining clean, extensible code architecture.

