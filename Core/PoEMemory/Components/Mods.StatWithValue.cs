// Partial extension that restores a nested type missing from the modernized source.
using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;
partial class Mods
{
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct StatWithValue
    {
        [FieldOffset(0)]
        public NativeUtf16Text Text;
    }
}