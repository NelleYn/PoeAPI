using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class GameConfig : StructuredRemoteMemoryObject<GameConfigOffsets>
{
    private readonly CachedValue<Dictionary<string, GameConfigSection>> _mapCache;
    public Dictionary<string, GameConfigSection> ConfigMap => (Dictionary<string, GameConfigSection>)(object)this;
}