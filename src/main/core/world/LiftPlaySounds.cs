using Godot;
using System;

public partial class LiftPlaySounds : AudioStreamPlayer3D {

  public void PlaySoundExclusive() {
    Stop();
    Play();
  }
}
