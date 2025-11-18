using Godot;
using System;

public partial class TEMP_GiveBlueCard : AreaInteraction {
    private ItemManager itemManager = null;
    private ToastManager toastManager = null;
    private Toast toast = null;
    private bool isInitialized = false;

    const string interactAction = "Interact";
    const int looseLookAngle = 30; // degrees.

    public override void _Ready() {
        base._Ready();
        InteractionInsideEvent += giveCardToPlayer;
        itemManager = GetTree().CurrentScene.FindChild("Player Items", true)
        as ItemManager;

        playerObserver = GetTree().CurrentScene.FindChild("Camera", true)
        as Node3D;

        toastManager = GetTree().CurrentScene.FindChild("Toast Manager", true)
        as ToastManager;

        toast = new Toast(toastManager, Toast.LIFETIME.INFINITE);
    }


    protected override void OnEnterArea(Node3D body) {
        base.OnEnterArea(body);
        if (body.GetMeta("PlayerCollider", false).AsBool()) {
            toast.post("Press [E] while looking at the box to get a card.");
        }
    }

    protected override void OnExitArea(Node3D body) {
        base.OnExitArea(body);
        toast.hide();
    }

    private void giveCardToPlayer(Node3D body, InputEvent iEvent) {
        if (itemManager != null && iEvent.IsActionPressed(interactAction) &&
            IsLookingAt(playerObserver, (Node3D)GetParent(), looseLookAngle)
        ) {
            itemManager.findByName("blue key").setUnlocked(true);
        }
    }
}
