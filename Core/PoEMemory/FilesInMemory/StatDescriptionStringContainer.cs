using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.FilesInMemory;
[StructLayout(LayoutKind.Explicit, Size = 168)]
public struct StatDescriptionStringContainer
{
    [FieldOffset(0)]
    public StdVector StatRangeVector;
    [FieldOffset(48)]
    public StdVector StatConverionVector;
    [FieldOffset(112)]
    public long StringPtr;
}