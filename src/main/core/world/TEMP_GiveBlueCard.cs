using Godot;
using System;

public partial class TEMP_GiveBlueCard : AreaInteraction {
    const string interactAction = "Interact";
    const int looseLookAngle = 30; // degrees.
    private ItemManager manager = null;
    private bool isInitialized = false;
    [Export] RichTextLabel infotext;

    public override void _Ready() {
        base._Ready();
        InteractionInsideEvent += giveCardToPlayer;
        manager = GetTree().CurrentScene.FindChild("Player Items", true) as ItemManager;

        playerObserver = GetTree().CurrentScene.FindChild("Camera", true) as Node3D;
    }


    protected override void OnEnterArea(Node3D body) {
        base.OnEnterArea(body);
        infotext.Text = "Press [E] while looking at the box to get a card.";
    }

    protected override void OnExitArea(Node3D body) {
        base.OnExitArea(body);
        infotext.Text = "";
    }

    private void giveCardToPlayer(Node3D body, InputEvent iEvent) {
        if (manager != null && iEvent.IsActionPressed(interactAction) &&
            IsLookingAt(playerObserver, (Node3D)GetParent(), looseLookAngle)
        ) {
            manager.giveBlueKey();
        }
    }
}
