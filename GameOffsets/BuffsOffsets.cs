using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    // Buffs component (328.8, in-process Marshal.OffsetOf dump). In 328.8 buffs were moved out
    // of the Life component into their own component. Buffs is a NativePtrArray of pointers;
    // each element points to a small wrapper whose +0x8 field is the actual buff object.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct BuffsOffsets
    {
        [FieldOffset(0x160)] public NativePtrArray Buffs;
    }
}
