namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the in-game Incursion window and its rewards.
/// </summary>
public class IncursionWindow : Element
{
    /// <summary>
    /// Gets the "enter incursion" button element, or <c>null</c> when it is not present.
    /// </summary>
    public Element AcceptElement
    {
        get
        {
            try
            {
                var button = GetChildFromIndices(3, 13, 2);

                if (button.GetChildAtIndex(0).Text == "enter incursion")
                    return button;
            }
            catch
            {
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the text of the first reward.
    /// </summary>
    public string Reward1 => GetChildFromIndices(3, 13, 3).Text;

    /// <summary>
    /// Gets the text of the second reward.
    /// </summary>
    public string Reward2 => GetChildFromIndices(3, 13, 4).Text;
}
