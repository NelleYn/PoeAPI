// Partial extension that restores a nested type missing from the modernized source.
using System.Runtime.InteropServices;

namespace ExileCore.PoEMemory.MemoryObjects;
partial class ServerData
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 9)]
    private struct QuestFlagStore
    {
        [FieldOffset(0)]
        public byte PartialId;
        [FieldOffset(1)]
        public ulong Flags;
    }
}