using System;
using System.IO;
using Godot;


public partial class JsonSave : Node3D
{
    protected string saveLoc = ProjectSettings.GlobalizePath("user://Saves/");

    protected void fun_SaveData(string path, string fileName, Godot.Collections.Dictionary data)
    {
        string json = Json.Stringify(data, "\t");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        path = Path.Join(path, fileName);

        try
        {
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            GD.Print(e);
        }
    }

    protected Godot.Collections.Dictionary fun_LoadData(string path, string fileName)
    {
        string json = null;

        path = Path.Join(path, fileName);

        if (!File.Exists(path))
            return null;

        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            GD.Print(e);
        }

        Json j = new Json();
        j.Parse(json);

        return (Godot.Collections.Dictionary)j.Data;
    }

    protected void fun_DeleteData(string path, string fileName)
    {
        path = Path.Join(path, fileName);

        if (File.Exists(path))
            File.Delete(path);
    }
}

