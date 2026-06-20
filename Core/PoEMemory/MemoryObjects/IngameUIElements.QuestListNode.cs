// Partial extension that restores a nested type missing from the modernized source.
using System.Runtime.InteropServices;

namespace ExileCore.PoEMemory.MemoryObjects;
partial class IngameUIElements
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct QuestListNode
    {
        public long Next;
        public long Prev;
        public long Ptr2_Key;
        public long Ptr1_Unused;
        public char Value;
    }
}