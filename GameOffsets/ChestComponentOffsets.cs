using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps the Chest component of an entity, exposing the chest's open/locked state,
/// whether it is a strongbox and a pointer to its strongbox-specific data.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ChestComponentOffsets
{
    /// <summary>Whether the chest has been opened.</summary>
    [FieldOffset(0x78)] public bool IsOpened;

    /// <summary>Whether the chest is locked.</summary>
    [FieldOffset(0x79)] public bool IsLocked;

    /// <summary>Whether the chest is a strongbox.</summary>
    [FieldOffset(0xB8)] public bool IsStrongbox;

    /// <summary>Quality value of the chest's contents.</summary>
    [FieldOffset(0x7C)] public readonly byte quality;

    /// <summary>Pointer to the strongbox-specific data (valid when <see cref="IsStrongbox"/>).</summary>
    [FieldOffset(0x58)] public long StrongboxData;
}
