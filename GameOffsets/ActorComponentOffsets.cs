using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets;

/// <summary>
/// Maps the Actor component of an entity, which describes the entity's current
/// action/animation, the skills it can use and any objects it has deployed
/// (totems, golems, mines, etc.). Offsets verified against client 328.8 via an
/// in-process Marshal.OffsetOf dump.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorComponentOffsets
{
    /// <summary>Pointer to the actor's animation controller.</summary>
    [FieldOffset(0x1A0)] public long AnimationControllerPtr;

    /// <summary>Pointer to the current action wrapper (see <see cref="ActionWrapperOffsets"/>).</summary>
    [FieldOffset(0x1B0)] public long ActionPtr;

    /// <summary>Identifier of the action currently being performed.</summary>
    [FieldOffset(0x218)] public short ActionId;

    /// <summary>Identifier of the animation currently playing.</summary>
    [FieldOffset(0x248)] public int AnimationId;

    /// <summary>Array of the actor's available skills.</summary>
    [FieldOffset(0x6F0)] public NativePtrArray ActorSkillsArray;

    /// <summary>Array of the actor's skill cooldowns.</summary>
    [FieldOffset(0x708)] public NativePtrArray ActorSkillsCooldownArray;

    /// <summary>Array of the actor's Vaal skills.</summary>
    [FieldOffset(0x720)] public NativePtrArray ActorVaalSkills;

    /// <summary>Array of objects deployed by the actor (totems, golems, mines, etc.).</summary>
    [FieldOffset(0x740)] public NativePtrArray DeployedObjectArray;
}
