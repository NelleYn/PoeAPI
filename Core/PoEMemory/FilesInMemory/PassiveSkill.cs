using System;
using System.Collections.Generic;
using System.Linq;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// A single passive skill record, lazily resolving its id, name, icon, and granted stats.
/// </summary>
public class PassiveSkill : RemoteMemoryObject
{
    private string id;
    private string name;
    private int passiveId = -1;
    private List<Tuple<StatsDat.StatRecord, int>> stats;

    /// <summary>The passive skill's numeric id, read lazily and cached.</summary>
    public int PassiveId => passiveId != -1 ? passiveId : passiveId = M.Read<int>(Address + 0x30);

    /// <summary>The passive skill's string id, read lazily and cached.</summary>
    public string Id => id != null ? id : id = M.ReadStringU(M.Read<long>(Address), 255);

    /// <summary>The passive skill's display name, read lazily and cached.</summary>
    public string Name => name != null ? name : name = M.ReadStringU(M.Read<long>(Address + 0x34), 255);

    /// <summary>The passive skill's icon path.</summary>
    public string Icon => M.ReadStringU(M.Read<long>(Address + 0x8), 255); //Read on request

    /// <summary>The stats granted by this passive skill, paired with their values; resolved lazily and cached.</summary>
    public IEnumerable<Tuple<StatsDat.StatRecord, int>> Stats
    {
        get
        {
            if (stats == null)
            {
                stats = new List<Tuple<StatsDat.StatRecord, int>>();

                var statsCount = M.Read<int>(Address + 0x10);
                var pointerToStats = M.Read<long>(Address + 0x18);
                var statsPointers = M.ReadSecondPointerArray_Count(pointerToStats, statsCount);

                stats = statsPointers.Select((x, i) =>
                    new Tuple<StatsDat.StatRecord, int>(
                        TheGame.Files.Stats.GetStatByAddress(x), ReadStatValue(i))).ToList();
            }

            return stats;
        }
    }

    /// <summary>Reads the stat value at the given index relative to this record.</summary>
    /// <param name="index">The zero-based stat index.</param>
    internal int ReadStatValue(int index)
    {
        return M.Read<int>(Address + 0x20 + index * 4);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name}, Id: {Id}, PassiveId: {PassiveId}";
    }
}
