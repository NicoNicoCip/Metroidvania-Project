using Godot;
using System;

public partial class FollowNode : Node3D {
    [Export] Node3D toFollow;
    [Export] float lerp = 0.2f;

    public override void _Process(double delta) {
        GlobalPosition = GlobalPosition.Lerp(toFollow.GlobalPosition, lerp);
    }
}
