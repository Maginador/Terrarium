# Spawn System Documentation

## Overview

The Spawn System is a comprehensive solution for automatically and manually spawning items in the terrarium. It supports three main categories: **Water**, **Food**, and **Entities**, each with their own configurable spawning parameters and item types.

## Key Features

### ✅ **Automatic Spawning**
- Time-based spawning with configurable intervals
- Random item selection from weighted lists
- Spawn limits and distance constraints
- Respects terrarium bounds and spawns on top of terrain

### ✅ **Manual Spawning API**
- Mouse-based spawning at click positions
- Specific item selection from categories
- Position-based spawning for custom locations
- Ready for UI integration

### ✅ **Base Classes for Consumables**
- **BaseConsumable**: Common functionality for all consumable items
- **FoodItem**: Food-specific mechanics (spoilage, nutrition, quality)
- **WaterItem**: Water-specific mechanics (evaporation, contamination, refill)

### ✅ **Advanced Item Mechanics**
- Consumption by NPCs with stat effects
- Visual feedback based on amount/quality
- Lifecycle management (spoilage, evaporation)
- Priority-based consumption for NPCs

## Architecture

### Core Components

```
SpawnSystem/
├── SpawnData.cs              # Data structures and configurations
├── SpawnManager.cs           # Main spawn system controller
├── BaseConsumable.cs         # Base class for consumable items
├── FoodItem.cs              # Food-specific implementation
├── WaterItem.cs             # Water-specific implementation
├── SpawnedItemTracker.cs    # Destruction tracking component
├── SpawnSystemSetup.cs      # Setup helper and testing utilities
└── README.md                # This documentation
```

### Data Flow

1. **SpawnManager** manages automatic and manual spawning
2. **SpawnCategoryConfig** defines spawnable items and parameters
3. **SpawnableItem** contains prefab references and spawn settings
4. **BaseConsumable** provides consumption mechanics
5. **SpawnedItemTracker** handles cleanup and notifications

## Usage

### Basic Setup

1. **Add SpawnManager to Scene**:
   ```csharp
   GameObject spawnManagerObj = new GameObject("SpawnManager");
   SpawnManager spawnManager = spawnManagerObj.AddComponent<SpawnManager>();
   ```

2. **Configure Spawn Categories**:
   ```csharp
   // Configure water spawning
   spawnManager.WaterConfig.autoSpawnConfig.spawnInterval = 10f;
   spawnManager.WaterConfig.autoSpawnConfig.maxItems = 5;
   spawnManager.WaterConfig.isEnabled = true;
   ```

3. **Add Spawnable Items**:
   ```csharp
   // Add water prefab
   GameObject waterPrefab = CreateWaterPrefab();
   spawnManager.WaterConfig.spawnableItems.Add(
       new SpawnableItem(waterPrefab, "Water Drop", 1f)
   );
   ```

### Automatic Spawning

The system automatically spawns items based on configured intervals:

```csharp
// Enable automatic spawning
spawnManager.EnableAutoSpawn = true;

// Configure spawn timing (0 or negative = disabled)
spawnManager.WaterConfig.autoSpawnConfig.spawnInterval = 5f; // Every 5 seconds
spawnManager.WaterConfig.autoSpawnConfig.timeVariation = 0.2f; // ±20% variation

// Set spawn limits
spawnManager.WaterConfig.autoSpawnConfig.maxItems = 10; // Max 10 water items
spawnManager.WaterConfig.autoSpawnConfig.minSpawnDistance = 2f; // Min 2 units apart
```

### Manual Spawning

Use the API for mouse-based or position-based spawning:

```csharp
// Spawn random item from category at mouse position
bool success = spawnManager.ManualSpawnAtMouse(SpawnCategory.Water);

// Spawn specific item at mouse position
bool success = spawnManager.ManualSpawnAtMouse(SpawnCategory.Food, "Apple");

// Spawn at specific position
Vector3 spawnPos = new Vector3(10, 5, 10);
bool success = spawnManager.ManualSpawnAtPosition(SpawnCategory.Entities, spawnPos);
```

### Creating Custom Items

#### Food Items

```csharp
public class Apple : FoodItem
{
    protected override void InitializeConsumable()
    {
        base.InitializeConsumable();
        
        // Set food-specific properties
        foodType = FoodType.Fruit;
        hungerSatisfaction = 15f;
        spoilageRate = 0.05f; // Spoils slowly
        quality = FoodQuality.Good;
    }
}
```

#### Water Items

```csharp
public class SpringWater : WaterItem
{
    protected override void InitializeConsumable()
    {
        base.InitializeConsumable();
        
        // Set water-specific properties
        waterType = WaterType.Spring;
        thirstSatisfaction = 20f;
        evaporationRate = 0.02f; // Evaporates slowly
        quality = WaterQuality.Spring;
        isStaticSource = true; // Refills over time
    }
}
```

## Configuration Options

### SpawnCategoryConfig

| Property | Description | Default |
|----------|-------------|---------|
| `category` | Spawn category (Water/Food/Entities) | - |
| `isEnabled` | Enable/disable this category | `true` |
| `spawnableItems` | List of spawnable items | `[]` |
| `autoSpawnConfig` | Automatic spawn settings | - |

### AutoSpawnConfig

