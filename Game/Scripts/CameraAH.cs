using Godot;
using System;

public partial class CameraAH : Node
{
	[Export] float xMul = 1;
	[Export] float yMul = 1;
	[Export] float speed = 1;

	Node3D camera;
	GlobalMS ms;

	float index;

    public override void _EnterTree()
    {
        base._EnterTree();
        camera = (Node3D)GetParent().GetChild(1);
		ms = (GlobalMS)FindParent("Player");
	}

	public override void _Process(double delta)
	{
		if (ms.groundCast.IsColliding() && ms.rig.LinearVelocity.Length() > .5f) anim_Bob((float)delta);
		else if (camera.Position.DistanceTo(Vector3.Zero) > .1f)
			camera.Position = camera.Position.Lerp(Vector3.Zero, (float)delta * speed);
    }

	void anim_Bob(float delta)
	{
		float spd = speed * ms.rig.LinearVelocity.Length();

        index += delta * spd;

		float xsin = Mathf.Sin(index)* xMul;
		float ysin = Mathf.Cos(index* 2) * yMul;

        camera.Position = new Vector3(xsin, ysin, 0.0f);
    }
}
