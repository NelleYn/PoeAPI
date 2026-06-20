namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the quality value of an item.
/// </summary>
public class Quality : Component
{
    /// <summary>Gets the item quality percentage.</summary>
    public int ItemQuality => Address != 0 ? M.Read<int>(Address + 0x18) : 0;
}
