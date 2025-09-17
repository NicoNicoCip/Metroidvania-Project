using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GeometryLoader : Node3D
{
	[Export] Node3D player;
	List<RegionData> region = new List<RegionData>();
	List<Area3D> area = new List<Area3D>();

	RigidBody3D rig;

    public override void _EnterTree()
    {
        base._EnterTree();

        // assign the rig og the player's body to a local variable
        rig = player.GetChild(1).GetChild(2) as RigidBody3D;

        // get the regions and areas
		ref_getRegions();
		ref_getAreas();

        // add the callbacks for the areas.
        foreach (Area3D a in area)
        {
            a.BodyEntered += fun_RecalcRegions;
            a.BodyExited += fun_RecalcRegions;
        }
    }

    // recalculate the regions if the rigidBody that entered 
    // (auto passed by the callback) is the player, load and
    // unload areas.
    void fun_RecalcRegions(Node3D body)
    {
        // if there is no rig, return immediately.
        if (rig == null) return;


        // if the body that entered is not the player return
        if (body != rig) return;

        // hide all region data 
        foreach (RegionData r in region)
            r.region.Visible = false;

        // calculate which regions to set visible.
        for (int i = 0; i < region.Count; i++)
        {
            if (area[i].OverlapsBody(rig))
            {
                region[i].region.Visible = true;
                foreach (RegionData neighbour in region[i].AdjacentRooms.Cast<RegionData>())
                    neighbour.region.Visible = true;
            }
        }
        
        // TODO: THIS IS SHIT. FIX IT.
    }

    // get the regions; uses normal for loop for some reason
    void ref_getRegions()
	{
        for (int i = 1; i < GetParent().GetChildCount(); i++)
			region.Add(GetParent().GetChild(i) as RegionData);
    }

    // get the areas, uses foreach for some reason.
	void ref_getAreas()
	{
		foreach (Node3D i in region)
			area.Add(i.GetChild(0) as Area3D);
	}
}
