# Terrarium Engine Architecture

## Overview
The Terrarium Engine is a modular, extensible system for creating terrarium simulation games. It follows clean architecture principles with clear separation of concerns and reusable components.

## Core Systems

### 1. Debug System
- **IDebuggable Interface**: Common interface for all debug-enabled components
- **DebugManager**: Singleton manager that controls debug state globally
- **Features**: Individual component debug control, global debug toggle, runtime debug UI

### 2. Time System
- **TimeManager**: Controls game time flow with acceleration capabilities
- **Features**: 1x, 2x, 4x, 10x speed controls, debug UI, time scale events
- **Integration**: All systems respect time scale for consistent behavior

### 3. Terrain System
- **TerrariumManager**: Main terrain controller
- **SandBlock**: Individual destructible terrain blocks
- **ObjectPool**: Efficient object reuse system
- **Features**: Configurable size, Level of Quality (LoQ), material system, grid-based positioning

### 4. Input System
- **InputHandler**: Mouse and keyboard input processing
- **Features**: World position detection, sand block destruction, raycast-based interaction
- **Integration**: Works with existing Unity Input System

### 5. NPC System
- **BaseNPC**: Abstract base class for all NPCs
- **NPCStats**: Configurable stats system (health, movement, lifespan)
- **QueenNPC**: Spawns and manages workers
- **WorkerNPC**: Random movement with collision avoidance
- **Features**: Lifecycle management, behavior patterns, debug visualization

## Architecture Principles

### 1. Interface-Based Design
- `IDebuggable`: Common debug interface
- `IDestructible`: Common destruction interface
- Enables polymorphism and easy extension

### 2. Singleton Pattern
- `DebugManager`: Global debug control
- `TimeManager`: Global time control
- Ensures single instances of critical systems

### 3. Component-Based Architecture
- Each system is a self-contained component
- Clear responsibilities and minimal coupling
- Easy to test and maintain

### 4. Event-Driven Communication
- Systems communicate through events
- Loose coupling between components
- Easy to extend and modify

### 5. Object Pooling
- Efficient memory management
- Reusable sand blocks
- Prevents garbage collection spikes

## File Structure

```
Assets/Project/Scripts/
├── TerrariumEngine/
│   ├── IDebuggable.cs          # Debug interface
│   ├── DebugManager.cs         # Debug system manager
│   ├── TimeManager.cs          # Time control system
│   ├── IDestructible.cs        # Destruction interface
│   ├── ObjectPool.cs           # Object pooling system
│   ├── SandBlock.cs            # Individual sand blocks
│   ├── TerrariumManager.cs     # Main terrain manager
│   └── GameManager.cs          # Game coordination
├── AI/
│   ├── NPCStats.cs             # NPC statistics system
│   ├── BaseNPC.cs              # Base NPC class
│   ├── QueenNPC.cs             # Queen NPC implementation
│   └── WorkerNPC.cs            # Worker NPC implementation
└── Input/
    └── InputHandler.cs         # Input processing system
```

## Usage Instructions

### 1. Manual Setup (Recommended)
**⚠️ IMPORTANT**: For proper fine-tuning and control, use manual setup instead of automatic initialization.

**See `SETUP_GUIDE.md` for complete step-by-step instructions.**

#### Quick Setup Summary:
1. **Create Core GameObjects**:
   - TerrariumEngine (with GameManager)
   - TerrariumManager (with component)
   - TimeManager (with component)
   - InputHandler (with component)

2. **Create Prefabs**:
   - SandBlockPrefab (with SandBlock component)
   - QueenPrefab (with QueenNPC component)
   - WorkerPrefab (with WorkerNPC component)

3. **Configure Components**:
   - Assign prefabs to managers
   - Set terrarium parameters
   - Configure camera and lighting
   - Note: Queen spawn position is automatically calculated

### 2. Automatic Setup (Not Recommended)
1. Create a GameObject in your scene
2. Add the `GameManager` component
3. The system will automatically initialize all required components
4. **Note**: This method doesn't allow fine-tuning of parameters

### 3. Configuration
- **Terrarium Size**: Set in TerrariumManager
- **Time Speeds**: Configure in TimeManager
- **NPC Behavior**: Modify stats in NPCStats
- **Debug Elements**: Control through DebugManager

### 4. Extending the System
- **New NPC Types**: Inherit from BaseNPC
- **New Destructible Objects**: Implement IDestructible
- **New Debug Elements**: Implement IDebuggable
- **New Input Actions**: Extend InputHandler

## Key Features

### Time System
- ✅ Configurable time acceleration (1x, 2x, 4x, 10x)
- ✅ Debug UI with speed controls
- ✅ Global time scale management

### Terrain System
- ✅ Configurable terrarium size and detail level
- ✅ Destructible sand blocks with object pooling
- ✅ Grid-based positioning system
- ✅ Material and color customization

### NPC System
- ✅ Queen spawns workers at configurable intervals
- ✅ Workers move randomly with collision avoidance
- ✅ Lifecycle management (health, age, death)
- ✅ Debug visualization for all NPCs

### Input System
- ✅ Mouse click to destroy sand blocks
- ✅ World position detection
- ✅ Raycast-based interaction
- ✅ Debug information display

## Future Enhancements

### Planned Features
- [ ] Advanced AI behaviors
- [ ] Resource gathering system
- [ ] Environmental factors (temperature, humidity)
- [ ] Save/load system
- [ ] Performance optimization
- [ ] Visual effects system

### Architecture Improvements
- [ ] Dependency injection container
- [ ] ScriptableObject-based configuration
- [ ] Event system with type safety
- [ ] Unit testing framework
- [ ] Performance profiling tools

## Performance Considerations

### Current Optimizations
- Object pooling for sand blocks
- Grid-based spatial queries
- Efficient raycast usage
- Minimal Update() calls

### Future Optimizations
- Spatial partitioning for large terrains
- LOD system for distant objects
- Culling for off-screen elements
- Batch rendering for similar objects

## Debugging

### Debug Features
- Real-time debug UI for all systems
- Individual component debug control
- Performance metrics display
- Visual gizmos for spatial data

### Debug Controls
- Toggle individual system debug info
- Global debug enable/disable
- Runtime configuration changes
- Visual debugging aids

## Conclusion

The Terrarium Engine provides a solid foundation for terrarium simulation games with:
- Clean, modular architecture
- Extensible design patterns
- Comprehensive debug system
- Efficient performance
- Easy-to-use API

The system is designed to grow with your project needs while maintaining clean code and good performance.
