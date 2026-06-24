using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.Village;
public class CurrencyExchangeCurrencyPickerElement : Element
{
    private readonly CachedValue<CurrencyExchangeCurrencyPickerElementOffsets> _cache;
    public bool IsPickingWantedCurrency => this == null;
    public Element OptionContainer => (Element)6;

    public List<CurrencyExchangeCurrencyPickerCurrencyOption> Options
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}