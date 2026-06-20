namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing a single skill slot on the skill bar.
/// </summary>
public class SkillElement : Element
{
    /// <summary>
    /// Gets a value indicating whether the skill slot points at valid skill data.
    /// </summary>
    public bool isValid => unknown1 != 0;

    /// <summary>
    /// Gets a value indicating whether the skill is assigned to a key or currently active.
    /// Useful for auras/golems: their value is <c>true</c> while active or bound to a key.
    /// </summary>
    public bool IsAssignedKeyOrIsActive => M.Read<int>(unknown1 + 0x08) > 3;

    /// <summary>
    /// Gets the path of the skill icon. (The skill path itself could not be located.)
    /// </summary>
    public string SkillIconPath => M.ReadStringU(M.Read<long>(unknown1 + 0x10), 100).TrimEnd('0');

    /// <summary>
    /// Gets the number of times the skill has been used; resets on area change.
    /// </summary>
    public int totalUses => M.Read<int>(unknown3 + 0x50);

    /// <summary>
    /// Gets a value indicating whether the skill is currently being used. Useful for channelling skills only.
    /// </summary>
    public bool isUsing => M.Read<byte>(unknown3 + 0x08) > 2;

    // Backing pointers whose exact meaning in memory is unknown.
    private long unknown1 => M.Read<long>(Address + OffsetBuffers + 0x244);
    private long unknown3 => M.Read<long>(Address + OffsetBuffers + 0x32C);
}
