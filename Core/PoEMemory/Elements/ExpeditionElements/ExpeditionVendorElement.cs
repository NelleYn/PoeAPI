using System.Collections.Generic;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements.ExpeditionElements;
public class ExpeditionVendorElement : Element
{
    public SearchBarElement HighlightSearchbarElement => (SearchBarElement)(object)new int[2]
    {
        4,
        1
    };
    public Element VendorResponseTextBox => (Element)(object)new int[2]
    {
        6,
        1
    };
    public string VendorWindowTitle => (string)(object)new int[3]
    {
        6,
        2,
        0
    };
    public Element RefreshItemsButton => (Element)(object)new int[2]
    {
        7,
        0
    };
    public Element RefreshCurrencyInfoBox => (Element)(object)new int[2]
    {
        7,
        1
    };

    public List<NormalInventoryItem> InventoryItems
    {
        get
        {
            //IL_0011: Expected O, but got I4
            _ = new int[4]
            {
                8,
                1,
                0,
                0
            };
            return (List<NormalInventoryItem>)1;
        }
    }

    public ExpeditionVendorCurrencyInfoElement CurrencyInfo
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<Entity> OfferedItems
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public TujenHaggleWindowElement TujenHaggleWindow => (TujenHaggleWindowElement)(object)new int[2]
    {
        11,
        0
    };
}