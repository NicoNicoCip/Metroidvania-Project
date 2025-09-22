using Godot;
using System;
using System.Collections.Generic;

public partial class GlobalMS : Node3D {
  // Core Movement Properties (Quake-style)
  [Export] protected float maxSpeed = 8.0f;           // Maximum ground movement speed
  [Export] private float maxWaterSpeed = 4.0f;        // Maximum water movement speed
  [Export] private float maxAirSpeed = 12.0f;         // Maximum air movement speed (air strafing)
  [Export] private float clFwrdSpd = 1.0f;            // Forward movement speed multiplier
  [Export] private float clSideSpd = 1.0f;            // Side movement speed multiplier
  [Export] protected float jumpForce = 15.0f;         // Jump impulse force
  [Export] private float groundDrag = 8.0f;           // Ground friction/drag coefficient
  [Export] private float waterDrag = 12.0f;           // Water resistance coefficient

  // External Force Reactivity
  [Export] private float externalForceMultiplier = 1.0f;   // Multiplier for all external forces
  [Export] private float externalForceDamping = 0.85f;     // How quickly external forces decay (0-1)
  [Export] private float impactThreshold = 5.0f;           // Minimum velocity difference for collision reaction
  [Export] private float maxExternalForce = 50.0f;         // Maximum allowed external force magnitude
  [Export] private float forceBlendRate = 0.3f;            // How much external forces affect movement (0-1)

  // Constants
  private const float VELOCITY_EPSILON = 0.00001f;         // Minimum velocity threshold
  private const float MIN_SPEED_THRESHOLD = 0.5f;          // Minimum speed for friction calculations
  private const float EXTERNAL_VELOCITY_CUTOFF = 0.1f;     // Cutoff for clearing small external velocities
  private const float EXTERNAL_FORCE_CUTOFF = 0.01f;       // Cutoff for ignoring small external forces
  private const float DEFAULT_JUMP_WAIT = 0.6f;            // Default jump cooldown time
  private const float GROUNDED_FORCE_REDUCTION = 0.5f;     // Force reduction when grounded
  private const float MOVING_FORCE_REDUCTION = 0.7f;       // Force reduction when actively moving
  private const float ACCEL_MULTIPLIER = 10.0f;            // Acceleration multiplier based on max speed
  private const float SLOPE_ANGLE_LIMIT = 45.0f;           // Maximum walkable slope angle
  private const float GRAVITY_SCALE_NORMAL = 6.0f;         // Normal gravity scale
  private const float IMPACT_FORCE_MULTIPLIER = 0.5f;      // Collision impact force multiplier
  private const float SWIM_FORCE_MULTIPLIER = 1.5f;        // Swimming force multiplier

  // Internal State
  protected float maxAccel;
  private float drag;
  private float wait0;
  public bool jumped;
  protected Vector3 dir;
  public Vector2 inputDir;
  private bool released;
  protected bool grounded;

  // External Force State
  private Vector3 externalVelocity;
  private List<ExternalForce> activeForces;
  private float lastImpactMagnitude;

  // Node References
  public RigidBody3D rig;
  protected Node3D orientation;
  protected Node3D head;
  protected Node3D body;
  public RayCast3D groundCast;
  private RayCast3D slopeCast;
  protected ShapeCast3D waterBoxCast;
  private ShapeCast3D contactCast;
  private Node initialParent;

  // Initial values storage
  private readonly float[] initial = new float[8];

  // External Force Structure
  public struct ExternalForce {
    public Vector3 force;
    public float duration;
    public float timeLeft;
    public bool persistent;
    public string source;
  }

  /// <summary>
  /// Gets current physics data snapshot for saving/loading
  /// </summary>
  protected Data get_Data() {
    return new Data() {
      _velocity = rig.LinearVelocity,
      _position = body.GlobalPosition,
      _groundDrag = groundDrag,
      _waterDrag = waterDrag,
      _jumpForce = jumpForce,
      _walkSpeed = maxSpeed,
      _waterSpeed = maxWaterSpeed,
      _externalVelocity = externalVelocity,
      _lastImpact = lastImpactMagnitude
    };
  }

  /// <summary>
  /// Applies physics data from save/load system
  /// </summary>
  protected void set_Data(Data data) {
    groundDrag = data._groundDrag;
    waterDrag = data._waterDrag;
    jumpForce = data._jumpForce;
    maxSpeed = data._walkSpeed;
    maxWaterSpeed = data._waterSpeed;
    externalVelocity = data._externalVelocity;
  }

