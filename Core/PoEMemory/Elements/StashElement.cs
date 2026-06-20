using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the player's stash, exposing the visible tab, tab names, and all stash inventories.
/// </summary>
public class StashElement : Element
{
    private int _indexVisibleStash;

    /// <summary>
    /// Gets the total number of stash tabs.
    /// </summary>
    public long TotalStashes => StashInventoryPanel != null ? StashInventoryPanel.ChildCount : 0;

    /// <summary>
    /// Gets the stash exit button element.
    /// </summary>
    public Element ExitButton => Address != 0 ? GetObject<Element>(M.Read<long>(Address + 0x2B8)) : null;

    // Nice struct starts at 0xB80 till 0xBD0 and all are 8 byte long pointers.
    private Element StashTitlePanel => Address != 0 ? GetObject<Element>(M.Read<long>(Address + 0x2D8, 0x428)) : null;
    private Element StashInventoryPanel => Address != 0 ? GetObject<Element>(M.Read<long>(Address + 0x2D8, 0x438)) : null;

    /// <summary>
    /// Gets the "view all stashes" button element.
    /// </summary>
    public Element ViewAllStashButton => Address != 0 ? GetObject<Element>(M.Read<long>(Address + 0x2D8, 0x440)) : null;

    /// <summary>
    /// Gets the "view all stashes" panel element.
    /// </summary>
    public Element ViewAllStashPanel =>
        Address != 0 ? GetObject<Element>(M.Read<long>(Address + 0x2D8, 0x448)) : null; // going extra inside.

    //Not fixed
    /// <summary>
    /// Gets the button that scrolls the stash tab labels left.
    /// </summary>
    public Element MoveStashTabLabelsLeft_Button => Address != 0 ? GetObject<Element>(M.Read<long>(Address + 0x2D8, 0x450)) : null;

    /// <summary>
    /// Gets the button that scrolls the stash tab labels right.
    /// </summary>
    public Element MoveStashTabLabelsRight_Button => Address != 0 ? GetObject<Element>(M.Read<long>(Address + 0x2D8, 0x458)) : null;

    /// <summary>
    /// Gets the index of the currently visible stash tab.
    /// </summary>
    public int IndexVisibleStash => M.Read<int>(Address + 0x2D8, 0x480);

    /// <summary>
    /// Gets the inventory of the currently visible stash tab.
    /// </summary>
    public Inventory VisibleStash => GetVisibleStash();

    /// <summary>
    /// Gets the names of all stash tabs.
    /// </summary>
    public IList<string> AllStashNames => GetAllStashNames();

    /// <summary>
    /// Gets the inventories of all stash tabs.
    /// </summary>
    public IList<Inventory> AllInventories => GetAllInventories();

    private Inventory GetVisibleStash()
    {
        return GetStashInventoryByIndex(IndexVisibleStash);
    }

    private List<string> GetAllStashNames()
    {
        var ret = new List<string>();

        for (var i = 0; i < TotalStashes; i++)
        {
            ret.Add(GetStashName(i));
        }

        return ret;
    }

    private IList<Inventory> GetAllInventories()
    {
        var result = new List<Inventory>();

        for (var i = 0; i < TotalStashes; i++)
        {
            result.Add(GetStashInventoryByIndex(i));
        }

        return result;
    }

    /// <summary>
    /// Gets the inventory for the stash tab at the specified index, or <c>null</c> when unavailable.
    /// </summary>
    /// <param name="index">The zero-based stash tab index.</param>
    public Inventory GetStashInventoryByIndex(int index) //This one is correct
    {
        if (index >= TotalStashes)
            return null;

        if (StashInventoryPanel.Children[index].ChildCount == 0)
            return null;

        Inventory stashInventoryByIndex = null;

        try
        {
            stashInventoryByIndex = StashInventoryPanel.Children[index].Children[0].Children[0].AsObject<Inventory>();
        }
        catch (Exception e)
        {
            DebugWindow.LogError($"Not found inventory stash for index: {index}");
        }

        return stashInventoryByIndex;
    }

    /// <summary>
    /// Gets the name of the stash tab at the specified index, or <see cref="string.Empty"/> when out of range.
    /// </summary>
    /// <param name="index">The zero-based stash tab index.</param>
    public string GetStashName(int index)
    {
        if (index >= TotalStashes || index < 0)
            return string.Empty;

        var temp = ViewAllStashPanel.Children.First(x => x.ChildCount >= 4)?[index];
        return temp != null ? temp[(int) temp.ChildCount - 1].Text : string.Empty;
    }
}
