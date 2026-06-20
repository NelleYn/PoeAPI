namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element wrapping the tooltip shown for an item lying on the ground.
/// </summary>
public class ItemOnGroundTooltip : Element
{
    /// <summary>
    /// Gets the frame element drawn around the item, or <c>null</c> when unavailable.
    /// </summary>
    public Element ItemFrame => GetChildAtIndex(0) == null ? null : GetChildAtIndex(0).GetChildAtIndex(0);

    /// <summary>
    /// Gets the tooltip body element, or <c>null</c> when unavailable.
    /// </summary>
    public Element Tooltip => GetChildAtIndex(0) == null ? null : GetChildAtIndex(0).GetChildAtIndex(1);

    /// <summary>
    /// Gets the tooltip UI container element, or <c>null</c> when unavailable.
    /// </summary>
    public Element TooltipUI => GetChildAtIndex(0) == null ? null : GetChildAtIndex(0).GetChildAtIndex(0);
}
