using System.Runtime.InteropServices;
using GameOffsets.Native;
using SharpDX;

namespace GameOffsets;

/// <summary>
/// Maps a generic UI element node in the in-game UI tree: its child list,
/// parent/root links, screen position, size, scale and highlight/visibility
/// state. The commented blocks below are retained as historical layouts from
/// earlier PoE builds (e.g. the 3.5 variant) for reference when re-resolving
/// offsets after a game patch.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ElementOffsets
{
    /// <summary>Shared buffer padding applied to UI element offsets.</summary>
    public const int OffsetBuffers = 0x6EC;

    // [FieldOffset(0x0)] public int vTable;
    /* [FieldOffset(0x3c + OffsetBuffers)] public long ChildStart;
    [FieldOffset(0x44 + OffsetBuffers)] public long ChildEnd;
    [FieldOffset(0x94 + OffsetBuffers)] public bool IsVisibleLocal;
    [FieldOffset(0xC4 + OffsetBuffers)] public long Root;
    [FieldOffset(0xCC + OffsetBuffers)] public long Parent;
    [FieldOffset(0xD4 + OffsetBuffers)] public float X;
    [FieldOffset(0xD8 + OffsetBuffers)] public float Y;
    [FieldOffset(0x104 + OffsetBuffers)] public long Tooltip;
    [FieldOffset(0x1D0 + OffsetBuffers)] public float Scale;
    [FieldOffset(0x21C  + OffsetBuffers)] public float Width;
    [FieldOffset(0x220  + OffsetBuffers)] public float Height;
    [FieldOffset(0x264 + OffsetBuffers)] public bool isHighlighted; */
    /* 3.5
    [FieldOffset(0x3c)] public long ChildStart;
    [FieldOffset(0x44)] public long ChildEnd;
    [FieldOffset(0x94)] public bool IsVisibleLocal;
    [FieldOffset(0xC4)] public long Root;
    [FieldOffset(0xCC)] public long Parent;
    [FieldOffset(0xD4)] public float X;
    [FieldOffset(0xD8)] public float Y;
    [FieldOffset(0x104)] public long Tooltip;
    [FieldOffset(0x1D0)] public float Scale;
    [FieldOffset(0x21C )] public float Width;
    [FieldOffset(0x220 )] public float Height;
    [FieldOffset(0x264)] public bool isHighlighted;
    */

    /// <summary>Pointer to this element itself; useful as a validity check.</summary>
    [FieldOffset(0x18)] public long SelfPointer; //Usefull for valid check

    /// <summary>Pointer to the first child element.</summary>
    [FieldOffset(0x38)] public long ChildStart;

    /// <summary>Child elements as a native pointer array (shares offset with <see cref="ChildStart"/>).</summary>
    [FieldOffset(0x38)] public NativePtrArray Childs;

    /// <summary>Pointer just past the last child element.</summary>
    [FieldOffset(0x40)] public long ChildEnd;

    /// <summary>Local visibility flag of the element.</summary>
    [FieldOffset(0x111)] public byte IsVisibleLocal;

    /// <summary>Pointer to the root element of the UI tree.</summary>
    [FieldOffset(0x88)] public long Root;

    /// <summary>Pointer to the parent element.</summary>
    [FieldOffset(0x90)] public long Parent; //0x1C0 work only for items

    /// <summary>Element position as a 2D vector (shares offset with <see cref="X"/>).</summary>
    [FieldOffset(0x98)] public Vector2 Position;

    /// <summary>X position of the element.</summary>
    [FieldOffset(0x98)] public float X;

    /// <summary>Y position of the element.</summary>
    [FieldOffset(0x9C)] public float Y;

    // [FieldOffset(0x338)] public long Tooltip;

    /// <summary>Scale factor applied to the element.</summary>
    [FieldOffset(0x108)] public float Scale;

    /// <summary>Width of the element.</summary>
    [FieldOffset(0x130)] public float Width;

    /// <summary>Height of the element.</summary>
    [FieldOffset(0x134)] public float Height;

    /// <summary>Whether the element is currently highlighted.</summary>
    [FieldOffset(0x178)] public bool isHighlighted;

    //  [FieldOffset(0x3CB)] public byte isShadow; //0
    //  [FieldOffset(0x3C9)] public byte isShadow2; //1

    //  [FieldOffset(0x3B0)] public NativeStringU TestString;
}
