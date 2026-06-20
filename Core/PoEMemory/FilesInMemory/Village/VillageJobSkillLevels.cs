using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory.Village;
public class VillageJobSkillLevels : UniversalFileWrapper<VillageJobSkillLevel>
{
    private readonly Dictionary<int, VillageJobSkillLevel> _bySkillLevel;
    protected override int? ArrayPointerStride => (int? )(object)16;

    public VillageJobSkillLevels(IMemory mem, Func<long> address)
    {
    }

    protected override void EntryAdded(long addr, VillageJobSkillLevel entry)
    {
    }

    public VillageJobSkillLevel GetByLevel(int level)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    protected override void OnReload()
    {
    }
}