using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

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

    AudioStream audiostream = walkstream[0];

    fun_jump();
    fun_walk();

    prevContact = msgc;
  }

  void fun_jump()
  {
    if (prevContact != msgc && ms.jumped)
    {
      Walk.PitchScale = rng.RandfRange(0.9f, 1.1f) + ms.rig.LinearVelocity.Length() * .01f;
      snd_walk();
    }
  }

  void fun_walk()
  {
    const float speed = .5f;
    const float lowerPitch = 0.9f;
    const float upperPitch = 1.1f;
    const float scaleModifier = .1f;

    if (ms.rig.LinearVelocity.Length() > speed)
    {
      if (msgc == true && !Walk.Playing)
      {
        Walk.PitchScale = rng.RandfRange(lowerPitch, upperPitch) + ms.rig.LinearVelocity.Length() * scaleModifier;
        snd_walk();
      }
    }
  }

  void snd_walk()
  {
    if (!msgc) return;
    string sound = null;
    var cast = ms.groundCast.GetCollider();
    Array<StringName> metadata = cast.GetMetaList();

    foreach (string name in metadata)
    {
      if (name.ToLower() == "sound")
      {
        sound = (string)cast.GetMeta(name);
        break;
      }
    }

    if (sound == null)
    {
      GD.PushError("Could not find the sound meta tag on " + ms.groundCast.GetCollider());
      return;
    }

    
    AudioStream audiostream = walkstream[0];
    
    if (sound == "iron") audiostream = walkstream[1];
    else if (sound == "stone") audiostream = walkstream[2];

    Walk.Stream = audiostream;
    Walk.Play();
  }
}
