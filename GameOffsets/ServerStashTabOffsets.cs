using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ServerStashTabOffsets
    {
        [FieldOffset(0x8)] public NativeStringU Name;
        [FieldOffset(0x2C)] public uint Color;
        [FieldOffset(0x31)] public byte MemberFlags;
        [FieldOffset(0x32)] public byte OfficerFlags;
        [FieldOffset(0x34)] public uint TabType;
        [FieldOffset(0x38)] public ushort DisplayIndex;
        [FieldOffset(0x3D)] public byte Flags;
    }
}
