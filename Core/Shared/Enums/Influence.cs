using System;

namespace ExileCore.Shared.Enums
{
    /// <summary>
    /// Bit mask of the influences applied to an item.
    /// </summary>
    [Flags]
    public enum Influence : byte
    {
        None = 0,
        Shaper = 1,
        Elder = 2,
        Crusader = 4,
        Redeemer = 8,
        Hunter = 16,
        Warlord = 32
    }
}
