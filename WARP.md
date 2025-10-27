# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview
A 3D Metroidvania game inspired by Metroid Prime, built with Godot 4.5 using C# (.NET 8.0). The game features first-person exploration, platforming, puzzles, and boss battles with retro-inspired mechanics.

## Development Commands

### Building
This is a Godot C# project. Build the project using:
```bash
dotnet build Metroidvania.csproj
```

### Running the Project
- Open the project in Godot Editor and press F5
- Or run from command line: `godot --path . --headless` (for headless mode)
- Main scene: `src/resources/scenes/MainScene.tscn`

### Testing
Test scene available at: `src/resources/scenes/TestScene.tscn`

### Debugging
- Use the debug teleport button (F2 by default) to teleport to position (0, 1, -16)
- Player respawns when falling below Y = -64

## Code Architecture

### Player Movement System
The player movement is built on a custom physics-based character controller with the following hierarchy:

1. **MotionController** (`src/main/core/player/MotionController.cs`) - Abstract base class providing:
   - Custom physics-based movement (not using CharacterBody3D)
   - RigidBody3D-based movement with friction simulation
   - Ground, air, and water movement states
   - Slope handling with angle detection
   - Dynamic reparenting for moving platforms
   - Velocity calculations using Source engine-inspired acceleration

2. **Player_MCON** (`src/main/core/player/Player_MCON.cs`) - Concrete implementation:
   - Input handling (WASD movement, Space jump, Shift crouch)
   - Mouse look with sensitivity control
   - Crouch mechanics with collision detection
   - Extends MotionController's abstract methods

3. **Legacy Classes** (GlobalMS, PlayerMS) - Older implementations that may still be referenced but Player_MCON is the active implementation.

**Key Movement Features:**
- Physics-based movement using RigidBody3D
- Separate speeds for ground (maxSpeed), air (maxAirSpeed), and water (maxWaterSpeed)
- Friction simulation with drag values for ground and water
- Jump buffering and coyote time
- Dynamic parent switching for moving platforms

### World Management System

**Region/Room Loading** (`src/main/core/world/`):
- **GeometryLoader.cs** - Manages dynamic region visibility based on player position
  - Loads/unloads regions using Area3D triggers
  - Enables visibility culling for performance
  - Supports adjacent room visibility
- **RegionData.cs** - Defines individual regions with adjacent room references
  - Each RegionData node contains an Area3D trigger and a region Node3D
  - `AdjacentRooms` array defines neighboring regions to keep loaded

**TrenchBroom Integration:**
- Uses the TBLoader addon (`addons/tbloader/`) to import `.map` files from TrenchBroom
- Geometry is built using the "Build Meshes" button in the 3D view when TBLoader node is selected
- See `addons/tbloader/Usage.md` for detailed TrenchBroom setup

### Item/Inventory System
**ItemManager** (`src/main/core/player/ItemManager.cs`) - Simple flag-based inventory:
- Currently tracks: boots, blue key
- Each item has give/take/check methods
- Designed for Metroidvania-style progression gates

### Project Structure
```
src/
├── main/core/
│   ├── player/           # All player-related scripts
│   │   ├── MotionController.cs    # Abstract physics controller base
│   │   ├── Player_MCON.cs         # Active player implementation
│   │   ├── ItemManager.cs         # Inventory system
│   │   ├── CameraAH.cs            # Camera handling
│   │   └── FootstepsEngine.cs     # Audio footsteps
│   ├── world/            # World and level management
│   │   ├── GeometryLoader.cs      # Region streaming system
│   │   ├── RegionData.cs          # Region definitions
│   │   ├── AreaInteraction.cs     # Interactive world objects
│   │   └── SPAnimator.cs          # Animated platforms
│   └── inputOutput/      # File I/O utilities
│       └── JsonFileManage.cs
├── resources/
│   ├── scenes/           # Main game scenes
│   ├── prefabs/          # Reusable scene objects (crates, lockers, etc.)
│   └── config/           # Audio bus and configuration
Maps/                     # TrenchBroom .map files
HL_Textures/             # Half-Life style textures for TrenchBroom
addons/tbloader/         # TrenchBroom integration plugin
```

## Input Configuration
All inputs are defined in `project.godot`:
- **Movement**: WASD
- **Jump**: Space
- **Crouch**: Shift
- **Interact**: E
- **Debug Teleport**: F2
- **Mouse**: Left/Right click for actions

## Important Technical Details

### Physics Engine
- Uses GodotPhysics3D (not Jolt or other alternatives)
- Custom RigidBody3D-based character controller (not CharacterBody3D)
- Gravity scale dynamically adjusted (0 when in water/on slopes, 6 otherwise)

### Display Settings
- Base viewport: 640x576
- Window override: 768x691
- Scaling mode: viewport
- FSR scaling: 0.75 with sharpness 0.2
- VSync disabled for performance testing

### Naming Conventions
- Methods prefixed with their category:
  - `phy_` - Physics-related methods
  - `fun_` - Utility/helper functions
  - `ref_` - Reference getters
  - `int_` - Internal/abstract methods to implement
  - `util_` - Utility methods

## Current Development Status
See `TODO.txt` for current development tasks. Priority items:
- Document all code
- Fix scene tree structure
- Fix animated platforms not moving player correctly
- Fix region system (needs to use RegionData, not Node3D)
