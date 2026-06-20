using System.Runtime.InteropServices;

namespace GameOffsets.Components;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AreaTransition
{
    [FieldOffset(0x0000)] public ComponentHeader Header;
    [FieldOffset(0x0028)] public ushort AreaId;
    [FieldOffset(0x002A)] public byte TransitionType;
    [FieldOffset(0x0048)] public long WorldAreaInfo;
}
