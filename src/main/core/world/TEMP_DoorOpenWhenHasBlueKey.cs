using Godot;
using System;

public partial class TEMP_DoorOpenWhenHasBlueKey : AreaInteraction {
  [Export] Node3D doorRight;
  [Export] Node3D doorLeft;

  const string interactAction = "Interact";
  private ItemManager manager = null;

  public override void _Ready() {
    base._Ready();
    InteractionInsideEvent += giveCardToPlayer;
  }

  private void giveCardToPlayer(Node3D body, InputEvent iEvent) {
    if (body.HasMeta("PlayerCollider") && manager == null) {
      manager = (ItemManager)body.GetParent().GetParent().GetChild(0);
    }

    if (manager != null && iEvent.IsActionPressed(interactAction)) {
      if (manager.checkBlueKey()) {
        manager.takeBlueKey();
        if (IsInstanceValid(doorRight)) doorRight.QueueFree();
        if (IsInstanceValid(doorLeft)) doorLeft.QueueFree();
      } else {
        GD.Print("No blue key.");
      }
    }
  }
}
