using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps a single entry of an actor's deployed-object array, linking a deployed
/// object (totem, golem, mine, etc.) to the skill that created it. Offsets verified
/// against client 328.8 via an in-process Marshal.OffsetOf dump.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorDeployedObject
{
    /// <summary>Entity id of the deployed object.</summary>
    [FieldOffset(0x0)] public uint ObjectId;

    /// <summary>Identifier of the skill that deployed this object.</summary>
    [FieldOffset(0x4)] public ushort SkillId;

    /// <summary>Type discriminator of the deployed object.</summary>
    [FieldOffset(0x8)] public ushort ObjectType;
}
