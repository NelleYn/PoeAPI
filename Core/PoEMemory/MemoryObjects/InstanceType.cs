namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Type of the game instance the client is currently connected to.
/// Values observed in the reference distribution.
/// </summary>
public enum InstanceType
{
    /// <summary>Regular game instance.</summary>
    Normal = 2,

    /// <summary>Guest trade instance.</summary>
    GuestTrade = 11
}
