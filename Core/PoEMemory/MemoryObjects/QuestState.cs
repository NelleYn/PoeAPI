namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory quest state record, exposing the quest it tracks and its progress.
/// </summary>
public class QuestState : RemoteMemoryObject
{
    /// <summary>Gets the pointer to the underlying quest record.</summary>
    public long QuestPtr => M.Read<long>(Address + 0x8);

    /// <summary>Gets the quest this state belongs to.</summary>
    public Quest Quest => TheGame.Files.Quests.GetByAddress(QuestPtr);

    /// <summary>Gets the numeric quest state identifier.</summary>
    public int QuestStateId => M.Read<int>(Address + 0x10);

    /// <summary>Gets a secondary offset value used for diagnostics.</summary>
    public int TestOffset => M.Read<int>(Address + 0x14);

    /// <summary>Gets the quest state description text.</summary>
    public string QuestStateText => M.ReadStringU(M.Read<long>(Address + 0x2c));

    /// <summary>Gets the quest progress description text.</summary>
    public string QuestProgressText => M.ReadStringU(M.Read<long>(Address + 0x34));

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Id: {QuestStateId}, Quest.Id: {Quest.Id}, ProgressText {QuestProgressText}, QuestName: {Quest.Name}";
    }
}
