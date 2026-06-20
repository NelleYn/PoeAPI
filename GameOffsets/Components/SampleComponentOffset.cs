using System.Runtime.InteropServices;

namespace GameOffsets.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SampleComponentOffset
{
    [FieldOffset(0x0000)] public ComponentHeader Header;
}
