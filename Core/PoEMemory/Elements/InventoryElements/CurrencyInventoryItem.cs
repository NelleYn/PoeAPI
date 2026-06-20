using SharpDX;

namespace ExileCore.PoEMemory.Elements.InventoryElements;

/// <summary>
/// Inventory item wrapper for the Currency stash tab, where item positions are fixed and inventory coordinates are ignored.
/// </summary>
public class CurrencyInventoryItem : NormalInventoryItem
{
    // Inventory Position in Currency Stash is always invalid.
    // Also, as items are fixed, so Inventory Position doesn't matter.

    /// <inheritdoc />
    public override int InventPosX => 0;

    /// <inheritdoc />
    public override int InventPosY => 0;

    /// <inheritdoc />
    public override RectangleF GetClientRect()
    {
        return Parent.GetClientRect();
    }
}
