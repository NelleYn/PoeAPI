using System;
using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;
public class StashTabContainer : Element
{
    private readonly CachedValue<StashTabContainerOffsets> _cache;
    private readonly CachedValue<List<StashTabContainerInventory>> _inventoriesCache;
    public int VisibleStashIndex => (int)this;

    public Element StashInventoryPanel
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element ViewAllStashPanel
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public long TotalStashes
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element ViewAllStashesButton => this;
    public Element PinStashTabListButton => this;
    public StashTopTabSwitcher TabSwitchBar => (StashTopTabSwitcher)(object)this;

    public Inventory VisibleStash
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    [Obsolete("Just use Inventories")]
    public IList<Inventory> AllInventories
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    [Obsolete("Just use Inventories")]
    public IList<string> AllStashNames
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public IList<Element> ViewAllStashPanelChildren
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public IList<Element> TabListButtons
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public virtual List<StashTabContainerInventory> Inventories => (List<StashTabContainerInventory>)(object)this;

    [Obsolete("Just use Inventories")]
    public string GetStashName(int index)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public Inventory GetStashInventoryByIndex(int index)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}