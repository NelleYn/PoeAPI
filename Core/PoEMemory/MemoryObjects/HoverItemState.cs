namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Describes what the client would do with the item currently held over an inventory slot.
/// </summary>
public enum HoverItemState
{
    None = 0,
    WillDrop = 1,
    WillSwap = 2,
    WillStack = 3,
    WillUse = 4
}
