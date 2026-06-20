using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory.Village;
public class VillageJobs : UniversalFileWrapper<VillageJob>
{
    private readonly Dictionary<string, VillageJob> _byName;
    public VillageJobs(IMemory mem, Func<long> address)
    {
    }

    protected override void EntryAdded(long addr, VillageJob entry)
    {
    }

    protected override void OnReload()
    {
    }

    public VillageJob GetByName(string name)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}