using System.Runtime.InteropServices;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct ServerInventoryOffsets
    {
        [FieldOffset(0x144)] public byte InventSlot;
        [FieldOffset(0x188)] public long InventorySlotItemsPtr;
    }
}
