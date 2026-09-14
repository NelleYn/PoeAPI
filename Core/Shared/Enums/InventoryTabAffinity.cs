using System;

namespace ExileCore.Shared.Enums
{
    /// <summary>
    /// Bit mask describing which item categories a stash tab is set as the affinity target for.
    /// Members named <c>BitN</c> have no known category in the reference distribution.
    /// </summary>
    [Flags]
    public enum InventoryTabAffinity : uint
    {
        Bit0 = 1,
        Incubator = 2,
        Bit2 = 4,
        Currency = 8,
        Unique = 16,
        Map = 32,
        DivinationCard = 64,
        Settlers = 128,
        Essence = 256,
        Fragment = 512,
        Sanctum = 1024,
        Bit11 = 2048,
        Delve = 4096,
        Blight = 8192,
        Ultimatum = 16384,
        Delirium = 32768,
        Flask = 131072,
        Gem = 262144,
        Bit19 = 524288,
        Bit20 = 1048576,
        Ritual = 2097152
    }
}
