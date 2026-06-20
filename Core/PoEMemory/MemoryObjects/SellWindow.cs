namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// UI element for the trade/sell window, exposing the offer panels and action buttons.
/// </summary>
public class SellWindow : Element
{
    /// <summary>Gets the root sell dialog element.</summary>
    public Element SellDialog => GetChildAtIndex(3);

    /// <summary>Gets the element showing the local player's offer.</summary>
    public Element YourOffer => SellDialog?.GetChildAtIndex(0);

    /// <summary>Gets the element showing the other party's offer.</summary>
    public Element OtherOffer => SellDialog?.GetChildAtIndex(1);

    /// <summary>Gets the name of the seller being traded with.</summary>
    public string NameSeller => SellDialog?.GetChildAtIndex(2).Text;

    /// <summary>Gets the accept button element.</summary>
    public Element AcceptButton => SellDialog?.GetChildAtIndex(5);

    /// <summary>Gets the cancel button element.</summary>
    public Element CancelButton => SellDialog?.GetChildAtIndex(6);
}
