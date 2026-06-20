namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the transition state flags of a transitionable object (e.g. doors, levers).
/// </summary>
public class Transitionable : Component
{
    /// <summary>Gets the first transition state flag.</summary>
    public byte Flag1 => M.Read<byte>(Address + 0x120);

    /// <summary>Gets the second transition state flag.</summary>
    public byte Flag2 => M.Read<byte>(Address + 0x124);
}
