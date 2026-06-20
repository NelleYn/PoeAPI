using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps the holder of the entity-label map, a pointer to the structure that
/// associates entities with their on-screen labels.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityLabelMapOffsets
{
    /// <summary>Pointer to the entity-to-label map.</summary>
    [FieldOffset(0x2A0)] public long EntityLabelMap;
}
