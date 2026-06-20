namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the current animation key of a client-side animation controller.
/// </summary>
public class ClientAnimationController : Component
{
    /// <summary>Gets the active animation key.</summary>
    public int AnimKey => M.Read<int>(Address + 0x9c);
}
