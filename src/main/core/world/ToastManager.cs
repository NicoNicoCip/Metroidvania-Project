using Godot;
using System;

public partial class ToastManager : Control {
    [Export] public RichTextLabel textLabel;
    public float lifeLeft = 0;

    public override void _Ready() {
        base._Ready();
        Visible = false;
        lifeLeft = 0;
    }

    public override void _Process(double delta) {
        base._Process(delta);

        if(lifeLeft == -25) {
            return;
        }

        if(lifeLeft > 0) {
            lifeLeft -= (float)delta * 1000;
        }

        if(lifeLeft <= 0) {
            lifeLeft = 0;
            Visible = false;
        }
    }
}