using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct RenderItem
{
    [FieldOffset(0x0000)] public ComponentHeader Header;
    [FieldOffset(0x0020)] public NativeUnicodeText ResourcePath;
}
