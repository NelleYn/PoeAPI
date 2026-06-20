namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// A single betrayal (Immortal Syndicate) job record.
/// </summary>
public class BetrayalJob : RemoteMemoryObject
{
    /// <summary>The job's internal id.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address));

    /// <summary>The job's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>The job's art path.</summary>
    public string Art => M.ReadStringU(M.Read<long>(Address + 0x20));

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
