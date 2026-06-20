using SharpDX;

namespace ExileCore.PoEMemory.Elements.InventoryElements;

/// <summary>
/// Inventory item wrapper for the Divination Card stash tab, applying the tab's scroll offset to its client rectangle.
/// </summary>
public class DivinationInventoryItem : NormalInventoryItem
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
        var tmp = Parent.GetClientRect();

        // div stash tab scrollbar element scroll value calculator
        var addr = Parent.Parent.Parent.Parent.Children[2].Address + 0xA64;
        var sub = M.Read<int>(addr) * (float) 107.5;
        tmp.Y -= sub;

        return tmp;
    }
}
