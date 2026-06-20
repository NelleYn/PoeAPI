using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
public class HarvestWindow : Element
{
    public List<HarvestCraftElement> Crafts => (List<HarvestCraftElement>)(object)new int[3]
    {
        7,
        0,
        1
    };

    public VendorInventory CraftInventory
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element CraftButton => (Element)(object)new int[2]
    {
        11,
        1
    };
}