using SharpDX;

namespace ExileCore.PoEMemory.Elements.InventoryElements;
public class GemInventoryItem : NormalInventoryItem
{
    public override int InventPosX => 0;
    public override int InventPosY => 0;

    public override RectangleF GetClientRect()
    {
        // Fixed-position inventory tab: item rect is the parent slot rect (matches CurrencyInventoryItem).
        return Parent.GetClientRect();
    }
}