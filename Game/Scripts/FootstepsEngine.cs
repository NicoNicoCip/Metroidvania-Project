using Godot;
using System;

public partial class FootstepsEngine : Node3D
{
    [Export] GlobalMS ms;
    [Export] AudioStreamPlayer3D Walk;
    [Export] AudioStream[] walkstream;

    RandomNumberGenerator rng = new RandomNumberGenerator();
	bool prevContact = true;
    bool msgc = true;
    bool cycled = false;

    public override void _EnterTree()
    {
        base._EnterTree();
        rng.InitRef();
    }

	public override void _Process(double delta)
	{
        base._Process(delta);
        msgc = ms.groundCast.IsColliding();

        if (!cycled)
        {
            cycled = true;
            return;
        }

        fun_jump();
        fun_walk();

        prevContact = msgc;
    }

    void fun_jump()
    {
        if(prevContact != msgc && ms.jumped)
        {
            Walk.PitchScale = rng.RandfRange(0.9f, 1.1f) + ms.rig.LinearVelocity.Length() * .01f;
            snd_walk();
        }
    }

    void fun_walk()
    {
        float spd = .5f;

        if (ms.rig.LinearVelocity.Length() > spd)
        {
            if (msgc == true && !Walk.Playing)
            {
                Walk.PitchScale = rng.RandfRange(0.9f,1.1f) + ms.rig.LinearVelocity.Length() * .1f ;
                snd_walk();
            }
        }
    }

    void snd_walk()
    {
        if (!msgc) return;

        string sound = (string)ms.groundCast.GetCollider().GetMeta("Sound");
        AudioStream aus = null;
        if(sound == "iron") aus = walkstream[1];
        if(sound == "stone") aus = walkstream[2];

        if(aus == null) aus = walkstream[0];

        Walk.Stream = aus;
        Walk.Play();
    }
}
