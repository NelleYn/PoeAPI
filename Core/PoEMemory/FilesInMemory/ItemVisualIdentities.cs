using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;
public class ItemVisualIdentities : UniversalFileWrapper<ItemVisualIdentity>
{
    private readonly Dictionary<string, List<ItemVisualIdentity>> _artPathDictionary;
    public ItemVisualIdentities(IMemory mem, Func<long> address)
    {
    }

    protected override void EntryAdded(long addr, ItemVisualIdentity entry)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public List<ItemVisualIdentity> GetByArtPath(string artPath)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}