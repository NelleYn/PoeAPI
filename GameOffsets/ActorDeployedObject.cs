using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps a single entry of an actor's deployed-object array, linking a deployed
/// object (totem, golem, mine, etc.) to the skill that created it.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorDeployedObject
{
    /// <summary>Unknown discriminator; observed as 4 for totems and 22 for golems.</summary>
    [FieldOffset(0x0)] public ushort Unknown1;//4 for totems, 22 for golems

    /// <summary>Identifier of the skill that deployed this object.</summary>
    [FieldOffset(0x2)] public ushort SkillId;

    /// <summary>Identifier of the deployed object entity.</summary>
    [FieldOffset(0x4)] public ushort ObjectId;

    /// <summary>Unknown; always observed as 0.</summary>
    [FieldOffset(0x6)] public ushort Unknown2;//Always 0
}
