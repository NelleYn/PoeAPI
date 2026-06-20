using System.Runtime.InteropServices;

namespace GameOffsets.Native;

/// <summary>
/// A pair of integer bounds (minimum and maximum) read directly from game memory.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MinMaxStruct
{
    /// <summary>The lower bound of the range.</summary>
    public int Min;

    /// <summary>The upper bound of the range.</summary>
    public int Max;
}
