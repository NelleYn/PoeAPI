using System.Runtime.InteropServices;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct MinimapIconOffsets
    {
        [FieldOffset(0x20)] public long NamePtr;
        [FieldOffset(0x38)] public byte IsVisible;
        [FieldOffset(0x3C)] public byte IsHide;
    }
}
