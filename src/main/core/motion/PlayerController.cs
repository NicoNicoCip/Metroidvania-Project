using System;
using Godot;

public partial class PlayerController : Entity {
    #region Exports
    [ExportGroup("Player References")]
    [Export] private RichTextLabel fpsLabel;
    [Export] private float dashForce = 15.0f;
    #endregion

    #region Input Action Names
    private const string INPUT_MOVE_FORWARD = "Move Forwards";
    private const string INPUT_MOVE_BACKWARD = "Move Backwards";
    private const string INPUT_MOVE_LEFT = "Move Left";
    private const string INPUT_MOVE_RIGHT = "Move Right";
    private const string INPUT_JUMP = "Jump";
    private const string INPUT_CROUCH = "Crouch";
    private const string INPUT_DEBUG_TP = "Debug TP Button";
    private const string INPUT_DEBUG_DASH = "Shift";
    #endregion

    protected override void OnReady() {
        base.OnReady();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void Debug(Vector3 wishDir) {
        string debutOut = "";

        debutOut += "Position: " + rigidBody.GlobalPosition.ToString() + "\n";
        debutOut += "Vector: " + rigidBody.LinearVelocity.ToString() + "\n";
        debutOut += "Wants To jump: " + wantsToJump + "\n";
        debutOut += "Can To jump: " + canJump + "\n";
        debutOut += "Wish dir: " + wishDir + "\n";

        GD.Print(debutOut + new string('=', 16));
    }

    #region Input Handling
    public override void _Input(InputEvent @event) {
        base._Input(@event);

        if (@event is InputEventMouseMotion mouseMotion) {
            HandleRotationLook(mouseMotion);
        }
    }
    #endregion

    #region Motion Controller Implementation
    protected override void SetTensors() {
        Vector2 inputMap = Input.GetVector(
            INPUT_MOVE_BACKWARD,
            INPUT_MOVE_FORWARD,
            INPUT_MOVE_LEFT,
            INPUT_MOVE_RIGHT
        );


        float verticalMap = 0.0f;
        
        if (Input.IsActionPressed(INPUT_CROUCH))
            verticalMap = -1.0f;
        else if (Input.IsActionPressed(INPUT_JUMP))
            verticalMap = 1.0f;

        tensors = new Vector3(inputMap.X, verticalMap, inputMap.Y);
    }

    protected override void OnPhysicsUpdate(Vector3 wishDirection) {
        base.OnPhysicsUpdate(wishDirection);

        HandleDebugInput();
        UpdateFPSDisplay();
        CheckForDeath();
        UseDash(wishDirection);
        // Debug(wishDirection);
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

    private bool canDash = true;

    private void UseDash(Vector3 wishDir) {
        
        if (Input.IsActionJustPressed(INPUT_DEBUG_DASH) 
            && !grounded && canDash) {
            canDash = false;
            
            Vector3 dashDir = wishDir.Length() > 0.01f ? wishDir.Normalized() 
            : -orientation.Transform.Basis.Z;
            
            ApplyImpulse(dashDir * dashForce);
        }

        if (!canDash && grounded) {
            canDash = true;
        }
    }
    #endregion

    #region getters
    public RigidBody3D getRigidBody() {
        return rigidBody;
    }
    #endregion
}
