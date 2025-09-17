using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GeometryLoader : Node3D
{
	[Export] Node3D player;
	List<RegionData> region = new List<RegionData>();
	List<Area3D> area = new List<Area3D>();

	RigidBody3D p;

    public override void _EnterTree()
    {
        base._EnterTree();

		p = player.GetChild(1).GetChild(2) as RigidBody3D;
		ref_getRegions();
		ref_getAreas();

        foreach(Area3D a in area)
        {
            a.BodyEntered += fun_RecalcRegions;
            a.BodyExited += fun_RecalcRegions;
        }
    }

	void fun_RecalcRegions(Node3D body)
	{
        if (body != p) return;

        foreach (RegionData r in region)
            r.region.Visible = false;

        for (int i = 0; i < region.Count; i++)
        {
            if (area[i].OverlapsBody(p))
            {
                region[i].region.Visible = true;

                foreach (RegionData n in region[i].AdjacentRooms)
                     n.region.Visible = true;
            }
        }
    }

	void ref_getRegions()
	{
        for (int i = 1; i < GetParent().GetChildCount(); i++)
			region.Add(GetParent().GetChild(i) as RegionData);
    }

	void ref_getAreas()
	{
		foreach (Node3D i in region)
			area.Add(i.GetChild(0) as Area3D);
	}
}
