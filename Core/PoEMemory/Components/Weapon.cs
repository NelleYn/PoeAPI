namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the damage and attack statistics of a weapon item.
/// </summary>
public class Weapon : Component
{
    /// <summary>Gets the minimum physical damage.</summary>
    public int DamageMin => Address != 0 ? M.Read<int>(Address + 0x28, 0x14) : 0;

    /// <summary>Gets the maximum physical damage.</summary>
    public int DamageMax => Address != 0 ? M.Read<int>(Address + 0x28, 0x18) : 0;

    /// <summary>Gets the attack time, in milliseconds.</summary>
    public int AttackTime => Address != 0 ? M.Read<int>(Address + 0x28, 0x1C) : 1;

    /// <summary>Gets the critical strike chance.</summary>
    public int CritChance => Address != 0 ? M.Read<int>(Address + 0x28, 0x20) : 0;
}
