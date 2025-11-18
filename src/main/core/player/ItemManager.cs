using Godot;
using System;

public partial class ItemManager : Node3D {
    [Export] private Item[] items = [];

    /// <summary>
    /// Returns the item by its index in the item array. Can error.
    /// </summary>
    /// <param name="unlockId">The id of the item, starting at 0</param>
    /// <returns>The item if it exists</returns>
    public Item findByID(int unlockId) {
        return items[unlockId];
    }

    /// <summary>
    /// Finds and returns the first occurence of an item with the given name.
    /// It doesnt care about case, but does about spaces.
    /// </summary>
    /// <param name="name">The name to search for.</param>
    /// <returns>
    /// The first occurence of an item with the given name, or null if it finds 
    /// nothing.
    /// </returns>
    public Item findByName(string name) {
        for (int i = 0; i < items.Length; i++) {
            if(items[i].getName().ToLower().Equals(name.ToLower())) {
                return items[i];
            }
        }

        return null;
    }
}
