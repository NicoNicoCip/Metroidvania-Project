# QuakeMotionController System Guide

## Overview
The `QuakeMotionController` is a generic, reusable physics system inspired by Quake 1 movement mechanics. It can be applied to any RigidBody3D to give it Source-engine style movement with proper friction, acceleration, and state handling.

## Philosophy
Unlike the old `MotionController`, this system:
- **Doesn't assume specific scene hierarchy** - You just reference the nodes you need
- **Works with any entity** - Players, enemies, items, projectiles, platforms
- **Makes features optional** - Water, slopes, moving platforms are all opt-in
- **Separates concerns** - Movement physics is separate from input/AI/behavior

## Core Concept: Wish Direction
The entire system revolves around one abstract method:
```csharp
protected abstract Vector3 GetWishDirection();
```

This returns the direction the entity *wants* to move in world space. The Quake physics system then applies acceleration and friction to make that happen realistically.

## Basic Setup

### 1. Create Your Script
```csharp
using Godot;

public partial class MyEntity : QuakeMotionController {
    protected override Vector3 GetWishDirection() {
        // Return the direction you want to move
        return Vector3.Forward; // Simple example
    }
}
```

### 2. Scene Structure
Your scene needs:
- A **Node3D** (root) with your script attached
- A **RigidBody3D** child
- A **RayCast3D** child pointing down (for ground detection)

```
MyEntity (Node3D + QuakeMotionController script)
├── RigidBody3D
│   └── CollisionShape3D
└── GroundCast (RayCast3D pointing down)
```

### 3. Inspector Setup
In the Godot inspector, assign:
- **Required References**:
  - `rigidBody` → Your RigidBody3D node
  - `groundCast` → Your ground detection RayCast3D
- **Movement Settings**:
  - `maxSpeed` → Maximum ground speed (default: 10)
  - `maxAirSpeed` → Maximum air speed (default: 12)
  - `groundDrag` → Friction when on ground (default: 8)
  - `airDrag` → Friction in air (default: 0)

## Usage Examples

### Player Controller
```csharp
public partial class MyPlayer : QuakeMotionController {
    [Export] private Node3D cameraOrientation;
    
    protected override Vector3 GetWishDirection() {
        // Get WASD input
        Vector2 input = Input.GetVector("left", "right", "back", "forward");
        
        // Convert to world space based on camera
        Vector3 forward = -cameraOrientation.Transform.Basis.Z;
        Vector3 right = cameraOrientation.Transform.Basis.X;
        
        return (forward * input.X + right * input.Y).Normalized();
    }
    
    protected override void OnPhysicsUpdate(Vector3 wishDirection) {
        // Handle jumping
        if (Input.IsActionJustPressed("jump") && grounded) {
            ApplyImpulse(Vector3.Up * 10.0f);
        }
    }
}
```

### AI Enemy
```csharp
public partial class Enemy : QuakeMotionController {
    [Export] private Node3D player;
    
    protected override Vector3 GetWishDirection() {
        if (player == null) return Vector3.Zero;
        
        // Move toward player
        Vector3 toPlayer = player.GlobalPosition - rigidBody.GlobalPosition;
        toPlayer.Y = 0; // Horizontal only
        return toPlayer.Normalized();
    }
}
```

### Physics Item (Crate)
```csharp
public partial class Crate : QuakeMotionController {
    protected override Vector3 GetWishDirection() {
        // Items don't move on their own
        return Vector3.Zero;
    }
}
```

## Optional Features

### Slope Handling
Add a **RayCast3D** for slope detection and assign it in the inspector:
```
MyEntity
├── RigidBody3D
├── GroundCast (RayCast3D)
└── SlopeCast (RayCast3D, angled slightly forward)
```
- Assign to `slopeCast` in inspector
- Set `maxSlopeAngle` (default: 45 degrees)

The system will automatically project movement along slopes.

### Water Detection
Add an **Area3D** that detects water bodies:
```
MyEntity
├── RigidBody3D
├── GroundCast
└── WaterDetector (Area3D with collision layer for water)
```
- Assign to `waterDetectionArea` in inspector
- Override `OnEnteredWater()` and `OnExitedWater()` for custom behavior

When in water:
- Gravity is disabled
- Movement speed is halved
- Drag is applied

### Moving Platforms
Add a **ShapeCast3D** to detect platforms:
```
MyEntity
├── RigidBody3D
├── GroundCast
└── PlatformDetector (ShapeCast3D pointing down)
```
- Assign to `platformDetectionCast` in inspector
- The entity will automatically reparent to moving platforms

