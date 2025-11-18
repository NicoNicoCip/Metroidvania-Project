using System;
using Godot;

public partial class Ball : MotionController {
    protected override Vector3 GetWishDirection() {
        return Vector3.Up * 0.005f;
    }
}