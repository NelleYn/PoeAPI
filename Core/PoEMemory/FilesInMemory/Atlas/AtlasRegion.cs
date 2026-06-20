namespace ExileCore.PoEMemory.FilesInMemory.Atlas;

/// <summary>
/// A single atlas region record.
/// </summary>
public class AtlasRegion : RemoteMemoryObject
{
    /// <summary>The region's sequential index, assigned by <see cref="AtlasRegions"/>.</summary>
    public int Index { get; internal set; }

    /// <summary>The region's internal id.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address));

    /// <summary>The region's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>An unknown field.</summary>
    public int Unknown => M.Read<int>(Address + 0x10);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
