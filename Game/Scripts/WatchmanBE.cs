using Godot;
using System;

public partial class WatchmanBE : Node3D
{
	ShapeCast3D scc;
	Master master;

	bool col;

    public override void _EnterTree()
    {
        base._EnterTree();
        scc = GetChild(0) as ShapeCast3D;
		master = GetTree().CurrentScene as Master;
	}

	public override void _Process(double delta)
	{
		scc.ForceShapecastUpdate();

		if (scc.IsColliding()) master.playerHasWatchman = true;

		if (master.playerHasWatchman) GetParent().Free();
    }
}
