using System.Runtime.InteropServices;

namespace GameOffsets
{
    // Verified against client 328.8 via an in-process Marshal.OffsetOf dump: Data, Camera,
    // EntityLabelMap, UIRoot, UIHover, IngameUi, TimeInGameF and the mouse/hover positions
    // (the latter are Vector2/Vector2i in 328.8, exposed here component-wise).
    // Fields NOT present in the dump (ServerData and the diagnostic rectangles) keep their
    // previous offsets and are UNVERIFIED for 328.8.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct IngameStateOffsets
    {
        [FieldOffset(0x218)] public long Data;
        [FieldOffset(0x270)] public long Camera;
        [FieldOffset(0x298)] public long EntityLabelMap;
        [FieldOffset(0x518)] public long UIRoot;
        [FieldOffset(0x590)] public long UIHover;
        [FieldOffset(0x8F0)] public long IngameUi;
        [FieldOffset(0x558)] public float CurentUElementPosX;
        [FieldOffset(0x55C)] public float CurentUElementPosY;
        [FieldOffset(0x5C4)] public float UIHoverX;
        [FieldOffset(0x5C8)] public float UIHoverY;
        [FieldOffset(0x5B8)] public int MouseXGlobal;
        [FieldOffset(0x5BC)] public int MouseYGlobal;
        [FieldOffset(0x5CC)] public float MouseXInGame;
        [FieldOffset(0x5D0)] public float MouseYInGame;
        [FieldOffset(0x8AC)] public float TimeInGame;
        [FieldOffset(0x8AC)] public float TimeInGameF;

        // --- not present in the 328.8 dump; offsets carried over and UNVERIFIED ---
        [FieldOffset(0x378)] public long ServerData;
        [FieldOffset(0x4E8)] public long UIHoverTooltip;
        [FieldOffset(0x568)] public int DiagnosticInfoType;
        [FieldOffset(0x798)] public long LatencyRectangle;
        [FieldOffset(0xC28)] public long FrameTimeRectangle;
        [FieldOffset(0xE70)] public long FPSRectangle;
    }
}
