using System.Runtime.InteropServices;
using GameOffsets.Native;
using SharpDX;

namespace GameOffsets;

/// <summary>
/// Maps a generic UI element node in the in-game UI tree: its child list,
/// parent/root links, screen position, size, scale and highlight/visibility
/// state. Offsets verified against client 328.8 via an in-process Marshal.OffsetOf
/// dump (System.Numerics.Vector2 read as the layout-identical SharpDX.Vector2).
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ElementOffsets
{
    /// <summary>
    /// Historical padding constant used by some derived UI element layouts (e.g. the map
    /// elements in MapElement.cs). UNVERIFIED for 328.8 - the in-process dump does not cover
    /// those repo-specific structs, so this carries over the previous value.
    /// </summary>
    public const int OffsetBuffers = 0x6EC;

    /// <summary>Pointer to this element itself; useful as a validity check.</summary>
    [FieldOffset(0xB0)] public long SelfPointer;

    /// <summary>Pointer to the first child element.</summary>
    [FieldOffset(0xB8)] public long ChildStart;

    /// <summary>Child elements as a native pointer array (shares offset with <see cref="ChildStart"/>).</summary>
    [FieldOffset(0xB8)] public NativePtrArray Childs;

    /// <summary>Pointer just past the last child element.</summary>
    [FieldOffset(0xC0)] public long ChildEnd;

    /// <summary>Pointer to the root element of the UI tree.</summary>
    [FieldOffset(0x160)] public long Root;

    /// <summary>Pointer to the parent element.</summary>
    [FieldOffset(0x1D0)] public long Parent;

    /// <summary>Element position as a 2D vector (shares offset with <see cref="X"/>).</summary>
    [FieldOffset(0x148)] public Vector2 Position;

    /// <summary>X position of the element.</summary>
    [FieldOffset(0x148)] public float X;

    /// <summary>Y position of the element.</summary>
    [FieldOffset(0x14C)] public float Y;

    /// <summary>Scale factor applied to the element.</summary>
    [FieldOffset(0x18C)] public float Scale;

    /// <summary>Width of the element (Size.X).</summary>
    [FieldOffset(0x258)] public float Width;

    /// <summary>Height of the element (Size.Y).</summary>
    [FieldOffset(0x25C)] public float Height;

    /// <summary>Element flags (ElementFlags). IsVisibleLocal is bit 0x800.</summary>
    [FieldOffset(0x1D8)] public ulong Flags;

    /// <summary>
    /// Byte of <see cref="Flags"/> holding the IsVisibleLocal bit. ElementFlags.IsVisibleLocal
    /// is 0x800, i.e. bit 0x08 of the byte at Flags+1 (0x1D9). Consumers test against 0x08.
    /// </summary>
    [FieldOffset(0x1D9)] public byte IsVisibleLocal;

    /// <summary>Highlight/shiny state of the element (best-effort: ShinyHighlightState).</summary>
    [FieldOffset(0x294)] public byte isHighlighted;
}
