namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the in-game chat panel and its messages.
/// </summary>
public class PoeChatElement : Element
{
    /// <summary>
    /// Gets the total number of chat messages currently present.
    /// </summary>
    public long TotalMessageCount => ChildCount;

    /// <summary>
    /// Gets the chat message label at the specified index, or <c>null</c> when out of range.
    /// </summary>
    /// <param name="index">The zero-based message index.</param>
    public EntityLabel this[int index]
    {
        get
        {
            if (index < TotalMessageCount)
                return GetChildAtIndex(index).AsObject<EntityLabel>();

            return null;
        }
    }
}
