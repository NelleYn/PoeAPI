namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory skill gem record, exposing its name and active skill.
/// </summary>
public class SkillGemWrapper : RemoteMemoryObject
{
    /// <summary>Gets the skill gem's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the active skill associated with this gem.</summary>
    public ActiveSkillWrapper ActiveSkill => ReadObject<ActiveSkillWrapper>(Address + 0x73);
}
