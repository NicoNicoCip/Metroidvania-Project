using Godot;

public partial class PlayerMS : GlobalMS {
	[Export] float sensetivity = 5.0f;
	[Export] RichTextLabel fpsLabel;

	ShapeCast3D crouchBoxCast;
	public bool readyFlag = false;
	float Xrot;
	float Yrot;
	float startYScale;
	bool releaseCrouch;

	const string moveBackwardsInput = "Move Backwards";
	const string moveForwardsInput = "Move Forwards";
	const string moveLeftInput = "Move Left";
	const string moveRightInput = "Move Right";
	const string jumpInput = "Jump";
	const string debugTpInput = "Debug TP Button";
	const string crouchInput = "Crouch";

	public override void _Ready() {
		fun_Bind();
		set_defaults();
		startYScale = rig.Scale.Y;
		readyFlag = true;
	}

	public override void _PhysicsProcess(double delta) {
		base._PhysicsProcess(delta);

		fpsLabel.Text = Engine.GetFramesPerSecond().ToString();
        
		phy_Grounded();
		inputDir = Input.GetVector(
			moveBackwardsInput,
			moveForwardsInput,
			moveLeftInput,
			moveRightInput
		);

		if (Input.IsActionJustPressed(debugTpInput)) {
			rig.GlobalPosition = new(0, 1, -16);
		}

		maxAccel = maxSpeed * 10;

		if (!phy_Sloped()) {
			dir = fun_CalculateWishDir();
		} else {
			dir = phy_GetSlopeDir();
			grounded = true;
		}

		fun_DragCalculations();
		fun_UpdateVelocity(delta);

		if (phy_Sloped()) {
			int defaultWait = -1;
			phy_Jump(jumpInput, defaultWait, delta);
		} else {
			float specialWait = 0.2f;
			phy_Jump(jumpInput, specialWait, delta);
		}

		phy_Crouch(delta);


		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Process(double delta) {
		base._Process(delta);
		int maxYLevelBeforeDeath = -64;
		if (rig.GlobalPosition.Y <= maxYLevelBeforeDeath) {
			fun_KillMe();
		}
	}

	public override void _Input(InputEvent @event) {
		base._Input(@event);
		const float sensitivityModifier = 0.001f;
		const float maxVerticalClamp = 1.45f;
		if (@event is InputEventMouseMotion mouseMotion) {
			Xrot -= mouseMotion.Relative.Y * sensitivityModifier * sensetivity;
			Yrot -= mouseMotion.Relative.X * sensitivityModifier * sensetivity;

			Xrot = Mathf.Clamp(Xrot, -maxVerticalClamp, maxVerticalClamp);
			head.GlobalRotation = new Vector3(Xrot, Yrot, 0.0f);

			orientation.GlobalRotation = new Vector3(0.0f, Yrot, 0.0f);
		}
	}

	void fun_Bind() {
		crouchBoxCast = (ShapeCast3D)GetChild(1).GetChild(2).GetChild(5);
		init_FullBind();
	}

	void phy_Crouch(double delta) {
		if (!waterBoxCast.IsColliding()) {
			if (Input.IsActionJustPressed(crouchInput)) {
				const float newYscale = 0.65f;
				body.Scale = new Vector3(body.Scale.X, newYscale, body.Scale.Z);
				rig.GetChild<CollisionShape3D>(0).Scale = body.Scale;
				maxSpeed = 7.6f;
			}
		} else {
			if (Input.IsActionPressed(crouchInput))
				rig.ApplyForce(-body.Basis.Y * jumpForce);
		}

		if (Input.IsActionJustReleased(crouchInput))
			releaseCrouch = true;

		if (releaseCrouch == true && !crouchBoxCast.IsColliding())
			phy_CheckForCrouching();
	}

	void phy_CheckForCrouching() {
		body.Scale = new Vector3(body.Scale.X, startYScale, body.Scale.X);
		rig.GetChild<CollisionShape3D>(0).Scale = body.Scale;
		set_defaults();

		if (grounded)
			rig.ApplyCentralForce(Vector3.Down * jumpForce * 2);

		releaseCrouch = false;
	}

	public void fun_KillMe() {
		GetTree().ReloadCurrentScene();
	}
}
