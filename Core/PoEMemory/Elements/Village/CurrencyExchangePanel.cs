using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.Village;
public class CurrencyExchangePanel : Element
{
    private readonly CachedValue<CurrencyExchangePanelOffsets> _cache;
    public BaseItemType OfferedItemType => (BaseItemType)(object)this;
    public BaseItemType WantedItemType => (BaseItemType)(object)this;
    private List<CurrencyExchangeStock> Stock1 => (List<CurrencyExchangeStock>)(object)this;
    private List<CurrencyExchangeStock> Stock2 => (List<CurrencyExchangeStock>)(object)this;

    public unsafe List<CurrencyExchangeStock> WantedItemStock
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public unsafe List<CurrencyExchangeStock> OfferedItemStock
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public short MarketRateGet => (short)(int)this;
    public short MarketRateGive => (short)(int)this;
    public CurrencyExchangeCurrencyPickerElement CurrencyPicker => (CurrencyExchangeCurrencyPickerElement)(object)this;
    public List<CurrencyExchangePanelOrderElement> OrderElements => (List<CurrencyExchangePanelOrderElement>)(object)this;
    public List<PlacedCurrencyExchangeOrder> Orders => (List<PlacedCurrencyExchangeOrder>)88;
    public Element OfferedItemCountInput => this;
    public Element WantedItemCountInput => this;
    public Element RatioElement => this;
}