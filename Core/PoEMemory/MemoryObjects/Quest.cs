namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory quest record, exposing its id, act, name and icon.
/// </summary>
public class Quest : RemoteMemoryObject
{
    private string id;
    private string name;

    /// <summary>Gets the quest's identifier string.</summary>
    public string Id => id != null ? id : id = M.ReadStringU(M.Read<long>(Address), 255);

    /// <summary>Gets the act this quest belongs to.</summary>
    public int Act => M.Read<int>(Address + 0x8);

    /// <summary>Gets the quest's display name.</summary>
    public string Name => name != null ? name : name = M.ReadStringU(M.Read<long>(Address + 0xc));

    /// <summary>Gets the path to the quest's icon.</summary>
    public string Icon => M.ReadStringU(M.Read<long>(Address + 0x18));

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Id: {Id}, Name: {Name}";
    }
}
