using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps a single buff/debuff applied to an entity (client 328.8), including the pointer used
/// to resolve its name, charges, source, and remaining duration.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct BuffOffsets
{
    /// <summary>Pointer to the buff definition (.dat row), used to resolve the buff name.</summary>
    [FieldOffset(0x8)] public long Name;

    /// <summary>Total duration of the buff, in seconds (infinity for auras / always-on buffs).</summary>
    [FieldOffset(0x18)] public float MaxTime;

    /// <summary>Elapsed time of the buff, in seconds.</summary>
    [FieldOffset(0x1C)] public float Timer;

    /// <summary>Entity id that applied the buff.</summary>
    [FieldOffset(0x28)] public uint SourceEntityId;

    /// <summary>Number of charges currently on the buff.</summary>
    [FieldOffset(0x40)] public ushort Charges;

    /// <summary>Flask slot that produced this buff, when applicable.</summary>
    [FieldOffset(0x42)] public ushort FlaskSlot;

    /// <summary>Skill id that produced this buff.</summary>
    [FieldOffset(0x48)] public ushort SourceSkillId;

    /// <summary>Secondary skill id that produced this buff.</summary>
    [FieldOffset(0x4A)] public ushort SourceSkillId2;
}
