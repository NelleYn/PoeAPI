using System.Runtime.InteropServices;

namespace GameOffsets
{
    // Item/Width/Height verified against client 328.8 via an in-process Marshal.OffsetOf dump.
    // InventPosX/InventPosY/ToolTip are not present in the 328.8 struct dump; their offsets below
    // are carried over from the previous version and are UNVERIFIED for 328.8 (TODO: confirm or
    // derive the item's grid position from the element geometry instead).
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct NormalInventoryItemOffsets
    {
        [FieldOffset(0x410)] public long Item;
        [FieldOffset(0x4B4)] public int Width;
        [FieldOffset(0x4B8)] public int Height;

        [FieldOffset(0x390)] public int InventPosX;
        [FieldOffset(0x394)] public int InventPosY;
        [FieldOffset(0x14)] public int ToolTip;
    }
}
