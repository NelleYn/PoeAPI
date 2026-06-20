namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// A single betrayal (Immortal Syndicate) rank record.
/// </summary>
public class BetrayalRank : RemoteMemoryObject
{
    /// <summary>The rank's internal id.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address));

    /// <summary>The rank's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>An unknown field.</summary>
    public int Unknown => M.Read<int>(Address + 0x10);

    /// <summary>The rank's art path.</summary>
    public string Art => M.ReadStringU(M.Read<long>(Address + 0x14));

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
