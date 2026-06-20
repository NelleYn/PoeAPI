namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the magnetic force of an in-game object.
/// </summary>
public class Magnetic : Component
{
    /// <summary>Gets the magnetic force value.</summary>
    public int Force => M.Read<int>(Address + 0x30);
}
