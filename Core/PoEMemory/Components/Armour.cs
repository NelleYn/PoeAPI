namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the defensive scores (evasion, armour, energy shield) granted by an item.
/// </summary>
public class Armour : Component
{
    /// <summary>Gets the evasion rating contributed by this item.</summary>
    public int EvasionScore => Address != 0 ? M.Read<int>(Address + 0x10, 0x10) : 0;

    /// <summary>Gets the armour rating contributed by this item.</summary>
    public int ArmourScore => Address != 0 ? M.Read<int>(Address + 0x10, 0x14) : 0;

    /// <summary>Gets the energy shield rating contributed by this item.</summary>
    public int EnergyShieldScore => Address != 0 ? M.Read<int>(Address + 0x10, 0x18) : 0;
}
