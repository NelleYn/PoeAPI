namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing whether a shrine is currently available to be activated.
/// </summary>
public class Shrine : Component
{
    /// <summary>Gets a value indicating whether the shrine is available for use.</summary>
    public bool IsAvailable => Address != 0 && M.Read<byte>(Address + 0x24) == 0;
}
