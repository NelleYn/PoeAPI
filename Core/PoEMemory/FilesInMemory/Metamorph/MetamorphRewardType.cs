namespace ExileCore.PoEMemory.FilesInMemory.Metamorph;

/// <summary>
/// A single metamorph reward type record.
/// </summary>
public class MetamorphRewardType : RemoteMemoryObject
{
    /// <summary>The reward type's internal id.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address + 0x0), 255);

    /// <summary>The reward type's art path.</summary>
    public string Art => M.ReadStringU(M.Read<long>(Address + 0x8), 255);

    /// <summary>The reward type's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x10), 255);

    //0x18 UINT unknown
    //0x20 ptr -> ptr[0x8] achievement

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Id}: {Name}";
    }
}
