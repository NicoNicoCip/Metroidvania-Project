using System;
using Godot;

public partial class Ball : MotionController {
    protected override Vector3 GetWishDirection() {
        return new();
    }
}