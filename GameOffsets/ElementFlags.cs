using System;

namespace GameOffsets;

/// <summary>
/// Bit flags stored in the UI element node's flag word. Only a few bits have a
/// known meaning; the rest are exposed under their bit index as observed in the
/// reference distribution.
/// </summary>
[Flags]
public enum ElementFlags : ulong
{
    /// <summary>Bit 0 of the element flag word.</summary>
    Flag0 = 1,

    /// <summary>Bit 1 of the element flag word.</summary>
    Flag1 = 2,

    /// <summary>Bit 2 of the element flag word.</summary>
    Flag2 = 4,

    /// <summary>Bit 3 of the element flag word.</summary>
    Flag3 = 8,

    /// <summary>Bit 4 of the element flag word.</summary>
    Flag4 = 16,

    /// <summary>Bit 5 of the element flag word.</summary>
    Flag5 = 32,

    /// <summary>Bit 6 of the element flag word.</summary>
    Flag6 = 64,

    /// <summary>Bit 7 of the element flag word.</summary>
    Flag7 = 128,

    /// <summary>Bit 8 of the element flag word.</summary>
    Flag8 = 256,

    /// <summary>Bit 9 of the element flag word.</summary>
    Flag9 = 512,

    /// <summary>Bit 10 of the element flag word.</summary>
    IsScrollable = 1024,

    /// <summary>Bit 11 of the element flag word.</summary>
    IsVisibleLocal = 2048,

    /// <summary>Bit 12 of the element flag word.</summary>
    Flag12 = 4096,

    /// <summary>Bit 13 of the element flag word.</summary>
    IsActive = 8192,

    /// <summary>Bit 14 of the element flag word.</summary>
    Flag14 = 16384,

    /// <summary>Bit 15 of the element flag word.</summary>
    Flag15 = 32768,

    /// <summary>Bit 16 of the element flag word.</summary>
    Flag16 = 65536,

    /// <summary>Bit 17 of the element flag word.</summary>
    Flag17 = 131072,

    /// <summary>Bit 18 of the element flag word.</summary>
    Flag18 = 262144,

    /// <summary>Bit 19 of the element flag word.</summary>
    Flag19 = 524288,

    /// <summary>Bit 20 of the element flag word.</summary>
    Flag20 = 1048576,

    /// <summary>Bit 21 of the element flag word.</summary>
    Flag21 = 2097152,

    /// <summary>Bit 22 of the element flag word.</summary>
    Flag22 = 4194304,

    /// <summary>Bit 23 of the element flag word.</summary>
    Flag23 = 8388608,

    /// <summary>Bit 24 of the element flag word.</summary>
    Flag24 = 16777216,

    /// <summary>Bit 25 of the element flag word.</summary>
    Flag25 = 33554432,

    /// <summary>Bit 26 of the element flag word.</summary>
    Flag26 = 67108864,

    /// <summary>Bit 27 of the element flag word.</summary>
    Flag27 = 134217728,

    /// <summary>Bit 28 of the element flag word.</summary>
    Flag28 = 268435456,

    /// <summary>Bit 29 of the element flag word.</summary>
    Flag29 = 536870912,

    /// <summary>Bit 30 of the element flag word.</summary>
    Flag30 = 1073741824,

    /// <summary>Bit 31 of the element flag word.</summary>
    Flag31 = 2147483648,

    /// <summary>Bit 32 of the element flag word.</summary>
    Flag32 = 4294967296,

    /// <summary>Bit 33 of the element flag word.</summary>
    Flag33 = 8589934592,

    /// <summary>Bit 34 of the element flag word.</summary>
    Flag34 = 17179869184,

    /// <summary>Bit 35 of the element flag word.</summary>
    Flag35 = 34359738368,

    /// <summary>Bit 36 of the element flag word.</summary>
    Flag36 = 68719476736,

    /// <summary>Bit 37 of the element flag word.</summary>
    Flag37 = 137438953472,

    /// <summary>Bit 38 of the element flag word.</summary>
    Flag38 = 274877906944,

    /// <summary>Bit 39 of the element flag word.</summary>
    Flag39 = 549755813888,

    /// <summary>Bit 40 of the element flag word.</summary>
    Flag40 = 1099511627776,

    /// <summary>Bit 41 of the element flag word.</summary>
    Flag41 = 2199023255552,

    /// <summary>Bit 42 of the element flag word.</summary>
    Flag42 = 4398046511104,

    /// <summary>Bit 43 of the element flag word.</summary>
    Flag43 = 8796093022208,

    /// <summary>Bit 44 of the element flag word.</summary>
    Flag44 = 17592186044416,

    /// <summary>Bit 45 of the element flag word.</summary>
    IsSaturated = 35184372088832,

    /// <summary>Bit 46 of the element flag word.</summary>
    Flag46 = 70368744177664,

    /// <summary>Bit 47 of the element flag word.</summary>
    Flag47 = 140737488355328,

    /// <summary>Bit 48 of the element flag word.</summary>
    Flag48 = 281474976710656,

    /// <summary>Bit 49 of the element flag word.</summary>
    Flag49 = 562949953421312,

    /// <summary>Bit 50 of the element flag word.</summary>
    Flag50 = 1125899906842624,

    /// <summary>Bit 51 of the element flag word.</summary>
    Flag51 = 2251799813685248,

    /// <summary>Bit 52 of the element flag word.</summary>
    Flag52 = 4503599627370496,

    /// <summary>Bit 53 of the element flag word.</summary>
    Flag53 = 9007199254740992,

    /// <summary>Bit 54 of the element flag word.</summary>
    Flag54 = 18014398509481984,

    /// <summary>Bit 55 of the element flag word.</summary>
    Flag55 = 36028797018963968,

    /// <summary>Bit 56 of the element flag word.</summary>
    Flag56 = 72057594037927936,

    /// <summary>Bit 57 of the element flag word.</summary>
    Flag57 = 144115188075855872,

    /// <summary>Bit 58 of the element flag word.</summary>
    Flag58 = 288230376151711744,

    /// <summary>Bit 59 of the element flag word.</summary>
    Flag59 = 576460752303423488,

    /// <summary>Bit 60 of the element flag word.</summary>
    Flag60 = 1152921504606846976,

    /// <summary>Bit 61 of the element flag word.</summary>
    Flag61 = 2305843009213693952,

    /// <summary>Bit 62 of the element flag word.</summary>
    Flag62 = 4611686018427387904,

    /// <summary>Bit 63 of the element flag word.</summary>
    Flag63 = 9223372036854775808,
}
