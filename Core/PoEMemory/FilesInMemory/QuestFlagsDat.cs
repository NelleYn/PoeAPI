using System;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;
public class QuestFlagsDat : UniversalFileWrapper<QuestFlagDat>
{
    private int _index;
    public QuestFlagsDat(IMemory mem, Func<long> address)
    {
    }

    protected override void EntryAdded(long addr, QuestFlagDat entry)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    protected override void OnReload()
    {
        _ = 0;
    }
}