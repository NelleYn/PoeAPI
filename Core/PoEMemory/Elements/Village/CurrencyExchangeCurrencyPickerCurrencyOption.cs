using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.Elements.Village;
public class CurrencyExchangeCurrencyPickerCurrencyOption : Element
{
    public BaseItemType ItemType => (BaseItemType)(this + (long)this);

    public int Owned
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}