using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component that exposes the base animated object entity backing an animated in-game object.
/// </summary>
public class Animated : Component
{
    /// <summary>Gets the entity that represents the base animated object.</summary>
    public Entity BaseAnimatedObjectEntity => GetObject<Entity>(M.Read<long>(Address + 0x78));
}
