using Godot;

public partial class PlayerController : MotionController {
    #region Exports
    [ExportGroup("Player Settings")]
    [Export] private float mouseSensitivity = 5.0f;
    [Export] private float jumpForce = 10.0f;
    [Export] private float coyoteTime = 0.2f;
    [Export] private float crouchScale = 0.65f;
    [Export] private float crouchSpeed = 7.6f;

    [ExportGroup("Player References")]
    [Export] private Node3D cameraHead;
    [Export] private Node3D cameraOrientation;
    [Export] private Node3D bodyMesh;
    [Export] private ShapeCast3D crouchCeilingCheck;
    [Export] private CollisionShape3D bodyCollision;
    [Export] private RichTextLabel fpsLabel;
    #endregion

    #region Input Action Names
    private const string INPUT_MOVE_FORWARD = "Move Forwards";
    private const string INPUT_MOVE_BACKWARD = "Move Backwards";
    private const string INPUT_MOVE_LEFT = "Move Left";
    private const string INPUT_MOVE_RIGHT = "Move Right";
    private const string INPUT_JUMP = "Jump";
    private const string INPUT_CROUCH = "Crouch";
    private const string INPUT_DEBUG_TP = "Debug TP Button";
    #endregion

    #region State
    private Vector2 inputDirection;
    private float cameraXRot;
    private float cameraYRot;
    private float timeSinceGrounded;
    private float timeSinceJumpPressed;
    private bool wantsToJump;
    private bool isCrouching;
    private bool wantsToUncrouch;
    private float originalBodyScaleY;
    private float originalMaxSpeed;
    private bool canJump => timeSinceGrounded < coyoteTime;
    public bool readyFlag { get; private set; }
    #endregion

    protected override void OnReady() {
        base.OnReady();

        if (bodyMesh != null) {
            originalBodyScaleY = bodyMesh.Scale.Y;
        }
        originalMaxSpeed = maxSpeed;

        readyFlag = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    #region Input Handling
    public override void _Input(InputEvent @event) {
        base._Input(@event);

        if (@event is InputEventMouseMotion mouseMotion) {
            HandleMouseLook(mouseMotion);
        }
    }

    private void HandleMouseLook(InputEventMouseMotion mouseMotion) {
        if (cameraHead == null || cameraOrientation == null) return;

        const float SENSITIVITY_MODIFIER = 0.001f;
        const float MAX_VERTICAL_ANGLE = 1.45f;

        cameraXRot -= mouseMotion.Relative.Y * SENSITIVITY_MODIFIER * mouseSensitivity;
        cameraYRot -= mouseMotion.Relative.X * SENSITIVITY_MODIFIER * mouseSensitivity;

        cameraXRot = Mathf.Clamp(cameraXRot, -MAX_VERTICAL_ANGLE, MAX_VERTICAL_ANGLE);

        cameraHead.GlobalRotation = new Vector3(cameraXRot, cameraYRot, 0.0f);
        cameraOrientation.GlobalRotation = new Vector3(0.0f, cameraYRot, 0.0f);
    }
    #endregion

    #region Motion Controller Implementation
    protected override Vector3 GetWishDirection() {
        if (cameraOrientation == null) return Vector3.Zero;

        // Get input each frame
        inputDirection = Input.GetVector(
            INPUT_MOVE_BACKWARD,
            INPUT_MOVE_FORWARD,
            INPUT_MOVE_LEFT,
            INPUT_MOVE_RIGHT
        );

        // Convert input to world space direction based on camera orientation
        Vector3 forward = -cameraOrientation.Transform.Basis.Z;
        Vector3 right = cameraOrientation.Transform.Basis.X;

        Vector3 wishDir = (forward * inputDirection.X + right * inputDirection.Y).Normalized();
        return wishDir;
    }

    protected override void OnPhysicsUpdate(Vector3 wishDirection) {
        base.OnPhysicsUpdate(wishDirection);

        UpdateJumpTimers();
        HandleJumpInput();
        HandleCrouchInput();
        HandleDebugInput();
        UpdateFPSDisplay();
        CheckForDeath();
    }
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
        if (inWater && Input.IsActionPressed(INPUT_JUMP)) {
            ApplyForce(rigidBody.Basis.Y * jumpForce * 1.5f);
            return;
        }

        // Jump button pressed
        if (Input.IsActionJustPressed(INPUT_JUMP)) {
            wantsToJump = true;
            timeSinceJumpPressed = 0;
        }

        // Jump button released
        if (Input.IsActionJustReleased(INPUT_JUMP)) {
            wantsToJump = false;
        }

        // Execute jump with coyote time
        if (wantsToJump && canJump && !inWater) {
            ExecuteJump();
        }
    }

    private void ExecuteJump() {
        ApplyImpulse(Vector3.Up * jumpForce);
        wantsToJump = false;
        timeSinceGrounded = coyoteTime; // Prevent double jumping
    }
    #endregion

    #region Crouch System
    private void HandleCrouchInput() {
        if (Input.IsActionJustPressed(INPUT_CROUCH) && !isCrouching) {
            StartCrouch();
        }

        if (Input.IsActionJustReleased(INPUT_CROUCH)) {
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

    #region Utility
    private void HandleDebugInput() {
        if (Input.IsActionJustPressed(INPUT_DEBUG_TP) && rigidBody != null) {
            rigidBody.GlobalPosition = new Vector3(0, 1, -16);
        }
    }

    private void UpdateFPSDisplay() {
        if (fpsLabel != null) {
            fpsLabel.Text = Engine.GetFramesPerSecond().ToString();
        }
    }

    private void CheckForDeath() {
        const float DEATH_Y_LEVEL = -64.0f;
        if (rigidBody != null && rigidBody.GlobalPosition.Y < DEATH_Y_LEVEL) {
            GetTree().ReloadCurrentScene();
        }
    }
    #endregion
}
