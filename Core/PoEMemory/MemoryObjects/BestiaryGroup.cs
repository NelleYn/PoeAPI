namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory Bestiary group record, exposing its identifiers, art and parent family.
/// </summary>
public class BestiaryGroup : RemoteMemoryObject
{
    private BestiaryFamily family;
    private string groupId;
    private string name;

    /// <summary>Gets or sets the row index of this group.</summary>
    public int Id { get; internal set; }

    /// <summary>Gets the group's identifier string.</summary>
    public string GroupId => groupId != null ? groupId : groupId = M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the group's description text.</summary>
    public string Description => M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>Gets the path to the group's illustration.</summary>
    public string Illustration => M.ReadStringU(M.Read<long>(Address + 0x10));

    /// <summary>Gets the group's display name.</summary>
    public string Name => name != null ? name : name = M.ReadStringU(M.Read<long>(Address + 0x18));

    /// <summary>Gets the path to the group's small icon.</summary>
    public string SmallIcon => M.ReadStringU(M.Read<long>(Address + 0x20));

    /// <summary>Gets the path to the group's item icon.</summary>
    public string ItemIcon => M.ReadStringU(M.Read<long>(Address + 0x28));

    /// <summary>Gets the Bestiary family this group belongs to.</summary>
    public BestiaryFamily Family =>
        family != null ? family : family = TheGame.Files.BestiaryFamilies.GetByAddress(M.Read<long>(Address + 0x38));

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
