using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps a node of the game's linked entity list, providing the forward/backward
/// links used to traverse the list and a pointer to the node's entity.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityListOffsets
{
    /// <summary>Pointer to the first/next linked-list node.</summary>
    [FieldOffset(0x0)] public long FirstAddr;

    /// <summary>Pointer to the second/previous linked-list node.</summary>
    [FieldOffset(0x10)] public long SecondAddr;

    /// <summary>Pointer to the entity referenced by this node.</summary>
    [FieldOffset(0x28)] public long Entity;
}
