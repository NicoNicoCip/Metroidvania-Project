# Physics Modifier API Guide

## Overview
The MotionController includes a powerful API for dynamically modifying physics properties based on surfaces, game states, or any other conditions. This allows you to create:
- **Ice surfaces** that reduce friction
- **Mud/quicksand** that slows movement
- **Speed boost pads** 
- **Conveyor belts**
- **Low/high gravity zones**
- **Bounce pads**
- And much more!

## Core Concepts

### Multipliers vs Overrides

**Multipliers** are applied to the base values:
- `SetDragMultiplier(2.0f)` → Double the friction
- `SetSpeedMultiplier(0.5f)` → Half speed
- Values multiply with the exported base values

**Overrides** replace the base values completely:
- `SetDragOverride(15.0f)` → Friction is exactly 15, ignoring base value
- `SetSpeedOverride(20.0f)` → Max speed is exactly 20, ignoring base value

### Surface Detection

The system automatically calls `OnSurfaceDetected()` when grounded, passing the surface collider. You can:
1. Read metadata from surfaces
2. Check surface types
3. Apply physics modifiers based on what you find

## API Reference

### Modifier Methods

```csharp
// Set multipliers (default: 1.0)
void SetDragMultiplier(float multiplier)
void SetSpeedMultiplier(float multiplier)
void SetAccelerationMultiplier(float multiplier)
void SetGravityMultiplier(float multiplier)

// Set absolute overrides (default: null)
void SetDragOverride(float? dragValue)
void SetSpeedOverride(float? speedValue)

// Reset everything to defaults
void ClearAllModifiers()
```

### Surface Query Methods

```csharp
// Get the current ground surface collider
GodotObject GetGroundSurface()

// Get metadata from the current surface
Variant GetGroundSurfaceMetadata(string key)
```

### Override Hook

```csharp
// Called every physics frame when grounded
protected virtual void OnSurfaceDetected(GodotObject surface)
```

## Usage Examples

### Example 1: Ice Surface

**Setup the surface:**
```csharp
public partial class IcePlatform : StaticBody3D {
    public override void _Ready() {
        SetMeta("friction", 0.1f);  // Very low friction
        SetMeta("surface_type", "ice");
    }
}
```

**React to it in your entity:**
```csharp
protected override void OnSurfaceDetected(GodotObject surface) {
    ClearAllModifiers();  // Reset first
    
    if (surface is Node node && node.HasMeta("friction")) {
        float friction = (float)node.GetMeta("friction");
        
        if (friction < 0.3f) {
            SetDragMultiplier(0.2f);  // Very slippery!
        }
    }
}
```

### Example 2: Speed Boost Zone

**Using Area3D for zones:**
```csharp
public partial class SpeedZone : Area3D {
    [Export] private float speedBoost = 2.0f;
    
    public override void _Ready() {
        BodyEntered += OnEntered;
        BodyExited += OnExited;
    }
    
    private void OnEntered(Node3D body) {
        if (body.GetParent() is MotionController controller) {
            controller.SetSpeedMultiplier(speedBoost);
        }
    }
    
    private void OnExited(Node3D body) {
        if (body.GetParent() is MotionController controller) {
            controller.SetSpeedMultiplier(1.0f);  // Reset
        }
    }
}
```

### Example 3: Conveyor Belt

**Surface that pushes entities:**
```csharp
public partial class ConveyorBelt : StaticBody3D {
    [Export] private Vector3 direction = Vector3.Forward;
    [Export] private float speed = 5.0f;
    
    public override void _Ready() {
        SetMeta("conveyor_direction", direction);
        SetMeta("conveyor_speed", speed);
    }
}
```

**Entity reacts:**
```csharp
protected override void OnSurfaceDetected(GodotObject surface) {
    if (surface is Node node) {
        if (node.HasMeta("conveyor_direction") && node.HasMeta("conveyor_speed")) {
            Vector3 dir = (Vector3)node.GetMeta("conveyor_direction");
            float speed = (float)node.GetMeta("conveyor_speed");
            ApplyForce(dir.Normalized() * speed);
        }
    }
}
```

### Example 4: Bounce Pad

**Instant impulse on contact:**
```csharp
public partial class BouncePad : StaticBody3D {
    [Export] private float bounceForce = 20.0f;
    
    public override void _Ready() {
        SetMeta("bounce_force", bounceForce);
    }
}
```

**Entity bounces:**
```csharp
protected override void OnSurfaceDetected(GodotObject surface) {
    if (surface is Node node && node.HasMeta("bounce_force")) {
        float force = (float)node.GetMeta("bounce_force");
        ApplyImpulse(Vector3.Up * force);
    }
}
```

### Example 5: Conditional Physics (Powerups)

**Temporary speed boost item:**
```csharp
public void CollectSpeedBoost() {
    SetSpeedMultiplier(2.0f);
    
    // Reset after 5 seconds
    GetTree().CreateTimer(5.0f).Timeout += () => {
        SetSpeedMultiplier(1.0f);
    };
}
```

