using Godot;
using System;

[GlobalClass]
public partial class Item : Resource {
    [Export] private string name = "name";
    [Export] private bool unlocked = false;

    public string getName() {
        return name;
    }

    public bool isUnlocked() {
        return unlocked;
    }

    public void setName(string name) {
        this.name = name;
    }

    public void setUnlocked(bool unlocked) {
        this.unlocked = unlocked;
    }
}
