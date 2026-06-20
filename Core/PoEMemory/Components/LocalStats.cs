using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class LocalStats : Component
{
    private readonly CachedValue<LocalStatsComponentOffsets> _localStatsValue;
    private readonly CachedValue<Dictionary<GameStat, int>> _statDictionary;
    private readonly Dictionary<GameStat, int> statDictionary;
    public Dictionary<GameStat, int> StatDictionary => (Dictionary<GameStat, int>)(object)this;

    public Dictionary<GameStat, int> ParseStats()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public int GetStatValue(GameStat stat)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}