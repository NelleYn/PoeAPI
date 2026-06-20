namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing targeting state from an entity's state machine.
/// </summary>
public class StateMachine : Component
{
    /// <summary>Gets a value indicating whether the entity can be targeted.</summary>
    public bool CanBeTarget => M.Read<byte>(Address + 0xA0) == 1;

    /// <summary>Gets a value indicating whether the entity is currently targeted.</summary>
    public bool InTarget => M.Read<byte>(Address + 0xA2) == 1;
}
