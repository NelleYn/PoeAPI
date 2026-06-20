using System.Runtime.InteropServices;

namespace GameOffsets.Native;

/// <summary>
/// A node of one of the game's native doubly-linked lists.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NativeListNode
{
    public long Next;
    public long Prev;
    public long Ptr1_Unused;
    public long Ptr2_Key;
    public int Value;
}
