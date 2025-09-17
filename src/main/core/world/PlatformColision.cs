using Godot;
using System;

public partial class PlatformColision : Area3D
{
  private Node3D lastBodyParent = null;
  private Node3D currentBody = null;

  public override void _Ready()
  {
    base._Ready();

    BodyEntered += OnBodyEntered;
    BodyExited += OnBodyExited;

    GD.Print("Platform collision area ready");
  }

  private void OnBodyEntered(Node3D body)
  {
    if (IsPlayerBody(body) && currentBody == null)
    {
      lastBodyParent = body.GetParent<Node3D>();
      currentBody = body;

      CallDeferred(nameof(ReparentPlayer), body);
    }
  }

  private void OnBodyExited(Node3D body)
  {
    if (IsPlayerBody(body) && currentBody == body && lastBodyParent != null)
    {
      CallDeferred(nameof(RestorePlayerParent), body);
    }
  }

  private void ReparentPlayer(Node3D body)
  {
    try
    {
      if (body.IsInsideTree() && GetParent() != null)
      {
        body.Reparent(GetParent(), true);
      }
    }
    catch (Exception e)
    {
      GD.PrintErr($"Error reparenting player: {e.Message}");
      currentBody = null;
      lastBodyParent = null;
    }
  }

  private void RestorePlayerParent(Node3D body)
  {
    try
    {
      if (body.IsInsideTree() && lastBodyParent != null && lastBodyParent.IsInsideTree())
      {
        body.Reparent(lastBodyParent, true);
      }
    }
    catch (Exception e)
    {
      GD.PrintErr($"Error restoring player parent: {e.Message}");
    }
    finally
    {
      currentBody = null;
      lastBodyParent = null;
    }
  }

  private bool IsPlayerBody(Node3D body)
  {
    if (body.HasMeta("PlayerCollider"))
    {
      return true;
    }

    return false;
  }
}