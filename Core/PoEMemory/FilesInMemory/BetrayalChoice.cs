namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// A single betrayal (Immortal Syndicate) choice record.
/// </summary>
public class BetrayalChoice : RemoteMemoryObject
{
    /// <summary>The choice's internal id.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address));

    /// <summary>The choice's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
