using Godot;
using System;

public partial class ItemManager : Node3D {
  [Export] private bool hasBoots = false;
  [Export] private bool hasBlueKey = false;

  public void giveBoots() {
    hasBoots = true;
  }

  public void takeBoots() {
    hasBoots = false;
  }

  public void giveBlueKey() {
    hasBlueKey = true;
  }

  public void takeBlueKey() {
    hasBlueKey = false;
  }

  public bool checkBoots() {
    return hasBoots;
  }

  public bool checkBlueKey() {
    return hasBlueKey;
  }
}
