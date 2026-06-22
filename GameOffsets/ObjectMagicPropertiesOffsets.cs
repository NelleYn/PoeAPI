using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ObjectMagicPropertiesOffsets
    {
        [FieldOffset(0x144)] public int Rarity;
        [FieldOffset(0x168)] public NativePtrArray Mods;
    }
}