## Protected Members You Can Access

### State Variables
- `bool grounded` - Is the entity on the ground?
- `bool inWater` - Is the entity in water?
- `bool onSlope` - Is the entity on a slope?
- `float delta` - Current physics delta time

### Helper Methods
```csharp
protected void ApplyImpulse(Vector3 impulse)  // One-time force (jumping)
protected void ApplyForce(Vector3 force)       // Continuous force (swimming)
protected Vector3 GetVelocity()                // Get current velocity
protected void SetVelocity(Vector3 velocity)   // Set velocity directly
protected void ResetToInitialSettings()       // Reset to exported values
```

### Override Points
```csharp
protected virtual void OnReady()                       // Called after _Ready
protected virtual void OnPhysicsUpdate(Vector3 wish)   // Called each physics frame
protected virtual void OnEnteredWater()                // Called when entering water
protected virtual void OnExitedWater()                 // Called when exiting water
```

## Migration from Old MotionController

### Old System Issues
```csharp
// Old: Assumed specific hierarchy
body = (Node3D)GetChild(1).GetChild(1);  // Fragile!
head = (Node3D)GetChild(1).GetChild(0).GetChild(0).GetChild(0);  // Very fragile!

// Old: Coupled to input
inputDir = Input.GetVector(...);  // Can't be used for AI

// Old: Required manual binding
init_FullBind();  // Complex setup
```

### New System Benefits
```csharp
// New: Explicit references
[Export] protected RigidBody3D rigidBody;  // Clear!
[Export] private Node3D cameraHead;  // Flexible!

// New: Decoupled from input
protected abstract Vector3 GetWishDirection();  // Works for anything

// New: Automatic setup
// Just assign references in inspector, done!
```

## Tips and Best Practices

1. **Keep GetWishDirection() Simple**: Just return the direction you want to move. Don't apply forces here.

2. **Use OnPhysicsUpdate() for Actions**: Jumping, shooting, special abilities go here.

3. **Don't Fight the Physics**: The system is designed to feel like Quake. If you need different behavior, adjust the exported parameters rather than overriding physics methods.

4. **Optional Features are Truly Optional**: If you don't need water detection, just don't assign `waterDetectionArea`. No performance cost.

5. **Tune the Numbers**:
   - `maxSpeed`: How fast you move on ground
   - `maxAirSpeed`: How fast you can strafe in air (usually higher for air control)
   - `groundDrag`: How quickly you stop (higher = stop faster)
   - `airDrag`: Usually 0 for Quake-like air control

## Common Patterns

### Jump with Coyote Time
```csharp
private float timeSinceGrounded = 0;
private const float COYOTE_TIME = 0.2f;

protected override void OnPhysicsUpdate(Vector3 wish) {
    timeSinceGrounded = grounded ? 0 : timeSinceGrounded + delta;
    
    if (Input.IsActionJustPressed("jump") && timeSinceGrounded < COYOTE_TIME) {
        ApplyImpulse(Vector3.Up * jumpForce);
    }
}
```

### Speed Boost Powerup
```csharp
public void ActivateSpeedBoost(float duration) {
    maxSpeed *= 2.0f;
    GetTree().CreateTimer(duration).Timeout += () => ResetToInitialSettings();
}
```

### AI Jump to Reach Player
```csharp
protected override void OnPhysicsUpdate(Vector3 wish) {
    if (grounded && NeedsToJump()) {
        ApplyImpulse(Vector3.Up * jumpStrength);
    }
}

private bool NeedsToJump() {
    // Check if there's an obstacle ahead requiring a jump
    // Implementation depends on your game
    return false;
}
```

## Troubleshooting

**Entity slides too much**
- Increase `groundDrag`

**Entity feels sluggish**
- Increase `maxSpeed`
- Check that `maxAccel` is being calculated (it's `maxSpeed * 10` by default)

**Entity doesn't stop when no input**
- Make sure `GetWishDirection()` returns `Vector3.Zero` when idle
- Increase `groundDrag`

**Entity floats on slopes**
- Ensure `slopeCast` is assigned and positioned correctly
- Check `maxSlopeAngle` setting

**Moving platforms don't work**
- Ensure `platformDetectionCast` is assigned
- Check that platforms are Node3D (not StaticBody3D)
- Verify collision layers/masks
