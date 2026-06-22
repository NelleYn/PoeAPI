using System.Runtime.InteropServices;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct TargetableComponentOffsets
    {
        [FieldOffset(0x50)] public bool isTargetable;
        [FieldOffset(0x52)] public bool isTargeted;
    }
}
