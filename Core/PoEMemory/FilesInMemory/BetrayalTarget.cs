using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// A single betrayal (Immortal Syndicate) target record, resolving its associated monster variety.
/// </summary>
public class BetrayalTarget : RemoteMemoryObject
{
    /// <summary>The target's internal id.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address));

    /// <summary>The monster variety associated with this target.</summary>
    public MonsterVariety MonsterVariety => TheGame.Files.MonsterVarieties.GetByAddress(M.Read<long>(Address + 0x20));

    /// <summary>The target's art path.</summary>
    public string Art => M.ReadStringU(M.Read<long>(Address + 0x38));

    /// <summary>The target's full name.</summary>
    public string FullName => M.ReadStringU(M.Read<long>(Address + 0x51));

    /// <summary>The target's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 0x61));

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