  /// <summary>
  /// Resets all physics parameters to their initial exported values
  /// </summary>
  protected void set_defaults() {
    var initialValues = initial;
    maxSpeed = initialValues[0];
    maxWaterSpeed = initialValues[1];
    maxAirSpeed = initialValues[2];
    maxAccel = maxSpeed * ACCEL_MULTIPLIER;
    clFwrdSpd = initialValues[3];
    clSideSpd = initialValues[4];
    jumpForce = initialValues[5];
    groundDrag = initialValues[6];
    waterDrag = initialValues[7];
  }

  /// <summary>
  /// Initializes all node references and physics systems
  /// Call this in your player's _Ready() method
  /// </summary>
  protected void init_FullBind() {
    // Cache initial values for reset functionality
    var initialValues = initial;
    initialValues[0] = maxSpeed;
    initialValues[1] = maxWaterSpeed;
    initialValues[2] = maxAirSpeed;
    initialValues[3] = clFwrdSpd;
    initialValues[4] = clSideSpd;
    initialValues[5] = jumpForce;
    initialValues[6] = groundDrag;
    initialValues[7] = waterDrag;

    // Initialize node references using cached path lookups
    var childCache = GetChild(1);
    rig = (RigidBody3D)childCache.GetChild(2);
    orientation = (Node3D)rig.GetChild(2);
    head = (Node3D)childCache.GetChild(0).GetChild(0).GetChild(0);
    body = (Node3D)childCache.GetChild(1);

    var rigChildren = rig.GetChildren();
    groundCast = (RayCast3D)rigChildren[3];
    slopeCast = (RayCast3D)rigChildren[4];
    waterBoxCast = (ShapeCast3D)rigChildren[6];
    contactCast = (ShapeCast3D)rigChildren[7];

    initialParent = GetParent();

    // Initialize external force system
    activeForces = new List<ExternalForce>();
    externalVelocity = Vector3.Zero;

    // Connect collision detection for external force reactions
    rig.BodyEntered += fun_OnBodyEntered;
  }

  /// <summary>
  /// Calculates drag and gravity scaling based on current environment
  /// </summary>
  protected void fun_DragCalculations() {
    bool inWater = waterBoxCast.IsColliding();
    bool onSlope = phy_Sloped();

    // Set drag based on environment priority: slope -> ground -> water -> air
    drag = (grounded || onSlope) ? groundDrag :
           inWater ? waterDrag : 0;

    // Disable gravity in water or on slopes for better movement control
    rig.GravityScale = (inWater || onSlope) ? 0 : GRAVITY_SCALE_NORMAL;
  }

  /// <summary>
  /// Main velocity update - combines Quake physics with external force reactions
  /// </summary>
  protected void fun_UpdateVelocity(double delta) {
    // Handle moving platform attachment/detachment
    bool hasContact = contactCast.IsColliding();
    bool isOnInitialParent = GetParent() == initialParent;

    if (hasContact && isOnInitialParent)
      Reparent((Node3D)contactCast.GetCollider(0), true);
    else if (!hasContact && !isOnInitialParent)
      Reparent(initialParent, true);

    // Update external force system
    fun_ProcessExternalForces(delta);

    // Calculate base Quake movement
    Vector3 quakeVelocity = CalculateQuakeMovement(delta);

    // Apply external forces and set final velocity
    rig.LinearVelocity = fun_BlendWithExternalForces(quakeVelocity);
  }

  /// <summary>
  /// Determines appropriate movement calculation based on current environment
  /// </summary>
  private Vector3 CalculateQuakeMovement(double delta) {
    var currentVelocity = rig.LinearVelocity;
    bool inWater = waterBoxCast.IsColliding();

    if ((grounded || phy_Sloped()) && !inWater)
      return phy_UpdateVelGround(dir, currentVelocity, delta);
    else if (!inWater)
      return phy_UpdateVelAir(dir, currentVelocity, delta);
    else
      return phy_UpdateVelWater(dir, currentVelocity, delta);
  }

