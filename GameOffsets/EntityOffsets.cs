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

    /// <summary>Pointer to the entity's component list.</summary>
    [FieldOffset(0x10)] public long ComponentList;

    // [FieldOffset(0x40)] public uint Id;
    //  [FieldOffset(0x58)] public uint InventoryId;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Head: {Head} ComponentList:{ComponentList}";
    }
}
