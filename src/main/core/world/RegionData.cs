using Godot;
using System;

public partial class RegionData : Node3D {
  [Export] public Node3D region;
  [Export] public Node[] AdjacentRooms = new Node[1];
}
