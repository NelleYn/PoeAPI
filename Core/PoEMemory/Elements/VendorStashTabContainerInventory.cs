using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
public class VendorStashTabContainerInventory : StashTabContainerInventory
{
    public override VendorInventory Inventory => (VendorInventory)(object)this;
}