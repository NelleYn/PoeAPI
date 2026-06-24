using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class HarvestWorldObject : Component
{
    private readonly CachedValue<HarvestWorldObjectComponentOffsets> _cacheValue;
    public List<HarvestSeedSpawnDescriptor> Seeds => (List<HarvestSeedSpawnDescriptor>)24;
}