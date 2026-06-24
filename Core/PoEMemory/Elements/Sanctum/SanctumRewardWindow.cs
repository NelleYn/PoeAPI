using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.Sanctum;
public class SanctumRewardWindow : Element
{
    private readonly CachedValue<SanctumRewardWindowOffsets> _cachedValue;
    public List<Element> RewardElements => (List<Element>)(object)this;
}