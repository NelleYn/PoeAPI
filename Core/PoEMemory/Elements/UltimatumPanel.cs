using System.Collections.Generic;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.FilesInMemory.Ultimatum;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;
public class UltimatumPanel : Element
{
    private readonly CachedValue<UltimatumPanelOffsets> _cache;
    public UltimatumChoicePanel ChoicesPanel
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element ConfirmButton => (Element)(object)new int[3]
    {
        2,
        6,
        0
    };
    public int SelectedChoice => (int)this;
    public List<UltimatumModifier> Modifiers => (List<UltimatumModifier>)(object)this;
    public List<UltimatumChoiceElement> ChoicesElements => (List<UltimatumChoiceElement>)(object)this;
    public VendorInventory EarnedRewardsInventory => (VendorInventory)(object)new int[2]
    {
        6,
        2
    };
    public Element OpenEarnedRewardsInventoryButton => (Element)(object)new int[2]
    {
        1,
        3
    };
    public VendorInventory LastRewardInventory => (VendorInventory)(object)new int[3]
    {
        1,
        1,
        0
    };

    public NormalInventoryItem LastRewardItem
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public VendorInventory NextRewardInventory => (VendorInventory)(object)new int[3];

    public NormalInventoryItem NextRewardItem
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}