using Godot;
using System;

public partial class ItemDisplay : Node3D {
    [Export] ItemManager manager;
    [Export] MeshInstance3D blueKey = new MeshInstance3D();

    public override void _Process(double delta) {
        base._Process(delta);

        blueKey.Visible = manager.findByID(0).isUnlocked();
    }
}
