using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets;

/// <summary>
/// The entity's component lookup (an in-client std::unordered_map name -&gt; component index).
/// Offsets verified against client 328.8 via an in-process Marshal.OffsetOf dump. Entity resolves
/// components by traversing the map's node list (see <see cref="Native.NativeListNodeComponent"/>),
/// whose value payload matches <see cref="ComponentNameAndIndexStruct"/>; this struct documents the
/// map container itself (bucket/prototype arrays and element count).
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentLookUpStruct
{
    /// <summary>Array of component prototypes (std::vector).</summary>
    [FieldOffset(0x10)] public NativePtrArray ComponentPrototypeArray;

    /// <summary>Array of name/index entries (std::vector of <see cref="ComponentNameAndIndexStruct"/>).</summary>
    [FieldOffset(0x28)] public NativePtrArray ComponentArray;

    /// <summary>Bucket capacity of the map.</summary>
    [FieldOffset(0x48)] public long Capacity;

    /// <summary>Number of components attached to the entity.</summary>
    [FieldOffset(0x50)] public long Count;
}

/// <summary>
/// A single name-&gt;index entry in the component lookup: a pointer to the component's name string
/// and its index into the entity's component pointer list. Verified against client 328.8.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ComponentNameAndIndexStruct
{
    /// <summary>Pointer to the component name string.</summary>
    [FieldOffset(0x0)] public long NamePtr;

    /// <summary>Index into the entity's component pointer list.</summary>
    [FieldOffset(0x8)] public int Index;
}
