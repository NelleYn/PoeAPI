namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// State identifiers of a Sentinel slot, as observed in the reference distribution.
/// </summary>
public enum SentinelState : uint
{
    /// <summary>Value observed in the reference distribution.</summary>
    Active1 = 165,

    /// <summary>Value observed in the reference distribution.</summary>
    Active2 = 229,

    /// <summary>Value observed in the reference distribution.</summary>
    Active3 = 227,

    /// <summary>Value observed in the reference distribution.</summary>
    OutOfCharges = 260,

    /// <summary>Value observed in the reference distribution.</summary>
    Unallocated = 298,

    /// <summary>Value observed in the reference distribution.</summary>
    UsableBlue = 346,

    /// <summary>Value observed in the reference distribution.</summary>
    UsableRed = 261,

    /// <summary>Value observed in the reference distribution.</summary>
    UsableYellow = 236,

    /// <summary>Value observed in the reference distribution.</summary>
    Used = 425
}
