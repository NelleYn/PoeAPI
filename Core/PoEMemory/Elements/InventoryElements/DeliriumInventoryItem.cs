using SharpDX;

namespace ExileCore.PoEMemory.Elements.InventoryElements
{
    public class DeliriumInventoryItem : NormalInventoryItem
    {
        // Inventory Position in Delirium Stash is always invalid.
        public override int InventPosX => 0;
        public override int InventPosY => 0;

        public override RectangleF GetClientRect()
        {
            return Parent.GetClientRect();
        }
    }
}
