using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets;

/// <summary>
/// Maps the in-game cursor element, exposing the current cursor action, click
/// count and the action's display string.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CursorOffsets
{
    /// <summary>Shared buffer padding applied to UI element offsets.</summary>
    public const int OffsetBuffers = 0x6EC;

    /// <summary>Pointer to the object's virtual method table.</summary>
    [FieldOffset(0x0)] public int vTable;

    /// <summary>Current cursor action identifier.</summary>
    [FieldOffset(0x238)] public int Action;

    /// <summary>Number of clicks registered.</summary>
    [FieldOffset(0x24C)] public int Clicks;

    /// <summary>Display string describing the current cursor action.</summary>
    [FieldOffset(0x2A0)] public NativeStringU ActionString;
}
