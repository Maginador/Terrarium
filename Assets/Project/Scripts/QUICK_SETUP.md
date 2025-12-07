# 🚀 Quick Setup Reference

## ⚡ **5-Minute Setup**

### **1. Create GameObjects (2 minutes)**
```
Scene Hierarchy:
├── TerrariumEngine (Empty GameObject)
│   └── Add Component: GameManager
├── TerrariumManager (Empty GameObject)
│   └── Add Component: TerrariumManager
├── TimeManager (Empty GameObject)
│   └── Add Component: TimeManager
├── StatsManager (Empty GameObject)
│   └── Add Component: StatsManager
├── SpawnManager (Empty GameObject)
│   └── Add Component: SpawnManager
├── POIManager (Empty GameObject)
│   └── Add Component: POIManager
├── InputHandler (Empty GameObject)
│   └── Add Component: InputHandler
├── CameraController (Empty GameObject)
│   └── Add Component: CameraController
└── Main Camera
    ├── Position: (10, 15, 10), Rotation: (45, 0, 0)
    └── Add Component: CameraController
```

### **2. Create Prefabs (2 minutes)**

#### **SandBlockPrefab:**
```
1. Create Empty GameObject
2. Add: MeshRenderer, MeshFilter, BoxCollider, SandBlock
3. Set MeshFilter.mesh = Cube
4. Set Color = Yellow
5. Save as Prefab
```

#### **QueenPrefab:**
```
1. Create Empty GameObject
2. Add: MeshRenderer, MeshFilter, CapsuleCollider, Rigidbody, QueenNPC
3. Set MeshFilter.mesh = Capsule
4. Set Color = Red, Scale = (1.5, 1.5, 1.5)
5. Save as Prefab
```

#### **WorkerPrefab:**
```
1. Create Empty GameObject
2. Add: MeshRenderer, MeshFilter, CapsuleCollider, Rigidbody, WorkerNPC
3. Set MeshFilter.mesh = Capsule
4. Set Color = Blue, Scale = (0.8, 0.8, 0.8)
5. Save as Prefab
```

#### **FoodPrefab:**
```
1. Create Empty GameObject
2. Add: MeshRenderer, MeshFilter, SphereCollider, Rigidbody, FoodItem
3. Set MeshFilter.mesh = Sphere
4. Set Color = Green, Scale = (0.5, 0.5, 0.5)
5. Save as Prefab
```

#### **WaterPrefab:**
```
1. Create Empty GameObject
2. Add: MeshRenderer, MeshFilter, CylinderCollider, Rigidbody, WaterItem
3. Set MeshFilter.mesh = Cylinder
4. Set Color = Blue (with transparency), Scale = (0.4, 0.2, 0.4)
5. Save as Prefab
```

### **3. Configure Components (1 minute)**
```
TerrariumManager:
├── Sand Block Prefab: Drag SandBlockPrefab
├── Terrarium Size: (20, 10, 20)
├── Level of Quality: 1.0
└── Initial Pool Size: 200

GameManager:
├── Queen Prefab: Drag QueenPrefab
└── Worker Prefab: Drag WorkerPrefab
Note: Queen spawns automatically on top of terrarium center

InputHandler:
├── Player Camera: Drag Main Camera
├── Input Actions: Drag InputSystem_Actions asset
└── Ignore Glass Blocks: true

CameraController:
├── Move Speed: 10
├── Fast Move Speed: 20
├── Mouse Sensitivity: 2
└── Use Bounds: true

StatsManager:
├── Global Temperature: 75
├── Global Environment: 80
├── Temperature Variation: 5
├── Environment Variation: 10
└── Update Interval: 1

SpawnManager:
├── Enable Auto Spawn: true
├── Enable Manual Spawn: true
├── Spawn On Top Of Terrarium: true
├── Spawn Area Margin: 1
├── Food Items: Drag FoodPrefab
├── Water Items: Drag WaterPrefab
└── Entity Items: Drag WorkerPrefab

POIManager:
├── Global Detection Range: 50
├── Detection Update Interval: 0.5
└── Enable POI Tracking: true

Note: InputSystem_Actions.inputactions already exists in project
```

## ✅ **Test Checklist**
- [ ] Sand blocks are visible and yellow
- [ ] Glass walls are visible and transparent
- [ ] Queen spawns on top of sand blocks (red capsule)
- [ ] Workers spawn around Queen (blue capsules)
- [ ] Mouse clicks destroy sand blocks (through glass)
- [ ] WASD moves camera around
- [ ] Mouse look works for camera rotation
- [ ] Time controls work (1x, 2x, 4x, 10x)
- [ ] Debug UI shows information
- [ ] **NEW**: NPCs display all 7 stats (Health, Food, Water, Stress, Environment, Temperature, Space)
- [ ] **NEW**: Food decreases every 10 seconds
- [ ] **NEW**: Water decreases every 5 seconds
- [ ] **NEW**: Space stat changes based on nearby NPCs
- [ ] **NEW**: NPCs die when 2+ stats are in bad state
- [ ] **NEW**: Stats Manager shows global temperature/environment
- [ ] **NEW**: Spawn Manager automatically spawns Water, Food, and Entities
- [ ] **NEW**: Manual spawn works with mouse clicks (API ready for implementation)
- [ ] **NEW**: Food and Water items have consumption mechanics
- [ ] **NEW**: Spawned items respect terrarium bounds and spawn on top of terrain
- [ ] **NEW**: Queen creates food and water deposits automatically
- [ ] **NEW**: Workers pick up food/water and transport to deposits
- [ ] **NEW**: Workers consume food/water when hungry/thirsty
- [ ] **NEW**: Workers fight each other when stress is low
- [ ] **NEW**: Workers break sand blocks randomly when idle
- [ ] **NEW**: POI system shows detection ranges for food/water
- [ ] **NEW**: AI behaviors switch based on priority (Combat > Transport > Consume > Fallback)
- [ ] No console errors

## 🎮 **Play!**
Your terrarium is ready! The Queen will spawn workers, and you can destroy sand blocks with mouse clicks.

---

**For detailed setup instructions, see `SETUP_GUIDE.md`**
