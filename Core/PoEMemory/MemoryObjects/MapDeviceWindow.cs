using System.Collections.Generic;
using ExileCore.PoEMemory.Elements.InventoryElements;

namespace ExileCore.PoEMemory.MemoryObjects;
public class MapDeviceWindow : Element
{
    public Element Title => (Element)(object)new int[3]
    {
        1,
        0,
        0
    };
    public Element ActivateButton => (Element)3;

    public List<MapDevicePrimordialChoiceElement> PrimordialBossSelectorButtons
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public VendorInventory MapSlot
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<VendorInventory> ScarabSlots
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<NormalInventoryItem> Items
    {
        get
        {
            while (this != null)
            {
            }

            return (List<NormalInventoryItem>)(object)this;
        }
    }
}