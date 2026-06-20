namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the remaining time on an entity's timer.
/// </summary>
public class TimerComponent : Component
{
    /// <summary>Gets the time remaining, in seconds.</summary>
    public float TimeLeft => M.Read<float>(Address + 0x18);
}
