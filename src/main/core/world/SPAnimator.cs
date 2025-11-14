using Godot;
using System;

public partial class SPAnimator : Node3D {
    // this code is for switching out sounds and animating them at the 
    // same time, like the lights and light sounds. 
    [Export] Node3D[] switchers;
    RandomNumberGenerator rand = new RandomNumberGenerator();

    float counter;
    float rid;
    bool Switch = true;
    bool prevSwitch = true;

    public override void _EnterTree() {
        base._EnterTree();
        Switch = true;
        rand.InitRef();
        rid = rand.RandfRange(0.2f, 5);
    }

    public override void _Process(double delta) {
        base._Process(delta);

        if (counter >= rid || counter >= 5) {
            counter = 0;
            rid = rand.RandfRange(0.2f, 5);
            Switch = !Switch;
        } else counter += (float)delta;

        if (Switch != prevSwitch) {
            ((AudioStreamPlayer3D)switchers[0]).StreamPaused = !Switch;
            AudioStreamPlayer3D swich = (AudioStreamPlayer3D)switchers[1];
            if (!Switch) {
                swich.PitchScale = .6f;
                swich.Play();
            } else {
                swich.PitchScale = .8f;
                swich.Play();
            }


            for (int i = 2; i < switchers.Length; i++)
                switchers[i].Visible = Switch;
        }

        prevSwitch = Switch;
    }
}
