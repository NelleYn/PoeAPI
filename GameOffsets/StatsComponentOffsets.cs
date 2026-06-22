using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    // Verified against client 328.8 (in-process Marshal.OffsetOf dump). In 328.8 the stat array
    // is no longer stored directly on the component; SubStatsPtr points to a
    // SubStatsComponentOffsets whose Stats vector holds the (id, value) pairs.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct StatsComponentOffsets
    {
        [FieldOffset(0x8)] public long Owner;
        [FieldOffset(0x20)] public long SubStatsPtr;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct SubStatsComponentOffsets
    {
        // std::vector of contiguous (int statId, int value) pairs.
        [FieldOffset(0xF0)] public NativePtrArray Stats;
    }

    // Verified against client 328.8 (in-process Marshal.OffsetOf dump). StatsPtr is the stat
    // vector itself (std::vector of contiguous (int statId, int value) pairs).
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct LocalStatsComponentOffsets
    {
        [FieldOffset(0x20)] public NativePtrArray StatsPtr;
    }
}
