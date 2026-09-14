namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Status of a relationship between two Immortal Syndicate members.
/// </summary>
public enum BetrayalRelationshipStatus
{
    /// <summary>The two members are rivals.</summary>
    Rival = 0,

    /// <summary>The two members have no relationship.</summary>
    Neutral = 1,

    /// <summary>The two members trust each other.</summary>
    Trusted = 2,
}
