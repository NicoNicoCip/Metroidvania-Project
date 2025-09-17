using Godot;

public partial class PlayerMS : GlobalMS
{
    [Export] float sensetivity = 5.0f;

    ShapeCast3D crouchBoxCast;
    public bool readyFlag = false;
    float Xrot;
    float Yrot; 
    float startYScale;
    bool releaseCrouch;

    public override void _Ready()
    {
        fun_Bind();
        set_defaults();
        startYScale = rig.Scale.Y;
        readyFlag = true;
    }
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        phy_Grounded();
        inputDir = Input.GetVector("Move Backwards", "Move Forwards", "Move Left", "Move Right");
        maxAccel = maxSpeed * 10;

        if (!phy_Sloped())
            dir = fun_CalculateWishDir();
        else
        {
            dir = phy_GetSlopeDir();
            grounded = true;
        }

        fun_DragCalculations();
        fun_UpdateVelocity(delta);

        if (phy_Sloped())
            phy_Jump("Jump", -1, delta);
        else
            phy_Jump("Jump", 0.2f, delta);

        phy_Crouch(delta);


        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (rig.GlobalPosition.Y <= -64) fun_KillMe();
    }
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if(@event is InputEventMouseMotion mouseMotion)
        {
            Xrot -= mouseMotion.Relative.Y * 0.001f * sensetivity;
            Yrot -= mouseMotion.Relative.X * 0.001f * sensetivity;

            Xrot = Mathf.Clamp(Xrot, -1.45f, 1.45f);
            head.GlobalRotation = new Vector3(Xrot,Yrot,0.0f);

            orientation.GlobalRotation = new Vector3(0.0f,Yrot,0.0f);
        }
    }

    void fun_Bind()
    {
        crouchBoxCast = GetChild(1).GetChild(2).GetChild(5) as ShapeCast3D;
        init_FullBind();
    }

    void phy_Crouch(double delta)
    {
        if (!waterBoxCast.IsColliding())
        {
            if (Input.IsActionJustPressed("Crouch"))
            {
                body.Scale = new Vector3(body.Scale.X, 0.65f, body.Scale.Z);
                rig.GetChild<CollisionShape3D>(0).Scale = body.Scale;
                maxSpeed = 7.6f;
            }
        }
        else
        {
            if (Input.IsActionPressed("Crouch"))
                rig.ApplyForce(-body.Basis.Y * jumpForce);
        }

        if (Input.IsActionJustReleased("Crouch"))
            releaseCrouch = true;

        if (releaseCrouch == true && !crouchBoxCast.IsColliding())
            phy_CheckForCrouching();
    }
    void phy_CheckForCrouching()
    {
        body.Scale = new Vector3(body.Scale.X, startYScale, body.Scale.X);
        rig.GetChild<CollisionShape3D>(0).Scale = body.Scale;
        set_defaults();

        if (grounded)
            rig.ApplyCentralForce(Vector3.Down * jumpForce * 2);

        releaseCrouch = false;
    }
    public void fun_KillMe()
    {
        GetTree().ReloadCurrentScene();
    }
}