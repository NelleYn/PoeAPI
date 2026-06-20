namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the attribute requirements (strength, dexterity, intelligence) of an item.
/// </summary>
public class AttributeRequirements : Component
{
    /// <summary>Gets the required strength.</summary>
    public int strength => Address != 0 ? M.Read<int>(Address + 0x10, 0x10) : 0;

    /// <summary>Gets the required dexterity.</summary>
    public int dexterity => Address != 0 ? M.Read<int>(Address + 0x10, 0x14) : 0;

    /// <summary>Gets the required intelligence.</summary>
    public int intelligence => Address != 0 ? M.Read<int>(Address + 0x10, 0x18) : 0;
}
