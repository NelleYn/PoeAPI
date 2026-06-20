using ExileCore.PoEMemory.Elements.InventoryElements;

namespace ExileCore.PoEMemory.MemoryObjects;
public class CardTradeWindow : Element
{
    public Element CardSlotElement => (Element)5;

    public NormalInventoryItem CardSlotItem
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element TradeButton => (Element)4;
}