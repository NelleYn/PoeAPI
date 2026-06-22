using System.Runtime.InteropServices;
using SharpDX;

namespace GameOffsets;

/// <summary>
/// Maps the in-memory action wrapper referenced by an actor's current action.
/// Describes the skill being used together with its target and destination, and
/// works for every kind of skill (unlike the actor's inline action fields).
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActionWrapperOffsets
{
    /// <summary>World destination of the current action.</summary>
    [FieldOffset(0x130)] public Vector2 Destination;

    /// <summary>Pointer to the target entity of the action.</summary>
    [FieldOffset(0x128)] public long Target;

    /// <summary>Pointer to the skill being used.</summary>
    [FieldOffset(0xF8)] public long Skill;
}
