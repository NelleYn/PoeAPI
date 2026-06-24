// Partial extension that restores a nested type missing from the modernized source.
using System.Runtime.InteropServices;

namespace ExileCore.PoEMemory.MemoryObjects;
partial class IngameData
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 3)]
    private struct TileIndexStruct
    {
        [FieldOffset(0)]
        public byte Index1;
        [FieldOffset(1)]
        public byte Index2;
        [FieldOffset(2)]
        public byte Index3;
    }
}