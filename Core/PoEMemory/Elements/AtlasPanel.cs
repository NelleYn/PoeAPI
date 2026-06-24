using System.Collections.Generic;
using ExileCore.PoEMemory.Elements.AtlasElements;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
public class AtlasPanel : Element
{
    public Element SearchInput => (Element)(object)new int[4]
    {
        5,
        0,
        2,
        0
    };
    public Element AtlasSkillsToggleButton => (Element)(object)new int[2]
    {
        6,
        0
    };
    public Element KiracsVaultPassButton => (Element)(object)new int[2]
    {
        6,
        1
    };

    public List<Element> TreeSelectorButtons
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private Element InnerAtlas => (Element)0;

    public Dictionary<Atlasbonus, int> AtlasBonus
    {
        get
        {
            /*Error: Stack underflow*/
            ;
            _ = 0;
            _ = 0;
            _ = 1;
            _ = 47;
            _ = ((object[])0)[0];
            _ = 2;
            _ = 47;
            return (Dictionary<Atlasbonus, int>)((object[])0)[1];
        }
    }

    public Element SearingExarchCounterElement => this;
    public Element MavenCounterElement => this;
    public Element EaterofWorldsCounterElement => this;

    public List<VoidStoneSlot> VoidstoneSlots
    {
        get
        {
            int[] result = new int[4];
            while (this != null)
            {
            }

            return (List<VoidStoneSlot>)(object)result;
        }
    }

    public VendorInventory MapDeviceStorage
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<VoidStoneSlot> MapDeviceVoidstoneStorage
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public MapDeviceWindow MapDeviceWindow
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}