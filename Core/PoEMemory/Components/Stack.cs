namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the stack size and currency info of a stackable item.
/// </summary>
public class Stack : Component
{
    /// <summary>Gets the current number of items in the stack.</summary>
    public int Size => Address == 0 ? 0 : M.Read<int>(Address + 0x18); //0xC ?

    /// <summary>Gets the currency info describing the stack, or <c>null</c> when unavailable.</summary>
    public CurrencyInfo Info => Address != 0 ? ReadObject<CurrencyInfo>(Address + 0x10) : null;
}
