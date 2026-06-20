using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps the data behind a diagnostic graph element (e.g. FPS/latency overlay):
/// a fixed-size ring of sampled values plus the most recent value.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct DiagnosticElementArrayOffsets
{
    /// <summary>Fixed-size buffer of historical sample values.</summary>
    [FieldOffset(0x0)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
    public float[] Values;

    /// <summary>Most recent sampled value.</summary>
    [FieldOffset(0x13C)] public float CurrValue;
}
