using Godot;
using System;
using static GlobalMS;

public partial class WatchmanItemBE : Node3D
{
	AnimationPlayer ap;
	AnimationTree at;
	Node3D model;
	Master master;
    TimeSave s = new TimeSave();
    Node3D cam;

	bool playedPickupAnimation;
	bool clickHolder;
	bool prevMB;

    public override void _EnterTree()
    {
        base._EnterTree();

        ap = GetParent().GetChild(0) as AnimationPlayer;
		at = GetParent().GetChild(1) as AnimationTree;
		model = GetParent().GetChild(2) as Node3D;
		master = GetTree().CurrentScene as Master;
		cam = GetParent().GetParent().GetParent() as Node3D;
		model.Visible = false;
    }

	public override void _Process(double delta)
	{
		fun_InitAnimations();
		fun_LeftClickAction();
	}

	void fun_InitAnimations()
	{
        if (master.playerHasWatchman && !playedPickupAnimation)
        {
            model.Visible = true;
            ap.Play("Pickup Animation");
            playedPickupAnimation = true;
        }

        if (master.playerHasWatchman)
        {
            Vector2 absCam = test_CameraRotationBounds(cam.RotationDegrees);

            model.Position += new Vector3(absCam.X * 0.01f, absCam.Y * 0.01f, model.Position.Z);
            model.Position = model.Position.Lerp(Vector3.Zero, 0.5f);
        }
    }

	void fun_LeftClickAction()
	{
		bool mbp = Input.IsActionJustPressed("Mouse Left");

		if (mbp && (!ap.IsPlaying() || ap.CurrentAnimation != "Pause Start"))
			clickHolder = !clickHolder;

		if(clickHolder && mbp)
		{
			ap.Stop();
            ap.Play("Pause Start");
            ap.Queue("Pause Hold");

            s.fun_SaveTimePoint();
        }
		else if (clickHolder && ap.CurrentAnimation == "Pause Hold")
		{
			ap.Play("Pause Hold");
		}
		else if(!clickHolder && mbp)
		{
            s.fun_LoadTimePoint();
            ap.Play("Pause Release");
		}
	}

	Vector3 localCamRot;
	Vector3 oldCamRot;

    Vector2 test_CameraRotationBounds(Vector3 camRot)
	{
		localCamRot = new Vector3(camRot.X - oldCamRot.X,camRot.Y - oldCamRot.Y,camRot.Z - oldCamRot.Z);
		oldCamRot = camRot;

		return new Vector2(localCamRot.X, localCamRot.Y).Normalized();
	}
}
