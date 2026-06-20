using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps a diagnostic graph element (e.g. the FPS/latency overlay): its position,
/// size and a pointer to the backing array of sampled values.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct DiagnosticElementOffsets
{
    /// <summary>Pointer to the array of diagnostic sample values.</summary>
    [FieldOffset(0x0)] public long DiagnosticArray;

    /// <summary>X position of the element.</summary>
    [FieldOffset(0x10)] public int X;

    /// <summary>Y position of the element.</summary>
    [FieldOffset(0x14)] public int Y;

    /// <summary>Width of the element.</summary>
    [FieldOffset(0x18)] public int Width;

    /// <summary>Height of the element.</summary>
    [FieldOffset(0x1C)] public int Height;
}
