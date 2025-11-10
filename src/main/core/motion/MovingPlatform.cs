using Godot;
using System;

/// <summary>
/// Attach this to any AnimatableBody3D platform that moves using 
/// AnimationPlayer.
/// It computes its linear velocity so MotionController can inherit its 
/// movement.
/// </summary>
public partial class MovingPlatform : AnimatableBody3D
{
	private Vector3 lastPosition;
	private Vector3 platformVelocity = Vector3.Zero;

	public override void _Ready()
	{
		lastPosition = GlobalTransform.Origin;
		SetMeta("platform_velocity", Vector3.Zero);
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 currentPos = GlobalTransform.Origin;
		platformVelocity = (currentPos - lastPosition) / (float)delta;
		lastPosition = currentPos;

		// Expose velocity to MotionController
		SetMeta("platform_velocity", platformVelocity);
	}

	// For MotionController to query directly
	public Vector3 GetPlatformVelocity() => platformVelocity;
}
