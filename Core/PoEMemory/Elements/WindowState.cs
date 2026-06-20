namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element exposing the local visibility flag of a game window.
/// </summary>
public class WindowState : Element
{
    /// <summary>
    /// Gets a value indicating whether the window is locally visible.
    /// </summary>
    public new bool IsVisibleLocal => M.Read<int>(Address + 0x860) == 1;
}
