# 🤖 AI System Documentation

## Overview

The Terrarium AI System provides intelligent behavior for Queen and Worker NPCs, enabling complex colony management, resource gathering, combat, and environmental interaction.

## 🏗️ Architecture

### Core Components

```
AI System Architecture:
├── AIConstants.cs                    # Centralized configuration
├── AIBehaviorBase.cs                 # Base class for all behaviors
├── AIStateMachine.cs                 # Behavior prioritization system
├── QueenAI/
│   ├── QueenOrganizationSystem.cs    # Queen coordination & deposit management
│   └── QueenNPC.cs                   # Queen NPC implementation
├── WorkerAI/
│   ├── WorkerTransportBehavior.cs    # Pickup, transport, deposit logic
│   ├── WorkerCombatBehavior.cs       # Combat mechanics
│   ├── WorkerConsumptionBehavior.cs  # Food/water consumption
│   ├── WorkerFallbackBehavior.cs     # Random movement & sand breaking
│   └── WorkerNPC.cs                  # Worker NPC implementation
└── DepositSystem/
    └── Deposit.cs                    # Deposit area management
```

## 🎯 Behavior Priority System

Workers use a priority-based state machine with the following hierarchy:

1. **Combat (Priority 100)** - Attack other workers when stress is low
2. **Transport (Priority 80)** - Pick up and transport food/water
3. **Consumption (Priority 60)** - Consume food/water when hungry/thirsty
4. **Pickup (Priority 40)** - Seek and pick up items
5. **Fallback (Priority 20)** - Random movement and sand breaking

## 👑 Queen System

### QueenOrganizationSystem

The Queen manages the colony through:

- **Deposit Management**: Creates and manages food/water deposit areas
- **Resource Requests**: Requests food/water from workers when needed
- **Consumption**: Consumes resources delivered by workers
- **Worker Coordination**: Commands workers within range

### Key Features

- **Origin Constraint**: Queen stays within 3 units of origin (configurable)
- **Automatic Deposits**: Creates food and water deposits automatically
- **Resource Detection**: Finds and consumes nearby resources
- **Worker Communication**: Coordinates with workers via events

## 👷 Worker System

### WorkerTransportBehavior

Handles the complete transport cycle:

1. **Seeking Pickup**: Finds nearby food/water items
2. **Moving to Pickup**: Navigates to item location
3. **Picking Up**: Uses NPCPicker to grab items
4. **Seeking Deposit**: Determines target deposit
5. **Moving to Deposit**: Navigates to deposit area
6. **Depositing**: Drops items in deposit area
7. **Delivering to Queen**: Special case for Queen requests

### WorkerCombatBehavior

Combat system based on stress levels:

- **Stress Threshold**: Workers become aggressive when stress < 20%
- **Combat Range**: 1.5m attack range
- **Damage**: 10 health points per attack
- **Death Effects**: Reduces stress for other workers
- **Target Selection**: Attacks nearest worker in range

### WorkerConsumptionBehavior

Resource consumption at deposits:

- **Consumption Rate**: 10 food/water per second
- **Threshold**: Consumes when below 30% of resource
- **Deposit Requirement**: Only consumes at designated deposits
- **Priority**: Based on need and distance

### WorkerFallbackBehavior

Idle behavior when no other actions available:

- **Random Movement**: 50% chance to move randomly
- **Sand Breaking**: 10% chance to break random sand blocks
- **Hole Preference**: 80% chance to break near existing holes
- **Search Range**: 5m radius for existing holes

## 🏪 Deposit System

### Deposit Management

- **Automatic Creation**: Queen creates deposits at startup
- **Area Clearing**: Removes sand blocks in deposit areas (2 levels max)
- **Radius**: 3m deposit areas (configurable)
- **Visual Feedback**: Color-coded areas (green=food, blue=water)

### Sand Clearing

- **Automatic Clearing**: Deposits clear sand blocks periodically
- **Level Limit**: Maximum 2 sand levels cleared
- **Clearing Radius**: 5m around deposit center
- **Performance**: Updates every 2 seconds

## ⚙️ Configuration

### AIConstants

All AI behavior is configurable through `AIConstants`:

```csharp
// Queen Settings
queenMaxDistanceFromOrigin = 3f;
queenConsumptionRange = 2f;
queenRequestInterval = 5f;

// Worker Transport
depositRadius = 3f;
maxSandLevelsToClear = 2;
pickupRange = 2f;
carryingSpeedMultiplier = 0.7f;

// Worker Consumption
foodConsumptionRate = 10f;
waterConsumptionRate = 10f;
consumptionRange = 1f;

// Worker Combat
combatStressThreshold = 0.2f;
combatDamage = 10f;
maxWorkerHealth = 100f;
combatRange = 1.5f;

// Stress System
stressIncreaseInterval = 10f;
stressIncreaseThreshold = 0.5f;
stressIncreaseAmount = 0.05f;

// Fallback Behaviors
randomSandBreakChance = 0.1f;
existingHoleBreakChance = 0.8f;
holeSearchRange = 5f;
```

