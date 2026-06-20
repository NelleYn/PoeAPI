using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the destination world area of a portal.
/// </summary>
public class Portal : Component
{
    /// <summary>Gets the world area this portal leads to.</summary>
    public WorldArea Area => TheGame.Files.WorldAreas.GetByAddress(M.Read<long>(Address + 0x30));
}
