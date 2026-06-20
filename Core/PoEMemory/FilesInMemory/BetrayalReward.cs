namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// A single betrayal (Immortal Syndicate) reward record, resolving its associated job, target, and rank.
/// </summary>
public class BetrayalReward : RemoteMemoryObject
{
    /// <summary>The job associated with this reward.</summary>
    public BetrayalJob Job => TheGame.Files.BetrayalJobs.GetByAddress(M.Read<long>(Address + 0x8));

    /// <summary>The target associated with this reward.</summary>
    public BetrayalTarget Target => TheGame.Files.BetrayalTargets.GetByAddress(M.Read<long>(Address + 0x18));

    /// <summary>The rank associated with this reward.</summary>
    public BetrayalRank Rank => TheGame.Files.BetrayalRanks.GetByAddress(M.Read<long>(Address + 0x28));

    /// <summary>The reward description.</summary>
    public string Reward => M.ReadStringU(M.Read<long>(Address + 0x30));

    /// <inheritdoc />
    public override string ToString()
    {
        return Reward;
    }
}
