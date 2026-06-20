namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing currency-stack metadata such as the maximum stack size.
/// </summary>
public class CurrencyInfo : Component
{
    /// <summary>Gets the maximum stack size for this currency item.</summary>
    public int MaxStackSize => Address != 0 ? M.Read<int>(Address + 0x28) : 0;
}
