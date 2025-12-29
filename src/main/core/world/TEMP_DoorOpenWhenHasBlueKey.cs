using Godot;
using System;

public partial class TEMP_DoorOpenWhenHasBlueKey : AreaInteraction {
    [Export] Node3D doorRight;
    [Export] Node3D doorLeft;

    private ItemManager manager = null;
    private ToastManager toastManager = null;
    private Toast toast = null;

    const string interactAction = "Interact";
    const int looseLookAngle = 60; // degrees.

    public override void _Ready() {
        base._Ready();
        InteractionInsideEvent += giveCardToPlayer;
        manager = GetTree().CurrentScene.FindChild("Player Items", true)
        as ItemManager;

        playerObserver = GetTree().CurrentScene.FindChild("Camera", true)
        as Node3D;

        toastManager = GetTree().CurrentScene.FindChild("Toast Manager", true)
        as ToastManager;

        toast = new Toast(toastManager, Toast.LIFETIME.TWO_SECONDS);
    }

    protected override void OnEnterArea(Node3D body) {
        base.OnEnterArea(body);

        if (body.GetMeta("PlayerCollider", false).AsBool()) {
            toast.post("Press [E] while looking at the door to open it.");
        }
    }

    protected override void OnExitArea(Node3D body) {
        base.OnExitArea(body);
        toast.hide();
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
            Item blueKey = manager.findByName("Blue Key");
            if (blueKey.isUnlocked()) {
                blueKey.setUnlocked(false);
                if (IsInstanceValid(doorRight)) doorRight.QueueFree();
                if (IsInstanceValid(doorLeft)) doorLeft.QueueFree();
            } else {
                toast.post("No blue key.");
            }
        }
    }
}
