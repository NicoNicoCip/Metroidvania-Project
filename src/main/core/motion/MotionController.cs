using Godot;
using System;

/// <summary>
/// Generic Quake 1-inspired physics controller for RigidBody3D.
/// Can be used for players, enemies, items, or any physics object that needs
/// Source-engine style movement with friction and acceleration.
/// </summary>
public abstract partial class MotionController : Node3D {
    #region Exported Settings
    [ExportGroup("Movement Settings")]
    [Export] protected float maxSpeed = 10.0f;
    [Export] protected float maxAirSpeed = 12.0f;
    [Export] protected float groundDrag = 8.0f;
    [Export] protected float airDrag = 0.0f;

    [ExportGroup("Physics Settings")]
    [Export] protected float gravityScale = 6.0f;
    [Export] protected float maxSlopeAngle = 45.0f;

    [ExportGroup("Required References")]
    [Export] protected RigidBody3D rigidBody;
    [Export] protected RayCast3D groundCast;
    #endregion

    #region Optional References
    [ExportGroup("Optional References")]
    [Export] protected RayCast3D slopeCast;
    [Export] protected ShapeCast3D contactCast;
    [Export] protected Area3D waterDetectionArea;
    #endregion

    #region Protected State
    protected float maxAccel;
    protected float currentDrag;
    protected bool grounded;
    protected bool inWater;
    protected bool onSlope;
    protected float delta;
    protected bool ignoreDragThisFrame = false;

    private Node initialParent;
    private InitialSettings cachedSettings;
    private PhysicsModifiers activeModifiers;
    #endregion

    #region Initialization
    public override void _Ready() {
        ValidateRequiredReferences();
        CacheInitialSettings();
        SetupOptionalFeatures();
        initialParent = GetParent();

        OnReady();
    }

    protected virtual void OnReady() { }

    private void ValidateRequiredReferences() {
        if (rigidBody == null) {
            GD.PushError($"QuakeMotionController: RigidBody3D reference is required on {Name}");
        }
        if (groundCast == null) {
            GD.PushError($"QuakeMotionController: GroundCast RayCast3D is required on {Name}");
        }
    }

    private void CacheInitialSettings() {
        cachedSettings = new InitialSettings {
            maxSpeed = this.maxSpeed,
            maxAirSpeed = this.maxAirSpeed,
            groundDrag = this.groundDrag,
            airDrag = this.airDrag,
            gravityScale = this.gravityScale
        };
        maxAccel = maxSpeed * 10.0f;
        activeModifiers = new PhysicsModifiers();
    }

    private void SetupOptionalFeatures() {
        if (waterDetectionArea != null) {
            waterDetectionArea.BodyEntered += OnWaterEntered;
            waterDetectionArea.BodyExited += OnWaterExited;
        }

        // Ensure RigidBody is set up correctly for motion control
        if (rigidBody != null) {
            // Disable built-in gravity - we'll handle it manually for better control
            rigidBody.GravityScale = 0.0f;
            rigidBody.LockRotation = true;  // Prevent physics from rotating the body
            rigidBody.ContinuousCd = true;  // Better collision detection for fast movement
        }
    }

    protected void ResetToInitialSettings() {
        maxSpeed = cachedSettings.maxSpeed;
        maxAirSpeed = cachedSettings.maxAirSpeed;
        groundDrag = cachedSettings.groundDrag;
        airDrag = cachedSettings.airDrag;
        gravityScale = cachedSettings.gravityScale;
        maxAccel = maxSpeed * 10.0f;
    }
    #endregion

    #region Physics Loop
    public override void _PhysicsProcess(double delta) {
        if (rigidBody == null) return;

        this.delta = (float)delta;

        UpdatePhysicsState();

        Vector3 wishDir = GetWishDirection();

        OnPhysicsUpdate(wishDir);

        ApplyModifications();
        ApplyDrag();
        ApplyMovement(wishDir);

        HandleMovingBodies();
    }

    /// <summary>
    /// Override this to provide custom physics behavior each frame.
    /// Called after physics state is updated but before movement is applied.
    /// </summary>
    protected virtual void OnPhysicsUpdate(Vector3 wishDirection) { }

