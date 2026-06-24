using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;
public class MercenaryEncounterWindow : Element
{
    public Element HireButton => (Element)(object)new int[3]
    {
        2,
        9,
        0
    };
    public Element TakeItemButton => (Element)(object)new int[3]
    {
        2,
        8,
        0
    };
    public Element ExileButton => (Element)(object)new int[3];
    public Element InventoryContainer => (Element)(object)new int[2]
    {
        2,
        10
    };
    public List<VendorInventory> Inventories => (List<VendorInventory>)2;
}