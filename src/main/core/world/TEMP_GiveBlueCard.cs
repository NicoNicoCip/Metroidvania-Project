using Godot;
using System;

public partial class TEMP_GiveBlueCard : AreaInteraction {
  const string interactAction = "Interact";
  const int looseLookAngle = 15; // degrees.
  private ItemManager manager = null;
  private bool isInitialized = false;

  public override void _Ready() {
    base._Ready();
    InteractionInsideEvent += giveCardToPlayer;
    manager = GetTree().CurrentScene.FindChild("Player Items", true) as ItemManager;
  }

  private void giveCardToPlayer(Node3D body, InputEvent iEvent) {
    if (manager != null && iEvent.IsActionPressed(interactAction) &&
        IsLookingAt(playerObserver, (Node3D)GetParent(), looseLookAngle)
    ) {
      manager.giveBlueKey();
    }
  }
}