    private void UpdatePhysicsState() {
        grounded = groundCast != null && groundCast.IsColliding();
        onSlope = CheckIfOnSlope();

        if (onSlope) {
            grounded = true;
        }

        // Check for surface physics modifiers
        if (grounded) {
            DetectSurfaceProperties();
        }
    }
    #endregion

    #region Abstract Methods - Must be implemented by subclasses
    /// <summary>
    /// Return the desired movement direction in world space.
    /// For players, this is calculated from input.
    /// For AI, this is calculated from pathfinding or behavior.
    /// For items, this might always be Vector3.Zero.
    /// </summary>
    protected abstract Vector3 GetWishDirection();
    #endregion

    #region Core Quake Physics
    private void ApplyMovement(Vector3 wishDir) {
        Vector3 velocity = rigidBody.LinearVelocity;
        Vector3 targetVel;

        if (onSlope && slopeCast != null) {
            wishDir = ProjectOnPlane(
                wishDir,
                slopeCast.GetCollisionNormal()
            ).Normalized();
        }

        if (grounded) {
            targetVel = UpdateVelocityGround(wishDir, velocity);
        } else if (inWater) {
            targetVel = UpdateVelocityWater(wishDir, velocity);
        } else {
            targetVel = UpdateVelocityAir(wishDir, velocity);
        }

        Vector3 velocityDelta = targetVel - velocity;

        rigidBody.ApplyCentralForce(velocityDelta / delta);
    }

    private Vector3 UpdateVelocityGround(Vector3 wishDir, Vector3 velocity) {
        float effectiveMaxSpeed = GetEffectiveMaxSpeed();
        float effectiveAccel = GetEffectiveAcceleration();

        float currentSpeed = velocity.Dot(wishDir);
        float addSpeed = Mathf.Clamp(effectiveMaxSpeed - currentSpeed, 0, effectiveAccel * delta);
        return velocity + addSpeed * wishDir;
    }

    private Vector3 UpdateVelocityAir(Vector3 wishDir, Vector3 velocity) {
        float effectiveAirSpeed = GetEffectiveAirSpeed();
        float effectiveAccel = GetEffectiveAcceleration();
        float effectiveGravity = GetEffectiveGravityScale();

        // Apply air acceleration
        float currentSpeed = velocity.Dot(wishDir);
        float addSpeed = Mathf.Clamp(effectiveAirSpeed - currentSpeed, 0, effectiveAccel * delta);
        velocity += addSpeed * wishDir;

        // Apply gravity manually (pulling down towards world origin)
        // Using ProjectSettings.GetSetting to get the global gravity vector
        float gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
        Vector3 gravityDir = (Vector3)ProjectSettings.GetSetting("physics/3d/default_gravity_vector");

        // Apply gravity with our scale
        velocity += gravityDir * gravity * effectiveGravity * delta;

        return velocity;
    }

    private Vector3 UpdateVelocityWater(Vector3 wishDir, Vector3 velocity) {
        float currentSpeed = velocity.Dot(wishDir);
        float waterSpeed = maxSpeed * 0.5f; // Water is half speed
        float addSpeed = Mathf.Clamp(waterSpeed - currentSpeed, 0, maxAccel * delta);
        return velocity + addSpeed * wishDir;
    }

    private void ApplyDrag() {
        if (ignoreDragThisFrame) {
            ignoreDragThisFrame = false;
            return;
        }

        if (grounded || onSlope) {
            currentDrag = GetEffectiveGroundDrag();
        } else if (inWater) {
            currentDrag = groundDrag * 0.5f; // Water drag
        } else {
            currentDrag = airDrag;
        }

        if (currentDrag > 0) {
            Vector3 velocity = rigidBody.LinearVelocity;
            ApplyFriction(ref velocity);
            rigidBody.LinearVelocity = velocity;
        }
    }

    private void ApplyFriction(ref Vector3 velocity) {
        float speed = velocity.Length();
        if (speed <= 0.00001f) return;

        float downLimit = Mathf.Max(speed, 0.5f);
        float dampAmount = speed - (downLimit * currentDrag * delta);

        if (dampAmount < 0) dampAmount = 0;

        velocity *= dampAmount / speed;
    }

