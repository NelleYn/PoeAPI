using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps the root of a game entity: its object header and a pointer to the list
/// of components attached to the entity.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EntityOffsets
{
    /// <summary>The entity's object header (see <see cref="ObjectHeaderOffsets"/>).</summary>
    [FieldOffset(0x8)] public ObjectHeaderOffsets Head;

    /// <summary>Base of the entity's component pointer array (std::vector begin pointer).</summary>
    [FieldOffset(0x10)] public long ComponentList;

    /// <summary>The entity id. Verified against client 328.8 (Marshal.OffsetOf dump).</summary>
    [FieldOffset(0x88)] public uint Id;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Head: {Head} ComponentList:{ComponentList}";
    }
}
