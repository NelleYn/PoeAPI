using SharpDX;

namespace ExileCore.PoEMemory.Elements.InventoryElements
{
    public class MetamorphInventoryItem : NormalInventoryItem
    {
        // Inventory Position in Metamorph Stash is always invalid.
        public override int InventPosX => 0;
        public override int InventPosY => 0;

        public override RectangleF GetClientRect()
        {
            return Parent.GetClientRect();
        }
    }
}
