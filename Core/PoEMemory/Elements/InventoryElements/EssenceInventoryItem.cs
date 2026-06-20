using SharpDX;

namespace ExileCore.PoEMemory.Elements.InventoryElements;

/// <summary>
/// Inventory item wrapper for the Essence stash tab, where item positions are fixed and inventory coordinates are ignored.
/// </summary>
public class EssenceInventoryItem : NormalInventoryItem
{
    // Inventory Position in Essence Stash is always invalid.
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
