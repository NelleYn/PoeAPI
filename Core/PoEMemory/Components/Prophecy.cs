using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the prophecy data record associated with an entity.
/// </summary>
public class Prophecy : Component
{
    /// <summary>Gets the prophecy data record backing this component.</summary>
    public ProphecyDat DatProphecy => TheGame.Files.Prophecies.GetByAddress(M.Read<long>(Address + 0x20));
}
