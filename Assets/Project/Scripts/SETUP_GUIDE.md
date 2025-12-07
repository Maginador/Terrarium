# 🏗️ Terrarium Engine Setup Guide

## 🎯 **Issues Identified & Solutions**

### **Current Problems:**
1. **Queen spawns at fixed position** - not on top of sand blocks
2. **Sand blocks are disabled** - object pool creates them as inactive
3. **Automatic component creation** - makes fine-tuning difficult
4. **Terrain generation issues** - blocks not properly positioned

---

## 📋 **Manual Setup Instructions**

### **Step 1: Create Core GameObjects**

#### **1.1 Main Scene Setup**
```
Create Empty GameObject: "TerrariumEngine"
├── Add Component: GameManager
└── Add Component: DebugManager (if not auto-created)
```

#### **1.2 TerrariumManager Setup**
```
Create Empty GameObject: "TerrariumManager"
├── Add Component: TerrariumManager
├── Position: (0, 0, 0)
└── Configure Settings:
    ├── Terrarium Size: (20, 10, 20)
    ├── Level of Quality: 1.0
    ├── Initial Pool Size: 200
    ├── Max Pool Size: 1000
    └── Default Sand Color: Yellow
```

#### **1.3 TimeManager Setup**
```
Create Empty GameObject: "TimeManager"
├── Add Component: TimeManager
├── Position: (0, 0, 0)
└── Configure Settings:
    ├── Time Speeds: [1, 2, 4, 10]
    ├── Default Time Speed Index: 0
    └── Show Debug UI: true
```

#### **1.4 InputHandler Setup**
```
Create Empty GameObject: "InputHandler"
├── Add Component: InputHandler
├── Position: (0, 0, 0)
└── Configure Settings:
    ├── Player Camera: Assign Main Camera
    ├── Sand Block Layer Mask: Default (Everything)
    ├── Input Actions: Create new InputSystem_Actions asset
    ├── Ignore Glass Blocks: true
    └── Show Debug Info: true
```

#### **1.5 CameraController Setup**
```
Create Empty GameObject: "CameraController"
├── Add Component: CameraController
├── Position: (0, 0, 0)
└── Configure Settings:
    ├── Move Speed: 10
    ├── Fast Move Speed: 20
    ├── Mouse Sensitivity: 2
    ├── Use Bounds: true
    ├── Min Bounds: (-50, 5, -50)
    ├── Max Bounds: (50, 50, 50)
    └── Show Debug Info: true
```

#### **1.6 Input System Configuration**
```
1. Use Existing Input Actions Asset:
   ├── The project already has "InputSystem_Actions.inputactions"
   ├── This file contains the "Attack" action bound to left mouse button
   └── No additional setup needed

2. Assign to InputHandler:
   ├── Select InputHandler GameObject
   ├── Drag "InputSystem_Actions" asset to "Input Actions" field
   └── Verify the assignment in inspector

3. Generate C# Class (if needed):
   ├── Select InputSystem_Actions.inputactions in Project
   ├── In Inspector, check "Generate C# Class"
   ├── Click "Apply" to generate the C# wrapper
   └── This creates InputSystem_Actions.cs automatically
```

### **Step 2: Create Sand Block Prefab**

#### **2.1 Create Sand Block Prefab**
```
1. Create Empty GameObject: "SandBlockPrefab"
2. Add Component: MeshRenderer
3. Add Component: MeshFilter
4. Add Component: BoxCollider
5. Add Component: SandBlock
6. Set MeshFilter.mesh = Cube (from Primitive)
7. Set Material/Color = Yellow
8. Scale: (1, 1, 1)
9. Save as Prefab in Project/Prefabs/
```

#### **2.2 Assign Prefab to TerrariumManager**
```
1. Select TerrariumManager GameObject
2. Drag SandBlockPrefab to "Sand Block Prefab" field
3. Set Default Sand Material (optional)
```

### **Step 3: Create NPC Prefabs**

#### **3.1 Queen Prefab**
```
1. Create Empty GameObject: "QueenPrefab"
2. Add Component: MeshRenderer
3. Add Component: MeshFilter
4. Add Component: CapsuleCollider
5. Add Component: Rigidbody
6. Add Component: QueenNPC
7. Set MeshFilter.mesh = Capsule
8. Set Material/Color = Red
9. Scale: (1.5, 1.5, 1.5)
10. Rigidbody Settings:
    ├── Mass: 2
    ├── Linear Damping: 2
    └── Angular Damping: 5
11. Save as Prefab
```

#### **3.2 Worker Prefab**
```
1. Create Empty GameObject: "WorkerPrefab"
2. Add Component: MeshRenderer
3. Add Component: MeshFilter
4. Add Component: CapsuleCollider
5. Add Component: Rigidbody
6. Add Component: WorkerNPC
7. Set MeshFilter.mesh = Capsule
8. Set Material/Color = Blue
9. Scale: (0.8, 0.8, 0.8)
10. Rigidbody Settings:
    ├── Mass: 1
    ├── Linear Damping: 1
    └── Angular Damping: 3
11. Save as Prefab
```