| Property | Description | Default |
|----------|-------------|---------|
| `spawnInterval` | Time between spawns (0 = disabled) | `5f` |
| `timeVariation` | Random time variation (0-1) | `0.2f` |
| `maxItems` | Maximum items of this type | `10` |
| `minSpawnDistance` | Minimum distance between items | `2f` |
| `spawnHeight` | Height above terrain | `2f` |
| `heightVariation` | Random height variation | `1f` |

### SpawnableItem

| Property | Description | Default |
|----------|-------------|---------|
| `prefab` | GameObject prefab to spawn | `null` |
| `itemName` | Name of the item | `"Item"` |
| `spawnWeight` | Selection weight (higher = more likely) | `1f` |
| `isEnabled` | Enable/disable this item | `true` |
| `spawnOffset` | Position offset when spawning | `Vector3.zero` |
| `randomRotationRange` | Random rotation range | `Vector3.zero` |
| `randomizeRotation` | Enable random rotation | `false` |

## Events

### SpawnManager Events

```csharp
// Item spawned
spawnManager.OnItemSpawned += (category, item, position) => {
    Debug.Log($"Spawned {category} item at {position}");
};

// Item destroyed
spawnManager.OnItemDestroyed += (category, item) => {
    Debug.Log($"{category} item destroyed");
};

// Manual spawn requested
spawnManager.OnManualSpawnRequested += (category, item) => {
    Debug.Log($"Manual spawn requested: {item.itemName}");
};
```

### BaseConsumable Events

```csharp
// Amount changed
consumable.OnAmountChanged += (item, deltaAmount) => {
    Debug.Log($"Amount changed by {deltaAmount}");
};

// Item consumed
consumable.OnConsumed += (item, consumer) => {
    Debug.Log($"{consumer.DebugName} consumed {item.ItemName}");
};

// Item empty
consumable.OnEmpty += (item) => {
    Debug.Log($"{item.ItemName} is empty");
};
```

## Debug Features

### Visual Debugging

- **Spawn Area Gizmos**: Shows spawn boundaries in Scene view
- **Item Tracking**: Visual indicators for spawned items
- **Debug UI**: Real-time spawn information and controls
- **Console Logging**: Detailed spawn and consumption logs

### Debug Controls

```csharp
// Enable/disable debug info
spawnManager.IsDebugEnabled = true;

// Show/hide spawn gizmos
spawnManager.showSpawnGizmos = true;

// Test manual spawning
spawnManager.ManualSpawnAtMouse(SpawnCategory.Water);
```

## Integration with Existing Systems

### TerrariumManager Integration

- Spawns respect terrarium bounds
- Items spawn on top of terrain blocks
- Uses terrarium's grid system for positioning

### NPC System Integration

- NPCs can consume Food and Water items
- Consumption affects NPC stats (Health, Food, Water, Stress)
- Priority-based consumption based on NPC needs

### Debug System Integration

- All components implement `IDebuggable`
- Integrated with `DebugManager` for global debug control
- Real-time debug UI with spawn information

## Performance Considerations

### Object Pooling

- Spawned items use Unity's built-in object management
- Automatic cleanup of destroyed items
- Efficient tracking with minimal overhead

### Spawn Limits

- Configurable maximum items per category
- Distance-based spawning prevents overcrowding
- Automatic cleanup when limits are reached

### Update Optimization

- Spawn timing uses efficient time-based checks
- Minimal Update() calls with event-driven architecture
- Lazy cleanup of destroyed items

## Future Enhancements

### Planned Features

- [ ] **Advanced Spawn Patterns**: Spiral, grid, or custom spawn patterns
- [ ] **Seasonal Spawning**: Different spawn rates based on time/season
- [ ] **NPC Interaction**: NPCs can create or modify spawn points
- [ ] **Save/Load**: Persist spawn configurations and item states
- [ ] **Visual Effects**: Particle effects for spawning and consumption
- [ ] **Audio Integration**: Sound effects for spawn and consumption events

### API Extensions

- [ ] **Batch Spawning**: Spawn multiple items at once
- [ ] **Conditional Spawning**: Spawn based on environmental conditions
- [ ] **Spawn Zones**: Define specific areas for different item types
- [ ] **Dynamic Spawn Rates**: Adjust spawn rates based on game state

## Troubleshooting

### Common Issues

1. **Items not spawning**:
   - Check if `spawnInterval > 0`
   - Verify `isEnabled = true` for category
   - Ensure prefabs are assigned and valid

2. **Items spawning outside terrarium**:
   - Check `spawnAreaMargin` setting
   - Verify terrarium bounds are correct
   - Ensure `spawnOnTopOfTerrarium = true`

3. **Manual spawn not working**:
   - Check `enableManualSpawn = true`
   - Verify camera is assigned
   - Ensure spawn layer mask is correct

4. **Items not being consumed**:
   - Check if items have `BaseConsumable` component
   - Verify colliders are set as triggers
   - Ensure NPCs can reach the items

### Debug Commands

```csharp
// Clear all spawned items
spawnManager.ClearAllSpawnedItems();

// Get spawn counts
int waterCount = spawnManager.GetSpawnedItemCount(SpawnCategory.Water);

// Toggle auto spawn
spawnManager.EnableAutoSpawn = !spawnManager.EnableAutoSpawn;
```

## Conclusion

The Spawn System provides a robust, extensible foundation for managing item spawning in the terrarium. With its automatic and manual spawning capabilities, advanced consumable mechanics, and seamless integration with existing systems, it's ready for immediate use and future expansion.

The system is designed to be both powerful for advanced users and simple for basic setup, making it suitable for a wide range of terrarium simulation needs.

