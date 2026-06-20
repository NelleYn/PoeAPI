namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory Vaal skill record of an actor skill, exposing its names, icon and soul counts.
/// </summary>
public class ActorVaalSkill : RemoteMemoryObject
{
    private const int NAMES_POINTER_OFFSET = 0x8;
    private const int INTERNAL_NAME_OFFSET = 0x0;
    private const int NAME_OFFSET = 0x8;
    private const int DESCRIPTION_OFFSET = 0x10;
    private const int SKILL_NAME_OFFSET = 0x18;
    private const int ICON_OFFSET = 0x20;
    private const int MAX_VAAL_SOULS_OFFSET = 0x10;
    private const int VAAL_SOULS_PER_USE_OFFSET = 0x14;
    private const int CURRENT_VAAL_SOULS_OFFSET = 0x18;

    /// <summary>Gets the internal name of the Vaal skill.</summary>
    public string VaalSkillInternalName => M.ReadStringU(M.Read<long>(NAMES_POINTER_OFFSET) + INTERNAL_NAME_OFFSET);

    /// <summary>Gets the display name of the Vaal skill.</summary>
    public string VaalSkillDisplayName => M.ReadStringU(M.Read<long>(NAMES_POINTER_OFFSET) + NAME_OFFSET);

    /// <summary>Gets the description of the Vaal skill.</summary>
    public string VaalSkillDescription => M.ReadStringU(M.Read<long>(NAMES_POINTER_OFFSET) + DESCRIPTION_OFFSET);

    /// <summary>Gets the skill name of the Vaal skill.</summary>
    public string VaalSkillSkillName => M.ReadStringU(M.Read<long>(NAMES_POINTER_OFFSET) + SKILL_NAME_OFFSET);

    /// <summary>Gets the path to the Vaal skill's icon.</summary>
    public string VaalSkillIcon => M.ReadStringU(M.Read<long>(NAMES_POINTER_OFFSET) + ICON_OFFSET);

    /// <summary>Gets the maximum number of Vaal souls the skill can store.</summary>
    public int VaalMaxSouls => M.Read<int>(Address + MAX_VAAL_SOULS_OFFSET);

    /// <summary>Gets the number of Vaal souls consumed per use.</summary>
    public int VaalSoulsPerUse => M.Read<int>(Address + VAAL_SOULS_PER_USE_OFFSET);

    /// <summary>Gets the current number of stored Vaal souls.</summary>
    public int CurrVaalSouls => M.Read<int>(Address + CURRENT_VAAL_SOULS_OFFSET);
}
