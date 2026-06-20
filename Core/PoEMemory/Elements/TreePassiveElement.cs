using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;
public class TreePassiveElement : Element
{
    private readonly CachedValue<TreePassiveElementOffsets> _cache;
    public PassiveSkill PassiveSkill => (PassiveSkill)(this + 8);
    public bool IsAllocatedForPlan => this != null;
    public bool CanAllocate => this != null;
    public bool CanDeallocate => this != null;
}