using Godot;
using System;

public partial class Lift_Sequence : AnimationPlayer
{
    public override void _EnterTree()
    {
        base._EnterTree();
        Play("Inro_LiftGlingDown");
	}
}
