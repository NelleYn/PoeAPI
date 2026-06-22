using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    // Verified against client 328.8 via an in-process Marshal.OffsetOf dump. The human-readable
    // stat arrays moved out of this component in 328.8 and now live in ModsComponentStatsOffsets,
    // reached via ModsComponentStatsPtr. UniqueName is the first pointer of a NativePtrArray and
    // is exposed as a long so the existing string-reading code is unchanged.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ModsComponentOffsets
    {
        [FieldOffset(0x38)] public long UniqueName;
        [FieldOffset(0xB0)] public bool Identified;
        [FieldOffset(0xB4)] public int ItemRarity;
        [FieldOffset(0xC0)] public NativePtrArray implicitMods;
        [FieldOffset(0xD8)] public NativePtrArray explicitMods;
        [FieldOffset(0xF0)] public NativePtrArray enchantMods;
        [FieldOffset(0x120)] public NativePtrArray crucibleMods;
        [FieldOffset(0x210)] public long ModsComponentStatsPtr;
        [FieldOffset(0x248)] public int ItemLevel;
        [FieldOffset(0x24C)] public int RequiredLevel;
        [FieldOffset(0x25F)] public byte IsUsable;
        [FieldOffset(0x26E)] public byte IsMirrored;
    }

    // Human-readable stat arrays, reached via ModsComponentOffsets.ModsComponentStatsPtr (328.8).
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ModsComponentStatsOffsets
    {
        [FieldOffset(0x8)] public NativePtrArray ImplicitStatsArray;
        [FieldOffset(0x48)] public NativePtrArray EnchantedStatsArray;
        [FieldOffset(0xC8)] public NativePtrArray CrucibleStatsArray;
        [FieldOffset(0x108)] public NativePtrArray ExplicitStatsArray;
        [FieldOffset(0x148)] public NativePtrArray CraftedStatsArray;
        [FieldOffset(0x188)] public NativePtrArray FracturedStatsArray;
    }
}
