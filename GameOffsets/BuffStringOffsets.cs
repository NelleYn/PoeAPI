using System.Runtime.InteropServices;

namespace GameOffsets;

/// <summary>
/// Maps a buff-name entry: a single pointer to the buff's name string.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BuffStringOffsets
{
    /// <summary>Pointer to the buff name string.</summary>
    public long String;
}
