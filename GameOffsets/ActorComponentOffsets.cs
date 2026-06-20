using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets;

/// <summary>
/// Maps the Actor component of an entity, which describes the entity's current
/// action/animation, the skills it can use and any objects it has deployed
/// (totems, golems, mines, etc.). Commented offsets below are kept as historical
/// notes about fields that are unreliable or build-specific.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorComponentOffsets
{
    /// <summary>Pointer to the current action wrapper (see <see cref="ActionWrapperOffsets"/>).</summary>
    [FieldOffset(0xA8)] public long ActionPtr;

    /// <summary>Identifier of the action currently being performed.</summary>
    [FieldOffset(0x108)] public short ActionId;

    // [FieldOffset(0xFA)] public short TotalActionCounterA;
    // [FieldOffset(0xFC)] public int TotalActionCounterB;
    // only works for channeling skills
    // [FieldOffset(0x100)] public float TotalTimeInAction;
    // some unknown timer whos value keep resetting to zero.
    // [FieldOffset(0x104)] public float UnknownTimer;

    /// <summary>Identifier of the animation currently playing.</summary>
    [FieldOffset(0x120)] public int AnimationId;

    // Use the one inside the ActionPtr struct (i.e. ActionWrapperOffsets).
    // That one works for all kind of skills.
    // [FieldOffset(0x128)] public Vector2 SkillDestination;

    /// <summary>Array of the actor's available skills.</summary>
    [FieldOffset(0x408)] public NativePtrArray ActorSkillsArray;

    // Broken Offset, remove comment on fixup.
    // [FieldOffset(0x418)] public NativePtrArray ActorVaalSkills;
    // [FieldOffset(0x438)] public NativePtrArray HasMinionArray;

    /// <summary>Array of objects deployed by the actor (totems, golems, mines, etc.).</summary>
    [FieldOffset(0x470)] public NativePtrArray DeployedObjectArray;
}
