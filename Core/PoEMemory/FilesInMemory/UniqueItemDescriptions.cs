using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;
public class UniqueItemDescriptions : UniversalFileWrapper<UniqueItemDescription>
{
    private readonly Dictionary<ItemVisualIdentity, List<UniqueItemDescription>> _visualIdentityDictionary;
    public UniqueItemDescriptions(IMemory mem, Func<long> address)
    {
    }

    protected override void EntryAdded(long addr, UniqueItemDescription entry)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public List<UniqueItemDescription> GetByVisualIdentity(ItemVisualIdentity itemVisualIdentity)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}