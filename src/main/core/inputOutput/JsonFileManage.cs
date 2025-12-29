using System;
using System.IO;
using Godot;
using Godot.Collections;


public class JsonSave {
    private string saveLoc = ProjectSettings.GlobalizePath("user://Saves/");

    /// <summary>
    /// Creates a file at the default save location with the provided name and
    /// data.Note, you can only replace, not update. idk why.
    /// </summary>
    /// <param name="fileName">The exact name of the file to create/update.
    /// </param>
    /// <param name="data">the Godot.Collections.Dictionary data to be parsed.
    /// </param>
    public void fun_CreateFile(string fileName, Dictionary data) {
        string json = Json.Stringify(data, "\t");
        if (!Directory.Exists(saveLoc))
            Directory.CreateDirectory(saveLoc);
        string path = Path.Join(saveLoc, fileName);

        try {
            File.WriteAllText(path, json);
        } catch (Exception e) {
            GD.PushError(e);
        }
    }

    /// <summary>
    /// Reads the data form a file that exists at the default save location.
    /// </summary>
    /// <param name="fileName">The exact name of the file to look for.</param>
    /// <returns>Godot.Collections.Dictionary data of the file.</returns>
    public Dictionary fun_ReadData(string fileName) {
        string json = null;
        string path = Path.Join(saveLoc, fileName);

        if (!File.Exists(path))
            return null;

        try {
            json = File.ReadAllText(path);
        } catch (Exception e) {
            GD.Print(e);
        }

        Json j = new Json();
        j.Parse(json);

        return j.Data.As<Dictionary>();
    }

    /// <summary>
    /// Deletes a file at the default save location
    /// </summary>
    /// <param name="fileName">The exact name of the file to delete.</param>
    public void fun_DeleteData(string fileName) {
        string path = Path.Join(saveLoc, fileName);

        if (File.Exists(path))
            File.Delete(path);
    }
}

