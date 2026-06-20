using System;
using System.Collections.Frozen;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;
public class MiscAnimatedList : UniversalFileWrapper<MiscAnimatedDat>
{
    private FrozenDictionary<string, MiscAnimatedDat> _entriesByAOFile;
    public MiscAnimatedList(IMemory mem, Func<long> address)
    {
    }

    public MiscAnimatedDat GetByAOFile(string aoFile)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    protected override void OnAllEntriesAdded()
    {
        while (this != null)
        {
        }

        while (this != null)
        {
        }

        while (this != null)
        {
        }

        _ = null;
    }
}