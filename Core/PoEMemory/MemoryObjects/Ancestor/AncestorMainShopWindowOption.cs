using ExileCore.PoEMemory.FilesInMemory.Ancestor;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects.Ancestor;
public class AncestorMainShopWindowOption : Element
{
    private readonly CachedValue<AncestorShopWindowOffsets> _cachedValue;
    public AncestralTrialUnit Unit => (AncestralTrialUnit)(object)this;
    public AncestralTrialItem Item => (AncestralTrialItem)(object)this;
}