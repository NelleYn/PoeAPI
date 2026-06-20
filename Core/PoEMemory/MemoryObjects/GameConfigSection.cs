using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class GameConfigSection : StructuredRemoteMemoryObject<GameConfigSectionOffsets>
{
    private readonly CachedValue<Dictionary<string, string>> _configCache;
    public string SectionKey => (string)(object)this;
    public Dictionary<string, string> SectionConfig => (Dictionary<string, string>)(object)this;
}