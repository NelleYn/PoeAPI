namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory Bestiary genus record, exposing its identifiers, art and parent group.
/// </summary>
public class BestiaryGenus : RemoteMemoryObject
{
    private BestiaryGroup bestiaryGroup;
    private string genusId;
    private string icon;
    private string name;
    private string name2;

    /// <summary>Gets or sets the row index of this genus.</summary>
    public int Id { get; internal set; }

    /// <summary>Gets the genus's identifier string.</summary>
    public string GenusId => genusId != null ? genusId : genusId = M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the genus's display name.</summary>
    public string Name => name != null ? name : name = M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>Gets the Bestiary group this genus belongs to.</summary>
    public BestiaryGroup BestiaryGroup =>
        bestiaryGroup != null ? bestiaryGroup : bestiaryGroup = TheGame.Files.BestiaryGroups.GetByAddress(M.Read<long>(Address + 0x18));

    /// <summary>Gets the genus's secondary name.</summary>
    public string Name2 => name2 != null ? name2 : name2 = M.ReadStringU(M.Read<long>(Address + 0x20));

    /// <summary>Gets the path to the genus's icon.</summary>
    public string Icon => icon != null ? icon : icon = M.ReadStringU(M.Read<long>(Address + 0x28));

    /// <summary>Gets the maximum number of this genus that can be stored.</summary>
    public int MaxInStorage => M.Read<int>(Address + 0x30);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name}, MaxInStorage: {MaxInStorage}";
    }
}
