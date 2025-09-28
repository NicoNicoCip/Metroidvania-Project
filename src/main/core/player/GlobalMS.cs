using Godot;
using System;

public partial class GlobalMS : Node3D {
  [Export] protected float maxSpeed;
  [Export] private float maxWaterSpeed;
  [Export] private float maxAirSpeed = 12.0f;
  [Export] private float clFwrdSpd = 1.0f;
  [Export] private float clSideSpd = 1.0f;
  [Export] protected float jumpForce;
  [Export] private float groundDrag;
  [Export] private float waterDrag;

  protected float maxAccel;
  private float drag;
  private float wait0;
  public bool jumped;
  protected Vector3 dir;
  public Vector2 inputDir;
  private bool released;
  protected bool grounded;

  public RigidBody3D rig;
  protected Node3D orientation;
  protected Node3D head;
  protected Node3D body;
  public RayCast3D groundCast;
  private RayCast3D slopeCast;
  protected ShapeCast3D waterBoxCast;
  private ShapeCast3D contactCast;
  private Node initialParent;

  private float[] initial = new float[8];

  protected Data get_Data() {
    return new Data() {
      _velocity = rig.AngularVelocity,
      _position = body.GlobalPosition,
      _groundDrag = groundDrag,
      _waterDrag = waterDrag,
      _jumpFroce = jumpForce,
      _walkSpeed = maxSpeed,
      _waterSpeed = maxAirSpeed
    };
  }
  protected void set_Data(Data data) {
    groundDrag = data._groundDrag;
    waterDrag = data._waterDrag;
    jumpForce = data._jumpFroce;
    maxSpeed = data._walkSpeed;
    maxWaterSpeed = data._walkSpeed;
  }
  protected void set_defaults() {
    maxSpeed = initial[0];
    maxWaterSpeed = initial[1];
    maxAirSpeed = initial[2];
    maxAccel = maxSpeed * 10;
    clFwrdSpd = initial[3];
    clSideSpd = initial[4];
    jumpForce = initial[5];
    groundDrag = initial[6];
    waterDrag = initial[7];
  }
  protected void init_FullBind() {
    initial[0] = maxSpeed;
    initial[1] = maxWaterSpeed;
    initial[2] = maxAirSpeed;
    initial[3] = clFwrdSpd;
    initial[4] = clSideSpd;
    initial[5] = jumpForce;
    initial[6] = groundDrag;
    initial[7] = waterDrag;

    rig = (RigidBody3D)GetChild(1).GetChild(2);
    orientation = (Node3D)rig.GetChild(2);
    head = (Node3D)GetChild(1).GetChild(0).GetChild(0).GetChild(0);
    body = (Node3D)GetChild(1).GetChild(1);
    groundCast = (RayCast3D)rig.GetChild(3);
    slopeCast = (RayCast3D)rig.GetChild(4);
    waterBoxCast = (ShapeCast3D)rig.GetChild(6);
    contactCast = (ShapeCast3D)rig.GetChild(7);
    initialParent = GetParent();
  }

