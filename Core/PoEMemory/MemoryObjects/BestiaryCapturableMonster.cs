namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory Bestiary capturable monster record, exposing its variety, taxonomy and capture count.
/// </summary>
public class BestiaryCapturableMonster : RemoteMemoryObject
{
    private BestiaryCapturableMonster bestiaryCapturableMonsterKey;
    private BestiaryGenus bestiaryGenus;
    private BestiaryGroup bestiaryGroup;
    private string monsterName;
    private MonsterVariety monsterVariety;

    /// <summary>Gets or sets the row index of this capturable monster.</summary>
    public int Id { get; set; }

    /// <summary>Gets the monster's display name.</summary>
    public string MonsterName => monsterName != null ? monsterName : monsterName = M.ReadStringU(M.Read<long>(Address + 0x20));

    /// <summary>Gets the monster variety associated with this capturable monster.</summary>
    public MonsterVariety MonsterVariety =>
        monsterVariety != null
            ? monsterVariety
            : monsterVariety = TheGame.Files.MonsterVarieties.GetByAddress(M.Read<long>(Address + 0x8));

    /// <summary>Gets the Bestiary group this monster belongs to.</summary>
    public BestiaryGroup BestiaryGroup =>
        bestiaryGroup != null
            ? bestiaryGroup
            : bestiaryGroup = TheGame.Files.BestiaryGroups.GetByAddress(M.Read<long>(Address + 0x18));

    /// <summary>Gets the pointer to the Bestiary encounters data for this monster.</summary>
    public long BestiaryEncountersPtr => M.Read<long>(Address + 0x30);

    /// <summary>Gets the key record referencing this capturable monster.</summary>
    public BestiaryCapturableMonster BestiaryCapturableMonsterKey =>
        bestiaryCapturableMonsterKey != null
            ? bestiaryCapturableMonsterKey
            : bestiaryCapturableMonsterKey =
                TheGame.Files.BestiaryCapturableMonsters.GetByAddress(M.Read<long>(Address + 0x6a));

    /// <summary>Gets the Bestiary genus this monster belongs to.</summary>
    public BestiaryGenus BestiaryGenus =>
        bestiaryGenus != null
            ? bestiaryGenus
            : bestiaryGenus = TheGame.Files.BestiaryGenuses.GetByAddress(M.Read<long>(Address + 0x61));

    /// <summary>Gets the number of this beast currently captured by the player.</summary>
    public int AmountCaptured => TheGame.IngameState.ServerData.GetBeastCapturedAmount(this);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Nane: {MonsterName}, Group: {BestiaryGroup.Name}, Family: {BestiaryGroup.Family.Name}, Captured: {AmountCaptured}";
    }
}
