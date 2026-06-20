namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the charge state of a flask or other charge-using item.
/// </summary>
public class Charges : Component
{
    /// <summary>Gets the current number of charges.</summary>
    public int NumCharges => Address != 0 ? M.Read<int>(Address + 0x18) : 0;

    /// <summary>Gets the number of charges consumed per use.</summary>
    public int ChargesPerUse => Address != 0 ? M.Read<int>(Address + 0x10, 0x14) : 0;

    /// <summary>Gets the maximum number of charges that can be stored.</summary>
    public int ChargesMax => Address != 0 ? M.Read<int>(Address + 0x10, 0x10) : 0;
}
