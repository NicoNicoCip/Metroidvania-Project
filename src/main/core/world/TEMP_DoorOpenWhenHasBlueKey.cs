using Godot;
using System;

public partial class TEMP_DoorOpenWhenHasBlueKey : AreaInteraction {
    [Export] Node3D doorRight;
    [Export] Node3D doorLeft;
    [Export] RichTextLabel infotext;

    const string interactAction = "Interact";

    const int looseLookAngle = 30; // degrees.
    private ItemManager manager = null;

    public override void _Ready() {
        base._Ready();
        InteractionInsideEvent += giveCardToPlayer;
        manager = GetTree().CurrentScene.FindChild("Player Items", true) as ItemManager;

        playerObserver = GetTree().CurrentScene.FindChild("Camera", true) as Node3D;
    }

    protected override void OnEnterArea(Node3D body) {
        base.OnEnterArea(body);
        infotext.Text = "Press [E] while looking at the door to open it.";
    }

    protected override void OnExitArea(Node3D body) {
        base.OnExitArea(body);
        infotext.Text = "";
    }

    private void giveCardToPlayer(Node3D body, InputEvent iEvent) {
        bool looking = IsLookingAt(
            playerObserver, 
            (Node3D)GetParent(), 
            looseLookAngle
        );

        if (manager != null 
            && iEvent.IsActionPressed(interactAction) 
            && looking
        ) {
            if (manager.checkBlueKey()) {
                manager.takeBlueKey();
                if (IsInstanceValid(doorRight)) doorRight.QueueFree();
                if (IsInstanceValid(doorLeft)) doorLeft.QueueFree();
            } else {
                infotext.Text = "No blue key.";
            }
        }
    }
}
