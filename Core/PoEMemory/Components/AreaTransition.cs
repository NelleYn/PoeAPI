using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the destination area and transition type of an area transition object.
/// </summary>
public class AreaTransition : Component
{
    /// <summary>Gets the world area id this transition leads to.</summary>
    public int WorldAreaId => M.Read<ushort>(Address + 0x28);

    /// <summary>Gets the destination world area resolved by its area id.</summary>
    public WorldArea AreaById => TheGame.Files.WorldAreas.GetAreaByAreaId(WorldAreaId);

    /// <summary>Gets the destination world area resolved by its memory address.</summary>
    public WorldArea WorldArea => TheGame.Files.WorldAreas.GetByAddress(M.Read<long>(Address + 0x48));

    /// <summary>Gets the type of this area transition.</summary>
    public AreaTransitionType TransitionType => (AreaTransitionType) M.Read<byte>(Address + 0x2A);
}
