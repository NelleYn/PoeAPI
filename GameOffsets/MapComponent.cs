using System.Runtime.InteropServices;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct MapComponentBase
    {
        [FieldOffset(0x10)] public long Base;
        [FieldOffset(0x18)] public byte Tier;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct MapComponentInner
    {
        [FieldOffset(0x20)] public long Area;
        [FieldOffset(0x58)] public int MapSeries;
    }
}
