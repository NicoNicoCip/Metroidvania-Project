using System;
using System.Collections.Generic;
using Godot;

public partial class TimeSave : JsonSave
{
    Godot.Collections.Dictionary data;
    PackedScene s = new PackedScene();
    Variant v;

    public void fun_SaveTimePoint()
    {
        s.Pack(GetTree().CurrentScene.FindChild("Player"));

        data.TryAdd("Player", s);
        fun_SaveData(saveLoc, "timeSave", data);
    }

    public void fun_LoadTimePoint()
    {
        data = fun_LoadData(saveLoc, "timeSave");
        GetTree().CurrentScene.FindChild("Player").Free();
        Node p = ((PackedScene)v).Instantiate();
        GetTree().CurrentScene.AddChild(p);
    }
}