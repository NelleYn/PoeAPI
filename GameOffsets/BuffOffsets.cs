using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps a single buff/debuff applied to an entity, including its name, charges
/// and remaining duration. Note that <see cref="IsInvisible"/> and
/// <see cref="MaxTime"/> deliberately share offset 0x10 in the source memory.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct BuffOffsets
{
    /// <summary>Pointer to the buff's name string.</summary>
    [FieldOffset(0x8)] public long Name;

    /// <summary>Whether the buff is hidden from the UI.</summary>
    [FieldOffset(0x10)] public byte IsInvisible;

    /// <summary>Whether the buff can be removed.</summary>
    [FieldOffset(0x11)] public byte IsRemovable;

    /// <summary>Number of charges currently on the buff.</summary>
    [FieldOffset(0x2C)] public byte Charges;

    /// <summary>Total duration of the buff, in seconds.</summary>
    [FieldOffset(0x10)] public float MaxTime;

    /// <summary>Elapsed time of the buff, in seconds.</summary>
    [FieldOffset(0x14)] public float Timer;
}