    private void ApplyModifications() {

    }
    #endregion

    #region Slope Handling
    private bool CheckIfOnSlope() {
        if (slopeCast == null || !slopeCast.IsColliding()) {
            return false;
        }

        float angle = Mathf.RadToDeg(Vector3.Up.AngleTo(slopeCast.GetCollisionNormal()));
        return angle < maxSlopeAngle && angle > 0.1f;
    }

    private Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal) {
        float sqrMag = planeNormal.Dot(planeNormal);
        if (sqrMag < Mathf.Epsilon) {
            return vector;
        }

        float dot = vector.Dot(planeNormal);
        return new Vector3(
            vector.X - planeNormal.X * dot / sqrMag,
            vector.Y - planeNormal.Y * dot / sqrMag,
            vector.Z - planeNormal.Z * dot / sqrMag
        );
    }
    #endregion

    #region Water Handling
    private void OnWaterEntered(Node3D body) {
        inWater = true;
        OnEnteredWater();
    }

    private void OnWaterExited(Node3D body) {
        inWater = false;
        OnExitedWater();
    }

    protected virtual void OnEnteredWater() { }

    protected virtual void OnExitedWater() { }

    #endregion

    #region Moving Body Support
    private void HandleMovingBodies() {
        if (contactCast == null) return;
        Node3D contactCollider = null;
        if (contactCast.IsColliding())
            contactCollider = (Node3D)contactCast.GetCollider(0);

        if (contactCollider != null) {
            if (contactCast.IsColliding() && GetParent() == initialParent) {
                CallDeferred(nameof(DeferredReparent), contactCollider);
            } else if (contactCast.IsColliding()
              && contactCollider != GetParent()) {
                CallDeferred(nameof(DeferredReparent), initialParent);
            }
        }
    }

    private void DeferredReparent(Node newParent) {
        if (newParent != null && IsInstanceValid(newParent)) {
            Reparent(newParent, true);
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Apply an impulse to the RigidBody. Useful for jumping, knockback, etc.
    /// </summary>
    protected void ApplyImpulse(Vector3 impulse) {
        if (rigidBody != null) {
            rigidBody.ApplyCentralImpulse(impulse);
        }
    }

    /// <summary>
    /// Apply a continuous force to the RigidBody.
    /// </summary>
    protected void ApplyForce(Vector3 force) {
        if (rigidBody != null) {
            rigidBody.ApplyCentralForce(force);
        }
    }

    /// <summary>
    /// Get the current velocity of the RigidBody.
    /// </summary>
    protected Vector3 GetVelocity() {
        return rigidBody != null ? rigidBody.LinearVelocity : Vector3.Zero;
    }

    /// <summary>
    /// Set the velocity directly (use sparingly, prefer forces/impulses).
    /// </summary>
    protected void SetVelocity(Vector3 velocity) {
        if (rigidBody != null) {
            rigidBody.LinearVelocity = velocity;
        }
    }
    #endregion

    #region Physics Modifier API
    /// <summary>
    /// Set a temporary multiplier for ground drag/friction.
    /// Use this for surfaces like ice (lower) or mud (higher).
    /// Set to 1.0 to use default value.
    /// </summary>
    public void SetDragMultiplier(float multiplier) {
        activeModifiers.dragMultiplier = multiplier;
    }

    /// <summary>
    /// Set a temporary multiplier for maximum movement speed.
    /// Use this for speed boosts (higher) or slowdown effects (lower).
    /// Set to 1.0 to use default value.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier) {
        activeModifiers.speedMultiplier = multiplier;
    }

    /// <summary>
    /// Set a temporary multiplier for acceleration.
    /// Affects how quickly the entity reaches max speed.
    /// Set to 1.0 to use default value.
    /// </summary>
    public void SetAccelerationMultiplier(float multiplier) {
        activeModifiers.accelerationMultiplier = multiplier;
    }

    /// <summary>
    /// Set a temporary multiplier for gravity scale.
    /// Use this for low gravity zones (lower) or high gravity (higher).
    /// Set to 1.0 to use default value.
    /// </summary>
    public void SetGravityMultiplier(float multiplier) {
        activeModifiers.gravityMultiplier = multiplier;
    }

    /// <summary>
    /// Set an absolute override value for ground drag.
    /// This will completely replace the default drag value.
    /// Set to null to use default value with multipliers.
    /// </summary>
    public void SetDragOverride(float? dragValue) {
        activeModifiers.dragOverride = dragValue;
    }

    /// <summary>
    /// Set an absolute override value for maximum speed.
    /// This will completely replace the default max speed.
    /// Set to null to use default value with multipliers.
    /// </summary>
    public void SetSpeedOverride(float? speedValue) {
        activeModifiers.speedOverride = speedValue;
    }

    /// <summary>
    /// Reset all physics modifiers to default (multipliers to 1.0, overrides to null).
    /// </summary>
    public void ClearAllModifiers() {
        activeModifiers = new PhysicsModifiers();
    }

    /// <summary>
    /// Get the currently standing surface collider.
    /// Returns null if not grounded or no collider detected.
    /// </summary>
    public GodotObject GetGroundSurface() {
        if (!grounded || groundCast == null) return null;
        return groundCast.GetCollider();
    }

    /// <summary>
    /// Get metadata from the current ground surface.
    /// Useful for reading surface properties like friction tags.
    /// Returns Variant.Nil if surface has no such metadata.
    /// </summary>
    public Variant GetGroundSurfaceMetadata(string key) {
        var surface = GetGroundSurface();
        if (surface == null) return default;

        if (surface is Node node && node.HasMeta(key)) {
            return node.GetMeta(key);
        }

        return default;
    }

    /// <summary>
    /// Called when surface properties are detected.
    /// Override this to implement custom surface-based physics.
    /// </summary>
    protected virtual void OnSurfaceDetected(GodotObject surface) { }

    private void DetectSurfaceProperties() {
        var surface = GetGroundSurface();
        if (surface != null) {
            OnSurfaceDetected(surface);
        }
    }

    private float GetEffectiveGroundDrag() {
        if (activeModifiers.dragOverride.HasValue) {
            return activeModifiers.dragOverride.Value;
        }
        return groundDrag * activeModifiers.dragMultiplier;
    }

    private float GetEffectiveMaxSpeed() {
        if (activeModifiers.speedOverride.HasValue) {
            return activeModifiers.speedOverride.Value;
        }
        return maxSpeed * activeModifiers.speedMultiplier;
    }

    private float GetEffectiveAirSpeed() {
        return maxAirSpeed * activeModifiers.speedMultiplier;
    }

    private float GetEffectiveAcceleration() {
        return maxAccel * activeModifiers.accelerationMultiplier;
    }

    private float GetEffectiveGravityScale() {
        return gravityScale * activeModifiers.gravityMultiplier;
    }
    #endregion

    #region Data Structures
    public struct InitialSettings {
        public float maxSpeed;
        public float maxAirSpeed;
        public float groundDrag;
        public float airDrag;
        public float gravityScale;
    }

    public struct PhysicsModifiers {
        public float dragMultiplier;
        public float speedMultiplier;
        public float accelerationMultiplier;
        public float gravityMultiplier;
        public float? dragOverride;
        public float? speedOverride;

        public PhysicsModifiers() {
            dragMultiplier = 1.0f;
            speedMultiplier = 1.0f;
            accelerationMultiplier = 1.0f;
            gravityMultiplier = 1.0f;
            dragOverride = null;
            speedOverride = null;
        }
    }
    #endregion

    #region Getters
    public bool isGrounded() {
        return grounded;
    }

    public bool isInWater() {
        return inWater;
    }

    public bool isOnSlope() {
        return onSlope;
    }

    public InitialSettings getCachedSettigns() {
        return cachedSettings;
    }

    public PhysicsModifiers getActiveSettings() {
        return activeModifiers;
    }

    public RayCast3D getGroundCast() {
        return groundCast;
    }

    public RigidBody3D getRigidBody() {
        return rigidBody;
    }
    #endregion
}