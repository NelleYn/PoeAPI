namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory hideout record, exposing its name and the world areas it maps to.
/// </summary>
public class HideoutWrapper : RemoteMemoryObject
{
    /// <summary>Gets the hideout's name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the first world area associated with this hideout.</summary>
    public WorldArea WorldArea1 => TheGame.Files.WorldAreas.GetByAddress(M.Read<long>(Address + 0x10));

    /// <summary>Gets the second world area associated with this hideout.</summary>
    public WorldArea WorldArea2 => TheGame.Files.WorldAreas.GetByAddress(M.Read<long>(Address + 0x30));

    /// <summary>Gets the third world area associated with this hideout.</summary>
    public WorldArea WorldArea3 => TheGame.Files.WorldAreas.GetByAddress(M.Read<long>(Address + 0x40));
}