### **Step 4: Configure GameManager**

#### **4.1 GameManager Settings**
```
1. Select GameManager GameObject
2. Assign Prefabs:
    ├── Queen Prefab: Drag QueenPrefab
    └── Worker Prefab: Drag WorkerPrefab
3. Enable Show Debug Info: true
Note: Queen spawn position is automatically calculated on top of terrarium center
```

### **Step 5: Input System Setup**

#### **5.1 Use Existing Input Actions**
```
1. The project already includes "InputSystem_Actions.inputactions"
2. This asset contains the "Attack" action properly configured
3. No additional Input Actions setup needed
```

#### **5.2 Generate C# Class (Optional)**
```
1. Select "InputSystem_Actions.inputactions" in Project window
2. In Inspector, check "Generate C# Class"
3. Click "Apply" to generate InputSystem_Actions.cs
4. This enables type-safe access to input actions
```

#### **5.3 Assign to InputHandler**
```
1. Select InputHandler GameObject
2. Drag "InputSystem_Actions" asset to "Input Actions" field
3. Verify the assignment in inspector
4. The system will work with or without the generated C# class
```

### **Step 7: Camera Setup**

#### **7.1 Main Camera Configuration**
```
1. Select Main Camera
2. Position: (10, 15, 10) - Above and looking down at terrarium
3. Rotation: (45, 0, 0) - Angled view
4. Projection: Perspective
5. Field of View: 60
6. Assign to InputHandler's Player Camera field
7. Add CameraController component to Main Camera
```

#### **7.2 CameraController Configuration**
```
1. Select Main Camera (with CameraController component)
2. Configure Movement Settings:
    ├── Move Speed: 10
    ├── Fast Move Speed: 20
    ├── Mouse Sensitivity: 2
    └── Invert Y: false
3. Configure Bounds:
    ├── Use Bounds: true
    ├── Min Bounds: (-50, 5, -50)
    └── Max Bounds: (50, 50, 50)
4. Enable Show Debug Info: true
```

---

## 🔧 **Code Fixes Required**

### **Fix 1: Queen Spawn Position**
The Queen should spawn on top of the highest sand block in the center.

### **Fix 2: Sand Block Pool Issue**
The object pool creates blocks as inactive, but they should be active when placed.

### **Fix 3: Terrain Generation**
Ensure blocks are properly positioned and visible.

---

## 🎮 **Testing Checklist**

### **After Setup, Verify:**
- [ ] TerrariumManager generates visible sand blocks
- [ ] Queen spawns on top of sand blocks (not floating)
- [ ] Workers spawn around Queen
- [ ] Mouse clicks destroy sand blocks
- [ ] Time controls work (1x, 2x, 4x, 10x)
- [ ] Debug UI shows all information
- [ ] NPCs move and avoid obstacles
- [ ] No console errors

---

## ⚙️ **Recommended Settings**

### **TerrariumManager:**
```
Terrarium Size: (20, 10, 20)
Level of Quality: 1.0
Initial Pool Size: 200
Max Pool Size: 1000
Default Sand Color: (1, 0.8, 0.2) - Sandy yellow
```

### **TimeManager:**
```
Time Speeds: [1, 2, 4, 10]
Default Time Speed Index: 0
Show Debug UI: true
```

### **QueenNPC:**
```
Spawn Interval: 10 seconds
Spawn Range: 3 units
Max Workers: 20
Idle Movement Range: 5 units
```

### **WorkerNPC:**
```
Direction Change Interval: 3 seconds
Movement Range: 10 units
Obstacle Avoidance Distance: 1 unit
```

---

## 🚨 **Common Issues & Solutions**

### **Issue: Sand blocks not visible**
- **Solution**: Check if SandBlockPrefab is assigned to TerrariumManager
- **Solution**: Verify sand blocks are being activated in the pool

### **Issue: Queen spawns in air**
- **Solution**: Calculate spawn position based on terrain height
- **Solution**: Use TerrariumManager to find highest block at center

### **Issue: Workers not spawning**
- **Solution**: Check if WorkerPrefab is assigned to QueenNPC
- **Solution**: Verify QueenNPC component is properly configured

### **Issue: Mouse clicks not working**
- **Solution**: Ensure Main Camera is assigned to InputHandler
- **Solution**: Check if sand blocks have colliders

---

## 📁 **Final Scene Hierarchy**
```
Scene
├── TerrariumEngine (GameManager)
├── TerrariumManager
├── TimeManager
├── InputHandler
├── Main Camera
├── Directional Light
└── SandBlocks (auto-created by TerrariumManager)
    ├── SandBlock_0
    ├── SandBlock_1
    └── ... (pooled objects)
```

---

## 🎯 **Next Steps After Setup**

1. **Test Basic Functionality**
2. **Fine-tune Parameters**
3. **Add Visual Polish**
4. **Implement Additional Features**
5. **Performance Optimization**

This manual setup ensures you have full control over all parameters and can fine-tune the system to your needs!
