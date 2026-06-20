using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the item entity contained within a ground/world item.
/// </summary>
public class WorldItem : Component
{
    private readonly CachedValue<Entity> _cachedValue;

    /// <summary>Initializes a new instance of the <see cref="WorldItem"/> class.</summary>
    public WorldItem()
    {
        _cachedValue = new FrameCache<Entity>(() => Address != 0 ? ReadObject<Entity>(Address + 0x28) : null);
    }

    //Size 0x28

    /// <summary>Gets the entity of the item lying on the ground.</summary>
    public Entity ItemEntity => _cachedValue.Value;
}
