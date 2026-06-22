using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an entity's local (item/skill-scoped) stats as a dictionary keyed by
/// <see cref="GameStat"/>. Stats are stored as a contiguous array of (id, value) pairs.
/// </summary>
public class LocalStats : Component
{
    private readonly CachedValue<LocalStatsComponentOffsets> _localStatsValue;
    private readonly CachedValue<Dictionary<GameStat, int>> _statDictionary;
    private Dictionary<GameStat, int> statDictionary = new Dictionary<GameStat, int>();

    /// <summary>Initializes a new instance of the <see cref="LocalStats"/> class.</summary>
    public LocalStats()
    {
        _localStatsValue = new FrameCache<LocalStatsComponentOffsets>(() => M.Read<LocalStatsComponentOffsets>(Address));
        _statDictionary = new FrameCache<Dictionary<GameStat, int>>(ParseStats);
    }

    /// <summary>Gets the raw local-stats offsets struct (cached per frame).</summary>
    public LocalStatsComponentOffsets LocalStatsStruct => _localStatsValue.Value;

    /// <summary>Gets the parsed stat dictionary keyed by <see cref="GameStat"/>.</summary>
    public Dictionary<GameStat, int> StatDictionary => _statDictionary.Value;

    /// <summary>Reads and parses the entity's local stats from memory.</summary>
    public Dictionary<GameStat, int> ParseStats()
    {
        if (Address == 0) return statDictionary;

        var statsVec = LocalStatsStruct.StatsPtr;
        var statPtrStart = statsVec.First;
        var totalStats = statsVec.Last - statPtrStart;
        var maxStats = statsVec.End - statPtrStart;

        if (statsVec.Size <= 0 || statPtrStart == 0 || totalStats <= 0 || totalStats > maxStats || maxStats > 9000)
            return statDictionary;

        var bytes = M.ReadMem(statPtrStart, (int) totalStats);
        if (statDictionary.Count == 0) statDictionary = new Dictionary<GameStat, int>((int) (maxStats / 8));
        statDictionary.Clear();

        for (var i = 0; i < bytes.Length - 0x04; i += 8)
        {
            var key = BitConverter.ToInt32(bytes, i);
            var value = BitConverter.ToInt32(bytes, i + 0x04);
            statDictionary[(GameStat) key] = value;
        }

        return statDictionary;
    }

    /// <summary>Gets the value of the given stat, or 0 if the entity does not have it.</summary>
    /// <param name="stat">The stat to look up.</param>
    /// <returns>The stat value, or 0 when absent.</returns>
    public int GetStatValue(GameStat stat)
    {
        return StatDictionary.TryGetValue(stat, out var value) ? value : 0;
    }
}