  /// <summary>
  /// Updates external force timers and applies decay
  /// </summary>
  private void fun_ProcessExternalForces(double delta) {
    float deltaTime = (float)delta;

    // Remove expired forces (reverse iteration for safe removal)
    for (int i = activeForces.Count - 1; i >= 0; i--) {
      var force = activeForces[i];
      force.timeLeft -= deltaTime;

      if (force.timeLeft <= 0 && !force.persistent)
        activeForces.RemoveAt(i);
      else
        activeForces[i] = force;
    }

    // Apply exponential decay to external velocity
    externalVelocity *= Mathf.Pow(externalForceDamping, deltaTime);

    // Clear negligible velocities to prevent jitter
    if (externalVelocity.Length() < EXTERNAL_VELOCITY_CUTOFF)
      externalVelocity = Vector3.Zero;
  }

  /// <summary>
  /// Blends Quake movement with external forces based on player state
  /// </summary>
  private Vector3 fun_BlendWithExternalForces(Vector3 quakeVel) {
    if (externalVelocity.Length() < EXTERNAL_FORCE_CUTOFF)
      return quakeVel;

    // Calculate blend factor based on player control state
    float blendFactor = grounded ? forceBlendRate * GROUNDED_FORCE_REDUCTION : forceBlendRate;

    // Reduce external force influence when player is actively inputting movement
    if (inputDir.Length() > EXTERNAL_FORCE_CUTOFF)
      blendFactor *= MOVING_FORCE_REDUCTION;

    return quakeVel + externalVelocity * blendFactor;
  }

  /// <summary>
  /// Applies external force that decays over time (for wind, pushers, etc.)
  /// </summary>
  public void fun_ApplyExternalForce(Vector3 force, float duration = 0.5f, string source = "unknown") {
    // Clamp force to prevent excessive effects
    if (force.Length() > maxExternalForce)
      force = force.Normalized() * maxExternalForce;

    externalVelocity += force * externalForceMultiplier;

    activeForces.Add(new ExternalForce {
      force = force,
      duration = duration,
      timeLeft = duration,
      persistent = false,
      source = source
    });
  }

  /// <summary>
  /// Applies instant impulse force (for explosions, collisions, etc.)
  /// </summary>
  public void fun_ApplyImpulse(Vector3 impulse, string source = "collision") {
    Vector3 velocityChange = impulse * externalForceMultiplier;

    // Clamp impulse magnitude
    if (velocityChange.Length() > maxExternalForce)
      velocityChange = velocityChange.Normalized() * maxExternalForce;

    externalVelocity += velocityChange;
    lastImpactMagnitude = impulse.Length();
  }

  /// <summary>
  /// Collision detection callback - automatically applies reaction forces
  /// </summary>
  private void fun_OnBodyEntered(Node body) {
    if (body is RigidBody3D otherRig) {
      Vector3 relativeVelocity = rig.LinearVelocity - otherRig.LinearVelocity;
      float impactSpeed = relativeVelocity.Length();

      if (impactSpeed > impactThreshold) {
        Vector3 impactDirection = (rig.GlobalPosition - otherRig.GlobalPosition).Normalized();
        Vector3 impactForce = impactDirection * impactSpeed * IMPACT_FORCE_MULTIPLIER;
        fun_ApplyImpulse(impactForce, "collision");
      }
    }
  }

  /// <summary>
  /// Jump with boolean input (for custom input systems)
  /// </summary>
  protected void phy_Jump(bool act, float wait, double delta) {
    float waitTime = wait == -1 ? DEFAULT_JUMP_WAIT : wait;
    bool inWater = waterBoxCast.IsColliding();

    if (act && !jumped && !inWater && grounded) {
      rig.ApplyCentralImpulse(jumpForce * Vector3.Up);
      jumped = true;
    } else if (act && inWater) {
      rig.ApplyCentralForce(rig.Basis.Y * jumpForce * SWIM_FORCE_MULTIPLIER);
    }

    HandleJumpCooldown(act, waitTime, delta);
  }

  /// <summary>
  /// Jump with input action string (standard Godot input)
  /// </summary>
  protected void phy_Jump(string act, float wait, double delta) {
    float waitTime = wait == -1 ? DEFAULT_JUMP_WAIT : wait;
    bool actionPressed = Input.IsActionPressed(act);
    bool inWater = waterBoxCast.IsColliding();

    if (actionPressed && !jumped && !inWater && grounded) {
      rig.ApplyCentralImpulse(jumpForce * Vector3.Up);
      jumped = true;
    } else if (actionPressed && inWater) {
      rig.ApplyCentralForce(rig.Basis.Y * jumpForce * SWIM_FORCE_MULTIPLIER);
    }

    if (Input.IsActionJustReleased(act)) {
      wait0 = 0;
      jumped = false;
    }

    HandleJumpCooldown(actionPressed, waitTime, delta);
  }

