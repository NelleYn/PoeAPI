using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets;

/// <summary>
/// Maps the in-memory action wrapper referenced by an actor's current action.
/// Describes the skill being used together with its target and destination, and
/// works for every kind of skill (unlike the actor's inline action fields).
/// Offsets verified against client 328.8 via an in-process Marshal.OffsetOf dump.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActionWrapperOffsets
{
    /// <summary>Pointer to the skill being used.</summary>
    [FieldOffset(0xF8)] public long Skill;

    /// <summary>Pointer to the target entity of the action.</summary>
    [FieldOffset(0x128)] public long Target;

    /// <summary>World/grid destination of the current action.</summary>
    [FieldOffset(0x130)] public Vector2i Destination;
}
