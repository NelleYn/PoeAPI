namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory Bestiary family record, exposing its identifiers, art and description.
/// </summary>
public class BestiaryFamily : RemoteMemoryObject
{
    private string familyId;
    private string name;

    /// <summary>Gets or sets the row index of this family.</summary>
    public int Id { get; internal set; }

    /// <summary>Gets the family's identifier string.</summary>
    public string FamilyId => familyId != null ? familyId : familyId = M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the family's display name.</summary>
    public string Name => name != null ? name : name = M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>Gets the path to the family's icon.</summary>
    public string Icon => M.ReadStringU(M.Read<long>(Address + 0x10));

    /// <summary>Gets the path to the family's small icon.</summary>
    public string SmallIcon => M.ReadStringU(M.Read<long>(Address + 0x18));

    /// <summary>Gets the path to the family's illustration.</summary>
    public string Illustration => M.ReadStringU(M.Read<long>(Address + 0x20));

    /// <summary>Gets the path to the family's page art.</summary>
    public string PageArt => M.ReadStringU(M.Read<long>(Address + 0x28));

    /// <summary>Gets the family's description text.</summary>
    public string Description => M.ReadStringU(M.Read<long>(Address + 0x30));

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