### Example 6: Progressive Slowdown (Quicksand)

**Area that progressively slows:**
```csharp
public partial class Quicksand : Area3D {
    [Export] private float slowdownRate = 0.1f;
    private MotionController affectedController;
    private float currentSlowdown = 1.0f;
    
    public override void _Ready() {
        BodyEntered += OnEntered;
        BodyExited += OnExited;
    }
    
    private void OnEntered(Node3D body) {
        if (body.GetParent() is MotionController controller) {
            affectedController = controller;
            currentSlowdown = 1.0f;
        }
    }
    
    public override void _PhysicsProcess(double delta) {
        if (affectedController != null) {
            currentSlowdown = Mathf.Max(0.2f, currentSlowdown - slowdownRate * (float)delta);
            affectedController.SetSpeedMultiplier(currentSlowdown);
            affectedController.SetDragMultiplier(3.0f);  // Hard to move
        }
    }
    
    private void OnExited(Node3D body) {
        if (affectedController != null) {
            affectedController.ClearAllModifiers();
            affectedController = null;
        }
    }
}
```

## Common Patterns

### Pattern 1: Multiplicative Effects

Stack multiple effects by using multipliers:
```csharp
SetSpeedMultiplier(2.0f);  // Speed boost powerup
// On ice surface: drag is 0.2x
// Result: Fast movement with low control!
```

### Pattern 2: Override for Absolute Control

When you need exact values regardless of base stats:
```csharp
// Force entity to move at exactly 5 units/sec
SetSpeedOverride(5.0f);
```

### Pattern 3: State-Based Physics

```csharp
private bool isExhausted = false;

protected override void OnPhysicsUpdate(Vector3 wish) {
    if (isExhausted) {
        SetSpeedMultiplier(0.5f);
        SetAccelerationMultiplier(0.3f);
    } else {
        ClearAllModifiers();
    }
}
```

### Pattern 4: Surface Type System

```csharp
protected override void OnSurfaceDetected(GodotObject surface) {
    ClearAllModifiers();
    
    if (surface is Node node && node.HasMeta("surface_type")) {
        string surfaceType = (string)node.GetMeta("surface_type");
        
        switch (surfaceType) {
            case "ice":
                SetDragMultiplier(0.2f);
                break;
            case "mud":
                SetDragMultiplier(2.0f);
                SetSpeedMultiplier(0.7f);
                break;
            case "sand":
                SetDragMultiplier(1.3f);
                break;
            case "metal":
                SetDragMultiplier(0.5f);
                break;
        }
    }
}
```

### Pattern 5: Temporary Status Effects

```csharp
public void ApplySlowEffect(float duration, float intensity) {
    SetSpeedMultiplier(1.0f - intensity);
    
    GetTree().CreateTimer(duration).Timeout += () => {
        SetSpeedMultiplier(1.0f);
    };
}

public void ApplySpeedBoost(float duration, float multiplier) {
    SetSpeedMultiplier(multiplier);
    SetAccelerationMultiplier(multiplier * 0.8f);  // Slightly less responsive
    
    GetTree().CreateTimer(duration).Timeout += () => {
        ClearAllModifiers();
    };
}
```

## Best Practices

1. **Always call `ClearAllModifiers()` first** when detecting new surfaces to avoid stacking unintended effects.

2. **Use multipliers for most cases** - They're more flexible and work with different entity base stats.

3. **Use overrides for forced mechanics** - Like cutscenes or special events where you need exact control.

4. **Check `IsInstanceValid()` in timers** - Entities might be freed before timers complete.

5. **Combine with visual/audio feedback** - Let players know why physics changed (ice sparkles, mud sounds, etc.).

6. **Test with different base stats** - Make sure surfaces work well for players AND enemies.

## Metadata Naming Conventions

Suggested metadata keys for consistency:

| Key | Type | Purpose |
|-----|------|---------|
| `friction` | float | Surface friction (0.1 = ice, 2.0 = mud) |
| `surface_type` | string | Named type ("ice", "mud", "metal") |
| `speed_multiplier` | float | Speed modifier zones |
| `bounce_force` | float | Bounce pad strength |
| `conveyor_direction` | Vector3 | Direction of conveyor |
| `conveyor_speed` | float | Speed of conveyor |
| `gravity_multiplier` | float | Gravity zone modifier |
| `damage_per_second` | float | Hazardous surfaces |

## Performance Notes

- Surface detection happens only when grounded
- Modifiers are applied as multiplications (fast)
- No allocations per frame
- Safe to use on hundreds of entities simultaneously

## Troubleshooting

**Modifiers not applying:**
- Check that surface has metadata set in `_Ready()`
- Verify `OnSurfaceDetected()` is being called
- Make sure you're not calling `ClearAllModifiers()` after setting

**Effects stacking unexpectedly:**
- Always `ClearAllModifiers()` at the start of `OnSurfaceDetected()`
- Check for multiple Area3D overlaps

**Speed feels wrong:**
- Remember multipliers stack with base values
- Try using overrides for exact control
- Check if multiple systems are modifying the same entity
