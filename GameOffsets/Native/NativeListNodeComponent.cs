using System.Runtime.InteropServices;

namespace GameOffsets.Native;

/// <summary>
/// A node of the game's native linked list that maps a component name to its component list entry.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct NativeListNodeComponent
{
    [FieldOffset(0x0)] public long Next;
    [FieldOffset(0x8)] public long Prev;
    [FieldOffset(0x10)] public long String;
    [FieldOffset(0x18)] public int ComponentList;

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Next: {Next} Prev: {Prev} String: {String} ComponentList: {ComponentList}";
    }
}
