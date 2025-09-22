using Godot;
using System;

public partial class AreaInteraction : Area3D {
  /// <summary>
  /// An event that gets called only when the player is inside the
  /// area, and a the player presses a button.
  /// </summary>
  public event Action<Node3D, InputEvent> InteractionInsideEvent;

  /// <summary>
  /// An event that gets called when the player exists the 
  /// area and presses a button.
  /// </summary>
  public event Action<Node3D, InputEvent> InteractionExitEvent;

  /// <summary>
  /// Helper variable that can be used in conjuction with the IsLookingAt
  /// function to make sure the player is looking at the area.
  /// </summary>
  [Export] public Node3D playerObserver { get; private set; } = null;

  private Node3D currentBody = null;
  private bool isBodyInside = false;

  /// <summary>
  /// Check if nodeA is looking at nodeB using dot product
  /// </summary>
  /// <param name="observer">The node doing the looking</param>
  /// <param name="target">The node being looked at</param>
  /// <param name="angleThreshold">Maximum angle in degrees (default 45°)</param>
  /// <returns>True if observer is looking at target</returns>
  public static bool IsLookingAt(Node3D observer, Node3D target, float angleThreshold = 45f) {
    // get world positions
    Vector3 observerPos = observer.GlobalPosition;
    Vector3 targetPos = target.GlobalPosition;

    // get the proper vectos and precalculate them.
    Vector3 directionToTarget = (targetPos - observerPos).Normalized();
    Vector3 observerForward = -observer.GlobalTransform.Basis.Z;

    // calculate the dot and the angle threshold
    float dotProduct = observerForward.Dot(directionToTarget);
    float cosineThreshold = Mathf.Cos(Mathf.DegToRad(angleThreshold));

    // return true if the dot product is withint the calculated angle threshold
    return dotProduct >= cosineThreshold;
  }

  public override void _Ready() {
    base._Ready();
    BodyEntered += OnBodyEntered;
    BodyExited += OnBodyExited;
  }

  private void OnBodyEntered(Node3D body) {
    isBodyInside = true;
    currentBody = body;
  }

  private void OnBodyExited(Node3D body) {
    isBodyInside = false;
    currentBody = body;
  }

  public override void _UnhandledKeyInput(InputEvent @event) {
    base._UnhandledKeyInput(@event);

    if (@event.IsPressed() && currentBody != null) {
      if (isBodyInside) {
        InteractionInsideEvent?.Invoke(currentBody, @event);
      } else {
        InteractionExitEvent?.Invoke(currentBody, @event);
        currentBody = null;
      }
    }
  }
}