  /// <summary>
  /// Handles jump cooldown timing for both jump methods
  /// </summary>
  private void HandleJumpCooldown(bool isActive, float waitTime, double delta) {
    if (!isActive && released) {
      wait0 = 0;
      jumped = false;
    }

    if (jumped) {
      wait0 += (float)delta;
      if (wait0 > waitTime && grounded) {
        jumped = false;
        wait0 = 0;
      }
    }

    released = isActive;
    rig.ForceUpdateTransform();
  }

  /// <summary>
  /// Quake-style friction calculation with velocity threshold optimization
  /// </summary>
  private void fun_CalculateFrinction(ref Vector3 vel, float delta) {
    float speed = vel.Length();
    if (speed <= VELOCITY_EPSILON) return;

    float downLimit = Mathf.Max(speed, MIN_SPEED_THRESHOLD);
    float dampAmount = speed - (downLimit * drag * delta);

    if (dampAmount <= 0) {
      vel = Vector3.Zero;
    } else {
      vel *= dampAmount / speed;
    }
  }

  /// <summary>
  /// Calculates desired movement direction based on input and orientation
  /// </summary>
  protected Vector3 fun_CalculateWishDir() {
    var basis = orientation.Transform.Basis;
    return (-basis.Z * inputDir.X * clFwrdSpd + basis.X * inputDir.Y * clSideSpd).Normalized();
  }

  // Quake movement physics methods
  private Vector3 phy_UpdateVelGround(Vector3 wishDir, Vector3 vel, double delta) {
    fun_CalculateFrinction(ref vel, (float)delta);

    float currentSpeed = vel.Dot(wishDir);
    float addSpeed = Mathf.Max(0, maxSpeed - currentSpeed);
    float accelSpeed = Mathf.Min(addSpeed, maxAccel * (float)delta);

    return vel + accelSpeed * wishDir;
  }

  private Vector3 phy_UpdateVelAir(Vector3 wishDir, Vector3 vel, double delta) {
    float currentSpeed = vel.Dot(wishDir);
    float addSpeed = Mathf.Max(0, maxAirSpeed - currentSpeed);
    float accelSpeed = Mathf.Min(addSpeed, maxAccel * (float)delta);

    return vel + accelSpeed * wishDir;
  }

  private Vector3 phy_UpdateVelWater(Vector3 wishDir, Vector3 vel, double delta) {
    fun_CalculateFrinction(ref vel, (float)delta);

    float currentSpeed = vel.Dot(wishDir);
    float addSpeed = Mathf.Max(0, maxWaterSpeed - currentSpeed);
    float accelSpeed = Mathf.Min(addSpeed, maxAccel * (float)delta);

    return vel + accelSpeed * wishDir;
  }

  /// <summary>
  /// Updates grounded state via groundcast raycast
  /// </summary>
  protected void phy_Grounded() {
    grounded = groundCast.IsColliding();
  }

  /// <summary>
  /// Checks if player is on a walkable slope
  /// </summary>
  protected bool phy_Sloped() {
    if (!slopeCast.IsColliding()) return false;

    float angle = Mathf.RadToDeg(Vector3.Up.AngleTo(slopeCast.GetCollisionNormal()));
    return angle > 0.1f && angle < SLOPE_ANGLE_LIMIT;
  }

  /// <summary>
  /// Projects movement direction onto slope surface
  /// </summary>
  protected Vector3 phy_GetSlopeDir() {
    return phy_ProjectOnPlane(fun_CalculateWishDir(), slopeCast.GetCollisionNormal()).Normalized();
  }

  /// <summary>
  /// Projects vector onto plane defined by normal (used for slope movement)
  /// </summary>
  private Vector3 phy_ProjectOnPlane(Vector3 vector, Vector3 planeNormal) {
    float normalLengthSq = planeNormal.Dot(planeNormal);
    if (normalLengthSq < Mathf.Epsilon) return vector;

    float projection = vector.Dot(planeNormal) / normalLengthSq;
    return vector - planeNormal * projection;
  }

  // Data structure for save/load system
  public struct Data {
    public Vector3 _position;
    public Vector3 _velocity;
    public float _groundDrag;
    public float _waterDrag;
    public float _jumpForce;
    public float _waterSpeed;
    public float _walkSpeed;
    public Vector3 _externalVelocity;
    public float _lastImpact;
  }
}