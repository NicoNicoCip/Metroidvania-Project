using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class FootstepsEngine : Node3D {
    [Export] GlobalMS ms;
    [Export] AudioStreamPlayer3D Walk;
    [Export] AudioStream[] walkstream;
    RandomNumberGenerator rng = new RandomNumberGenerator();
    bool prevContact = true;
    bool msgc = true;
    bool cycled = false;
    float minWaitTime = 0.4f;

    private float waitedTime = 0.0f;
    private AudioStream currentStream;

    public override void _EnterTree() {
        base._EnterTree();
        rng.InitRef();
    }

    public override void _Process(double delta) {
        if (waitedTime < minWaitTime) waitedTime += (float)delta;

        base._Process(delta);
        msgc = ms.groundCast.IsColliding();
        if (!cycled) {
            cycled = true;
            return;
        }

        fun_jump();
        fun_walk();
        prevContact = msgc;
    }

    void fun_jump() {
        if (prevContact != msgc && ms.jumped) {
            Walk.PitchScale = rng.RandfRange(0.9f, 1.1f) + ms.rig.LinearVelocity.Length() * .01f;
            _ = snd_walk();
        }
    }

    void fun_walk() {
        const float speed = .5f;
        const float lowerPitch = 0.9f;
        const float upperPitch = 1.1f;
        const float scaleModifier = .1f;
        const float linearVelLowerCap = 0.0f;
        const float linearVelUpperCap = 3.5f;
        float linearVelocity = Mathf.Clamp(
          ms.rig.LinearVelocity.Length(),
          linearVelLowerCap,
          linearVelUpperCap
        );

        if (linearVelocity > speed) {
            if (msgc == true && !Walk.Playing) {
                Walk.PitchScale = rng.RandfRange(lowerPitch, upperPitch) + linearVelocity * scaleModifier;
                _ = snd_walk();
            }
        }
    }

    async Task snd_walk() {
        if (waitedTime < minWaitTime) return;
        waitedTime = 0;

        if (!msgc) return;
        string sound = null;
        var cast = ms.groundCast.GetCollider();
        Array<StringName> metadata = cast.GetMetaList();

        foreach (string name in metadata) {
            if (name.ToLower() == "sound") {
                sound = (string)cast.GetMeta(name);
                break;
            }
        }

        if (sound == null) {
            GD.PushError("Could not find the sound meta tag on " + ms.groundCast.GetCollider());
            return;
        }

        AudioStream audiostream = walkstream[0];
        if (sound == "iron") audiostream = walkstream[1];
        else if (sound == "stone") audiostream = walkstream[2];

        // Only stop and restart if we're changing streams
        if (currentStream != audiostream) {
            currentStream = audiostream;
            Walk.Stream = audiostream;
            Walk.Stop();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        Walk.Play();
    }
}