using System.Runtime.InteropServices;

namespace GameOffsets
{
    // Offsets verified against client 328.8 via an in-process Marshal.OffsetOf dump. In 328.8 the
    // fake/real cursor positions and the inventory size are Vector2i values (FakePos@0x300,
    // RealPos@0x308, InventorySize@0x5D8); the X/Y components are exposed here under the original
    // field names so the inventory element code is unchanged.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct InventoryOffsets
    {
        [FieldOffset(0x250)] public int ItemHoverState;
        [FieldOffset(0x2F8)] public long HoverItem;
        [FieldOffset(0x300)] public int XFake;
        [FieldOffset(0x304)] public int YFake;
        [FieldOffset(0x308)] public int XReal;
        [FieldOffset(0x30C)] public int YReal;
        [FieldOffset(0x318)] public int CursorInInventory;
        [FieldOffset(0x480)] public long ItemCount;
        [FieldOffset(0x5B0)] public int ServerInventoryId;
        [FieldOffset(0x5D8)] public int TotalBoxesInInventoryRow;
        [FieldOffset(0x5F0)] public long CursorPtr;
    }
}
