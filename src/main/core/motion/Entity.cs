using System;
using Godot;

public abstract partial class Entity : MotionController {
    #region Exports
    [ExportGroup("Entity Settings")]
    [Export] protected float rotationSensitivity = 5.0f;
    [Export] protected float jumpForce = 10.0f;
    [Export] protected float coyoteTime = 0.2f;
    [Export] protected float crouchScale = 0.65f;
    [Export] protected float crouchSpeed = 7.6f;

    [ExportGroup("Entity References")]
    [Export] protected Node3D head;
    [Export] protected Node3D orientation;
    [Export] protected Node3D bodyMesh;
    [Export] protected ShapeCast3D crouchCeilingCheck;
    [Export] protected CollisionShape3D bodyCollision;
    #endregion

    #region State
    protected Vector3 tensors;
    protected float cameraXRot;
    protected float cameraYRot;
    protected float timeSinceGrounded;
    protected float timeSinceJumpPressed;
    protected bool wantsToJump;
    protected bool isCrouching;
    protected bool wantsToUncrouch;
    protected float originalBodyScaleY;
    protected float originalMaxSpeed;
    protected bool canJump => timeSinceGrounded < coyoteTime;
    public bool readyFlag { get; private set; }
    #endregion

    protected override void OnReady() {
        base.OnReady();

        if (bodyMesh != null) {
            originalBodyScaleY = bodyMesh.Scale.Y;
        }
        originalMaxSpeed = maxSpeed;

        readyFlag = true;
    }

    #region Rotation Handling
    protected void HandleRotationLook(InputEventMouseMotion mouseMotion) {
        if (head == null || orientation == null) return;

        const float SENSITIVITY_MODIFIER = 0.001f;
        const float MAX_VERTICAL_ANGLE = 1.45f;

        cameraXRot -= mouseMotion.Relative.Y * SENSITIVITY_MODIFIER * 
                    rotationSensitivity;
        cameraYRot -= mouseMotion.Relative.X * SENSITIVITY_MODIFIER * 
                    rotationSensitivity;

        cameraXRot = Mathf.Clamp(cameraXRot, -MAX_VERTICAL_ANGLE, MAX_VERTICAL_ANGLE);

        head.GlobalRotation = new Vector3(cameraXRot, cameraYRot, 0.0f);
        orientation.GlobalRotation = new Vector3(0.0f, cameraYRot, 0.0f);
    }
    #endregion

    #region Motion Controller Implementation
    protected override Vector3 GetWishDirection() {
        if (orientation == null) return Vector3.Zero;

        // Get input each frame
        SetTensors();

        // Convert input to world space direction based on camera orientation
        Vector3 forward = -orientation.Transform.Basis.Z;
        Vector3 right = orientation.Transform.Basis.X;

        Vector3 wishDir = (forward * tensors.X + right * tensors.Z)
            .Normalized();
        return wishDir;
    }

    protected override void OnPhysicsUpdate(Vector3 wishDirection) {
        base.OnPhysicsUpdate(wishDirection);

        UpdateJumpTimers();
        HandleJumpInput();
        HandleCrouchInput();
    }

    protected abstract void SetTensors();
    #endregion

    #region Jump System
    private void UpdateJumpTimers() {
        if (grounded) {
            timeSinceGrounded = 0;
        } else {
            timeSinceGrounded += delta;
        }

        if (wantsToJump) {
            timeSinceJumpPressed += delta;
        }
    }

    private void HandleJumpInput() {
        // Underwater jumping
        if (inWater && tensors.Y > 0) {
            ApplyForce(rigidBody.Basis.Y * jumpForce * 1.5f);
            return;
        }

        // Jump button pressed
        if (tensors.Y > 0) {
            wantsToJump = true;
            timeSinceJumpPressed = 0;
        }

        // Jump button released
        if (tensors.Y == 0) {
            wantsToJump = false;
        }

        // Execute jump with coyote time
        if (wantsToJump && canJump && !inWater) {
            ExecuteJump();
        }
    }

    private void ExecuteJump() {
        ApplyForce(Vector3.Up * jumpForce);
        wantsToJump = false;

        if(timeSinceGrounded == 0)
            wantsToJump = true;
        
        timeSinceGrounded = coyoteTime;
    }
    #endregion

    #region Crouch System
    private void HandleCrouchInput() {
        if (tensors.Y < 0 && !isCrouching) {
            StartCrouch();
        }

        if (tensors.Y == 0 && isCrouching) {
            wantsToUncrouch = true;
        }

        if (wantsToUncrouch && CanUncrouch()) {
            StopCrouch();
        }

        // Crouch swimming (move down in water)
        if (isCrouching && inWater) {
            ApplyForce(-rigidBody.Basis.Y * jumpForce);
        }
    }

    private void StartCrouch() {
        if (bodyMesh == null || bodyCollision == null) return;

        isCrouching = true;
        bodyMesh.Scale = new Vector3(bodyMesh.Scale.X, crouchScale, bodyMesh.Scale.Z);
        bodyCollision.Scale = bodyMesh.Scale;
        maxSpeed = crouchSpeed;
    }

    private void StopCrouch() {
        if (bodyMesh == null || bodyCollision == null) return;

        isCrouching = false;
        wantsToUncrouch = false;
        bodyMesh.Scale = new Vector3(bodyMesh.Scale.X, originalBodyScaleY, bodyMesh.Scale.Z);
        bodyCollision.Scale = bodyMesh.Scale;
        maxSpeed = originalMaxSpeed;

        // Push player down slightly to prevent floating
        if (grounded) {
            ApplyForce(Vector3.Down * jumpForce * 2);
        }
    }

    private bool CanUncrouch() {
        if (crouchCeilingCheck == null) return true;
        return !crouchCeilingCheck.IsColliding();
    }
    #endregion
}
