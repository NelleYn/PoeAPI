using System.Runtime.InteropServices;

namespace GameOffsets
{
    // Verified against client 328.8 (in-process Marshal.OffsetOf dump). In 328.8 each vital's
    // values (current/max/reserved/regen) are grouped into VitalStruct, and Health/Mana/
    // EnergyShield each embed one. Buffs are no longer part of the Life component in 328.8 -
    // they live in the Buffs component (see BuffsOffsets).
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct LifeComponentOffsets
    {
        [FieldOffset(0x8)] public long Owner;
        [FieldOffset(0x178)] public VitalStruct Health;
        [FieldOffset(0x1C8)] public VitalStruct Mana;
        [FieldOffset(0x210)] public VitalStruct EnergyShield;
    }

    /// <summary>A single regenerating vital (life, mana, or energy shield) as laid out in 328.8.</summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct VitalStruct
    {
        /// <summary>Flat amount reserved (e.g. Icicle Mine).</summary>
        [FieldOffset(0x10)] public int ReservedFlat;

        /// <summary>Reserved fraction (e.g. Herald). Does not change <see cref="ReservedFlat"/>.</summary>
        [FieldOffset(0x14)] public int ReservedFraction;

        /// <summary>Regeneration per second; greater than zero while regenerating.</summary>
        [FieldOffset(0x28)] public float Regen;

        /// <summary>Maximum value of the vital.</summary>
        [FieldOffset(0x2C)] public int Max;

        /// <summary>Current value of the vital.</summary>
        [FieldOffset(0x30)] public int Current;
    }
}
