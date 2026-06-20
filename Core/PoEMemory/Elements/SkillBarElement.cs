namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the player's skill bar.
/// </summary>
public class SkillBarElement : Element
{
    /// <summary>
    /// Gets the total number of skill slots on the bar.
    /// </summary>
    public long TotalSkills => ChildCount;

    /// <summary>
    /// Gets the <see cref="SkillElement"/> at the specified slot index.
    /// </summary>
    /// <param name="k">The zero-based slot index.</param>
    public SkillElement this[int k] => Children[k].AsObject<SkillElement>();
}
