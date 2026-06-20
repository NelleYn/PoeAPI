namespace ExileCore.PoEMemory.FilesInMemory.Metamorph;

/// <summary>
/// A single metamorph reward type items client record, resolving its associated reward type.
/// </summary>
public class MetamorphRewardTypeItemsClient : RemoteMemoryObject
{
    /// <summary>The reward type associated with this record.</summary>
    public MetamorphRewardType RewardType => TheGame.Files.MetamorphRewardTypes.GetByAddress(M.Read<long>(Address + 0x8));

    /// <summary>An unknown field.</summary>
    public int Unknown => M.Read<int>(Address + 0x10);

    /// <summary>The reward description.</summary>
    public string Description => M.ReadStringU(M.Read<long>(Address + 0x14), 255);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{RewardType.Id}: {Description}";
    }
}
