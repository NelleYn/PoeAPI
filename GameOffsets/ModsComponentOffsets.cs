using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ModsComponentOffsets
    {
        public static readonly int HumanStats = 0x20;
        [FieldOffset(0x30)] public long UniqueName;
        [FieldOffset(0x88)] public bool Identified;
        [FieldOffset(0x8C)] public int ItemRarity;
        [FieldOffset(0x90)] public NativePtrArray implicitMods;
        [FieldOffset(0xA8)] public NativePtrArray explicitMods;

        // Third array of the implicit/explicit/enchantment triple. Derived — not dumped — from
        // ModsAndObjectMagicPropertiesCommonStruct (GameOffsets/Components/ModsAndObjectMagicProperties.cs),
        // which documents the same three arrays "one after the other" at 0x60/0x78/0x90 together with the
        // rule "If looking at this Struct from Mods component, Add 0x30 to each offset". That rule
        // reproduces every field of this struct that both describe:
        //     WordsPtr 0x00 + 0x30 = 0x30 = UniqueName
        //     Identified   0x58 + 0x30 = 0x88   ItemRarity 0x5C + 0x30 = 0x8C
        //     Implicit     0x60 + 0x30 = 0x90   Explicit   0x78 + 0x30 = 0xA8
        // so EnchantmentModsPtr 0x90 + 0x30 = 0xC0. The same value follows independently from the
        // arrays being contiguous: NativePtrArray is 0x18 bytes, and 0x90 + 0x18 = 0xA8 (explicit),
        // 0xA8 + 0x18 = 0xC0. The client-328.8 reconstruction shows the identical
        // implicit -> explicit -> enchant ordering at its own (different) offsets, confirming the shape.
        //
        // Caveat: the two structs diverge in the later stats region (StatsPtr 0xF0 + 0x30 = 0x120 does
        // not line up with GetStats 0x1A0 below), so the "+0x30" rule is only relied on here for the
        // mod-array block it demonstrably reproduces. Readers of this field must stay fail-safe: the
        // walk in ComponentCompat.EnchantedMods bails out on an implausible range, so a wrong offset
        // yields "no enchanted mods" rather than garbage mods.
        [FieldOffset(0xC0)] public NativePtrArray enchantedMods;
        [FieldOffset(0x170)] public NativePtrArray GetImplicitStats;
        [FieldOffset(0x1A0)] public NativePtrArray GetStats;
        [FieldOffset(0x1B8)] public NativePtrArray GetCraftedStats;
        [FieldOffset(0x1D0)] public NativePtrArray GetFracturedStats;
        [FieldOffset(0x434)] public int ItemLevel;
        [FieldOffset(0x438)] public int RequiredLevel;
        [FieldOffset(0x370)] public byte IsUsable;
        [FieldOffset(0x371)] public byte IsMirrored;
    }
}
