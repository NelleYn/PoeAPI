namespace ExileCore.PoEMemory.Elements;
public interface IItemRightClickPriceMenu
{
    Element ApplyButton { get; }

    Element CloseButton { get; }

    Element PriceAmountInput { get; }

    DropdownElement PriceCurrencyDropdown { get; }

    DropdownElement PriceTypeDropdown { get; }

    bool IsVisible { get; }
}