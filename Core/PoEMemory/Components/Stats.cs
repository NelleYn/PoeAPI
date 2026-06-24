using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an entity's game stats as a dictionary keyed by <see cref="GameStat"/>.
/// </summary>
public class Stats : Component
{
    private readonly CachedValue<StatsComponentOffsets> _cachedValue;
    private readonly CachedValue<Dictionary<GameStat, int>> _statDictionary;
    private readonly Dictionary<string, int> testHumanDictionary = new Dictionary<string, int>();
    private Dictionary<GameStat, int> testStatDictionary = new Dictionary<GameStat, int>();

    /// <summary>Initializes a new instance of the <see cref="Stats"/> class.</summary>
    public Stats()
    {
        _cachedValue = new FrameCache<StatsComponentOffsets>(() => M.Read<StatsComponentOffsets>(Address));
        _statDictionary = new FrameCache<Dictionary<GameStat, int>>(ParseStats);
    }

    /// <summary>Gets the address of the entity that owns this component.</summary>
    public new long OwnerAddress => StatsComponent.Owner;

    /// <summary>Gets the raw stats offsets struct (cached per frame).</summary>
    public StatsComponentOffsets StatsComponent => _cachedValue.Value;

    //Stats goes as sequence of 2 values, 4 byte each. First goes stat ID then goes stat value

    /// <summary>Gets the parsed stat dictionary keyed by <see cref="GameStat"/>.</summary>
    public Dictionary<GameStat, int> StatDictionary => _statDictionary.Value;

    /// <summary>Gets the contiguous (id, value) stat vector, reached via the sub-stats pointer (328.8).</summary>
    private NativePtrArray StatsArray
    {
        get
        {
            var subPtr = StatsComponent.SubStatsPtr;
            return subPtr == 0 ? default : M.Read<SubStatsComponentOffsets>(subPtr).Stats;
        }
    }

    /// <summary>Gets the number of stats on the entity.</summary>
    public long StatsCount => (StatsArray.Last - StatsArray.First) / 8;

    /// <summary>Reads and parses the entity's stats from memory.</summary>
    public Dictionary<GameStat, int> ParseStats()
    {
        if (Address == 0) return testStatDictionary;

        var statsVec = StatsArray;
        var statPtrStart = statsVec.First;
        var statPtrLast = statsVec.Last;
        var statPtrEnd = statsVec.End;
        if (statsVec.Size <= 0) return testStatDictionary;
        var key = 0;
        var value = 0;
        var total_stats = statPtrLast - statPtrStart;
        var max_stats = statPtrEnd - statPtrStart;
        if (total_stats > max_stats || statPtrStart == 0) return testStatDictionary;
        var bytes = M.ReadMem(statPtrStart, (int) total_stats);
        var capacity = max_stats / 8;

        if (max_stats > 9000)
        {
            Core.Logger.Error(
                $"Stats over capped: {statsVec} Total Stats: {total_stats} Max Stats: {max_stats}");

            return testStatDictionary;
        }

        if (capacity < 0) return testStatDictionary;
        if (testStatDictionary.Count < capacity) testStatDictionary = new Dictionary<GameStat, int>((int) capacity);
        testStatDictionary.Clear();

        for (var i = 0; i < bytes.Length - 0x04; i += 8)
        {
            try
            {
                key = BitConverter.ToInt32(bytes, i);
                value = BitConverter.ToInt32(bytes, i + 0x04);
                testStatDictionary[(GameStat) key] = value;
            }
            catch (Exception e)
            {
                throw new Exception($"Stats parse {e}");
            }
        }

        return testStatDictionary;
    }

    /// <summary>Resolves the stat dictionary into a dictionary keyed by human-readable stat names.</summary>
    public Dictionary<string, int> HumanStats()
    {
        var dictionary = StatDictionary;
        testHumanDictionary.Clear();

        var stats = TheGame.Files.Stats;

        if (stats == null)
            return null;

        foreach (var d in dictionary)
        {
            if (stats.recordsById.TryGetValue((int) d.Key, out var res))
                testHumanDictionary[res.Key] = d.Value;
        }

        return testHumanDictionary;
    }
}