## 🔄 State Machine

### Behavior Transitions

The AI State Machine handles smooth transitions between behaviors:

- **Priority-Based**: Higher priority behaviors interrupt lower ones
- **Condition-Based**: Behaviors activate based on game state
- **Event-Driven**: Behaviors communicate through events
- **Debug-Friendly**: Full state transition logging

### State Management

```csharp
// Example: Transport behavior states
enum TransportState
{
    Idle,
    SeekingPickup,
    MovingToPickup,
    PickingUp,
    SeekingDeposit,
    MovingToDeposit,
    Depositing,
    DeliveringToQueen
}
```

## 🎮 Integration with Existing Systems

### Spawn System Integration

- **Food/Water POIs**: Items automatically emit POI signals
- **Spawned Items**: Respect terrarium bounds and physics
- **Lifecycle Management**: Items rot and become unusable over time

### Stats System Integration

- **Resource Consumption**: Affects Food/Water stats
- **Combat Damage**: Affects Health stat
- **Stress Management**: Affects Stress stat
- **Environmental Effects**: Affects Environment stat

### Terrarium Integration

- **Sand Block Destruction**: Workers break sand blocks
- **Boundary Respect**: All movement respects terrarium bounds
- **Physics Integration**: Items fall and interact with terrain

## 🐛 Debug Features

### Visual Debugging

- **Behavior States**: Real-time display of current behavior
- **POI Ranges**: Visual representation of detection areas
- **Combat Ranges**: Red spheres showing attack ranges
- **Deposit Areas**: Color-coded deposit zones
- **Transport Paths**: Lines showing movement targets

### Console Logging

- **State Transitions**: Logs when behaviors change
- **Combat Events**: Logs attacks and deaths
- **Transport Events**: Logs pickup and drop actions
- **Consumption Events**: Logs resource consumption
- **Error Handling**: Comprehensive error logging

## 🚀 Performance Considerations

### Optimization Features

- **Update Intervals**: Configurable update rates for different systems
- **Range-Based Detection**: Only processes nearby entities
- **State Caching**: Caches expensive calculations
- **Event-Driven Updates**: Only updates when necessary

### Scalability

- **Component-Based**: Modular design allows easy extension
- **Configurable Limits**: All limits are adjustable
- **Efficient Queries**: Optimized entity searches
- **Memory Management**: Proper cleanup and disposal

## 🔧 Extensibility

### Adding New Behaviors

1. Inherit from `AIBehaviorBase`
2. Implement required methods
3. Set appropriate priority
4. Register with state machine
5. Configure in `AIConstants`

### Adding New NPC Types

1. Inherit from `BaseNPC`
2. Add specific behaviors
3. Configure in `AIConstants`
4. Create prefab with components
5. Add to spawn system

### Customizing Existing Behaviors

- **Override Methods**: Extend base behavior classes
- **Modify Constants**: Adjust behavior parameters
- **Add Events**: Subscribe to behavior events
- **Custom States**: Add new state machine states

## 📊 Monitoring and Analytics

### Built-in Metrics

- **Behavior Distribution**: Track which behaviors are most active
- **Combat Statistics**: Monitor attack frequency and outcomes
- **Transport Efficiency**: Measure pickup and delivery success
- **Resource Consumption**: Track food/water usage patterns

### Performance Metrics

- **Update Times**: Monitor AI system performance
- **Memory Usage**: Track component memory consumption
- **Entity Counts**: Monitor active NPCs and items
- **State Transitions**: Track behavior switching frequency

## 🎯 Best Practices

### Development Guidelines

1. **Use Constants**: Always use `AIConstants` for configurable values
2. **Event-Driven**: Use events for inter-component communication
3. **State Management**: Let the state machine handle behavior switching
4. **Debug-Friendly**: Include comprehensive debug information
5. **Performance-Conscious**: Consider update intervals and ranges

### Testing Recommendations

1. **Start Simple**: Test individual behaviors before integration
2. **Use Debug Mode**: Enable all debug features during development
3. **Monitor Performance**: Watch for performance issues with many NPCs
4. **Test Edge Cases**: Test behavior transitions and error conditions
5. **Validate Constants**: Ensure all configurable values work as expected

## 🔮 Future Enhancements

### Planned Features

- **Advanced Pathfinding**: A* pathfinding for complex navigation
- **Group Behaviors**: Coordinated group actions
- **Learning AI**: Adaptive behavior based on experience
- **Environmental Awareness**: More sophisticated environmental interaction
- **Communication System**: Inter-NPC communication and coordination

### Extension Points

- **Custom Behaviors**: Easy addition of new behavior types
- **AI Personalities**: Different behavior patterns for different NPCs
- **Dynamic Priorities**: Priorities that change based on context
- **Multi-Objective**: Behaviors that pursue multiple goals
- **Hierarchical AI**: Sub-behaviors within main behaviors

---

This AI system provides a solid foundation for complex NPC behavior while maintaining performance and extensibility. The modular design allows for easy customization and extension as the project grows.

