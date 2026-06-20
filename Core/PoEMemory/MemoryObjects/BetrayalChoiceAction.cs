using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory Betrayal choice action, exposing its id and the choice it resolves to.
/// </summary>
public class BetrayalChoiceAction : RemoteMemoryObject
{
    /// <summary>Gets the choice action's identifier string.</summary>
    public string Id => M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the Betrayal choice this action corresponds to.</summary>
    public BetrayalChoice Choice => TheGame.Files.BetrayalChoises.GetByAddress(M.Read<long>(Address + 0x10));

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Id} ({Choice.Name})";
    }
}
