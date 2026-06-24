using System;
using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.MemoryObjects.Heist;

public class HeistNpcRecord : RemoteMemoryObject
{
    private long _StatCount => Math.Max(0, Math.Min(32, M.Read<long>(Address + 0x38)));

    public string PortraitFile => M.ReadStringU(M.Read<long>(Address + 0x30));
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x6C));
    public List<StatsDat.StatRecord> Stats => GetStats(M.Read<long>(Address + 0x40));

    // Offset 0x28 is correct, but TheGame.Files.HeistJobs is not present in this fork — returns empty. Verify.
    public List<HeistJobRecord> Jobs => new List<HeistJobRecord>();

    private List<StatsDat.StatRecord> GetStats(long source)
    {
        var stats = new List<StatsDat.StatRecord>();
        if (source == 0) return stats;

        for (var i = 0; i < _StatCount; ++i, source += 0x10)
        {
            stats.Add(TheGame.Files.Stats.GetStatByAddress(M.Read<long>(source, 0x0)));
        }

        return stats;
    }

    public override string ToString() => Name;
}
