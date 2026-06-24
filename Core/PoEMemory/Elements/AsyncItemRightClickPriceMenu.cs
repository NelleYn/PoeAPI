namespace ExileCore.PoEMemory.Elements;
public class AsyncItemRightClickPriceMenu : Element, IItemRightClickPriceMenu
{
    public Element ControlRoot => (Element)(object)new int[2]
    {
        2,
        0
    };

    public Element ApplyButton
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element CloseButton => (Element)1;
    public DropdownElement PriceTypeDropdown => null;

    public Element PriceAmountInput
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public DropdownElement PriceCurrencyDropdown
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}