  protected void fun_DragCalculations() {
    if (grounded || phy_Sloped()) drag = groundDrag;
    else if (waterBoxCast.IsColliding()) drag = waterDrag;
    else drag = 0;

    if (waterBoxCast.IsColliding() || phy_Sloped())
      rig.GravityScale = 0;
    else
      rig.GravityScale = 6;
  }
  protected void fun_UpdateVelocity(double delta) {
    Node3D contactCollider = null;
    if (grounded) contactCollider = (Node3D)contactCast.GetCollider(0);

    if (contactCollider != null) {
      if (contactCast.IsColliding() && GetParent() == initialParent) {
        Reparent(contactCollider, true);
      } else if (contactCast.IsColliding() && contactCollider != GetParent()) {
        Reparent(initialParent, true);
      }
    }

    if (grounded || phy_Sloped() && !waterBoxCast.IsColliding())
      rig.LinearVelocity = phy_UpdateVelGround(dir, rig.LinearVelocity, delta);
    else if (!waterBoxCast.IsColliding())
      rig.LinearVelocity = phy_UpdateVelAir(dir, rig.LinearVelocity, delta);
    else
      rig.LinearVelocity = phy_UpdateVelWater(dir, rig.LinearVelocity, delta);
  }
  protected void phy_Jump(bool act, float wait, double delta) {
    float w = 0;
    if (wait == -1) w = 0.6f;
    else w = wait;

    if (act && jumped == false && !waterBoxCast.IsColliding()) {
      if (grounded && !waterBoxCast.IsColliding()) {
        rig.ApplyCentralImpulse(jumpForce * Vector3.Up);
        jumped = true;
      }
    } else if (act && waterBoxCast.IsColliding()) {
      rig.ApplyCentralForce(rig.Basis.Y * jumpForce * 1.5f);
    }

    if (!act && released) {
      wait0 = 0;
      jumped = false;
    }

    if (jumped) {
      wait0 += (float)delta;

      if (wait0 > w && grounded) {
        jumped = false;
        wait0 = 0;
      }
    }

    released = act;
    rig.ForceUpdateTransform();
  }
  protected void phy_Jump(string act, float wait, double delta) {
    float w = 0;
    if (wait == -1) w = 0.6f;
    else w = wait;

    if (Input.IsActionPressed(act) && jumped == false && !waterBoxCast.IsColliding()) {
      if (grounded && !waterBoxCast.IsColliding()) {
        rig.ApplyCentralImpulse(jumpForce * Vector3.Up);
        jumped = true;
      }
    } else if (Input.IsActionPressed(act) && waterBoxCast.IsColliding()) {
      rig.ApplyCentralForce(rig.Basis.Y * jumpForce * 1.5f);
    }

    if (Input.IsActionJustReleased(act)) {
      wait0 = 0;
      jumped = false;
    }

    if (jumped) {
      wait0 += (float)delta;

      if (wait0 > w && grounded) {
        jumped = false;
        wait0 = 0;
      }
    }

    rig.ForceUpdateTransform();
  }
  private void fun_CalculateFrinction(ref Vector3 vel, float delta) {
    float spd = vel.Length();

    if (spd <= 0.00001f) return;

    float downLimit = Mathf.Max(spd, 0.5f);
    float dampAmmount = spd - (downLimit * drag * delta);

    if (dampAmmount < 0) dampAmmount = 0;

    vel *= dampAmmount / spd;
  }

  protected Vector3 fun_CalculateWishDir() {
    return (-orientation.Transform.Basis.Z * inputDir.X * clFwrdSpd + orientation.Transform.Basis.X * inputDir.Y * clSideSpd).Normalized();
  }
  private Vector3 phy_UpdateVelGround(Vector3 wishDir, Vector3 vel, double delta) {
    fun_CalculateFrinction(ref vel, (float)delta);

    float currSpd = vel.Dot(wishDir);
    float addSpd = (float)Mathf.Clamp(maxSpeed - currSpd, 0, maxAccel * delta);

    return vel + addSpd * wishDir;
  }
  private Vector3 phy_UpdateVelAir(Vector3 wishDir, Vector3 vel, double delta) {

    float currSpd = vel.Dot(wishDir);
    float addSpd = (float)Mathf.Clamp(maxAirSpeed - currSpd, 0, maxAccel * delta);

    return vel + addSpd * wishDir;
  }
  private Vector3 phy_UpdateVelWater(Vector3 wishDir, Vector3 vel, double delta) {
    fun_CalculateFrinction(ref vel, (float)delta);

    float currSpd = vel.Dot(wishDir);
    float addSpd = (float)Mathf.Clamp(maxWaterSpeed - currSpd, 0, maxAccel * delta);

    return vel + addSpd * wishDir;
  }

  protected void phy_Grounded() {
    grounded = groundCast.IsColliding();
  }
  protected bool phy_Sloped() {
    if (slopeCast.IsColliding()) {
      float angle = Vector3.Up.AngleTo(slopeCast.GetCollisionNormal());
      return angle < 45 && angle != 0;
    }

    return false;
  }
  protected Vector3 phy_GetSlopeDir() {
    return ProjectOnPlane(fun_CalculateWishDir(), slopeCast.GetCollisionNormal()).Normalized();
  }
  private Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal) {
    float num = planeNormal.Dot(planeNormal);
    if (num < Mathf.Epsilon) {
      return vector;
    }

    float num2 = vector.Dot(planeNormal);
    return new Vector3(
        vector.X - planeNormal.X * num2 / num,
        vector.Y - planeNormal.Y * num2 / num,
        vector.Z - planeNormal.Z * num2 / num);
  }

  public struct Data {
    public Vector3 _position;
    public Vector3 _velocity;
    public float _groundDrag;
    public float _waterDrag;
    public float _jumpFroce;
    public float _waterSpeed;
    public float _walkSpeed;
  }
}