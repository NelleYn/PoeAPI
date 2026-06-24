using System.Collections.Generic;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
public class RitualWindow : Element
{
    private const int InventoryOffset = 952;
    public VendorInventory InventoryElement => (VendorInventory)(object)this;
    public List<NormalInventoryItem> Items => (List<NormalInventoryItem>)(object)this;
}