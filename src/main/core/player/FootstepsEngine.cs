using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class FootstepsEngine : Node3D {
    [Export] PlayerController motion;
    [Export] AudioStreamPlayer3D streamPlayer;
    [Export] String[] metas;
    [Export] AudioStream[] stream;
    RandomNumberGenerator rng = new RandomNumberGenerator();
    bool prevGroundCheck = true;
    bool motionGroundCheck = true;
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
        motionGroundCheck = motion.isGrounded();
        if (!cycled) {
            cycled = true;
            return;
        }

        fun_jump();
        fun_streamPlayer();
        prevGroundCheck = motionGroundCheck;
    }

    void fun_jump() {
        if (prevGroundCheck != motionGroundCheck && motion.getTensors().Y > 0) {
            streamPlayer.PitchScale = rng.RandfRange(0.9f, 1.1f) 
                + motion.getRigidBody().LinearVelocity.Length() * .01f;
            _ = snd_streamPlayer();
        }
    }

    void fun_streamPlayer() {
        const float speed = .5f;
        const float lowerPitch = 0.9f;
        const float upperPitch = 1.1f;
        const float scaleModifier = .1f;
        const float linearVelLowerCap = 0.0f;
        const float linearVelUpperCap = 3.5f;
        float linearVelocity = Mathf.Clamp(
          motion.getRigidBody().LinearVelocity.Length(),
          linearVelLowerCap,
          linearVelUpperCap
        );

        if (linearVelocity > speed) {
            if (motionGroundCheck == true && !streamPlayer.Playing) {
                streamPlayer.PitchScale = rng.RandfRange(lowerPitch, upperPitch) + linearVelocity * scaleModifier;
                _ = snd_streamPlayer();
            }
        }
    }

    async Task snd_streamPlayer() {
        if (waitedTime < minWaitTime) return;
        waitedTime = 0;

        if (!motionGroundCheck) return;
        string sound = null;
        var cast = motion.getGroundCast().GetCollider();
        Array<StringName> metadata = cast.GetMetaList();

        foreach (string name in metadata) {
            if (name.ToLower() == "sound") {
                sound = (string)cast.GetMeta(name);
                break;
            }
        }

        if (sound == null) {
            GD.PushError("Could not find the sound meta tag on " 
            + motion.getGroundCast().GetCollider());
            return;
        }
        
        AudioStream audiostream = stream[0];
        for (int i = 1; i < metas.Length; i++) {
            if(sound == metas[i]) {
                audiostream = stream[i];
                
                break;
            }
        }

        // Only stop and restart if we're changing streams
        if (currentStream != audiostream) {
            currentStream = audiostream;
            streamPlayer.Stream = audiostream;
            streamPlayer.Stop();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        streamPlayer.Play();
    }
}