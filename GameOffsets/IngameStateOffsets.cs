using System.Runtime.InteropServices;

namespace GameOffsets
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct IngameStateOffsets
    {
        // Measured against the installed client on 2026-09-15, not inherited from the old build.
        // The reference distribution reports IngameState.Data at this offset for the same process,
        // 0x218 is the only match in a 0x2000 window, and the object it points at carries this
        // zone's level and hash. Neighbouring fields below are still from the old build.
        [FieldOffset(0x218)] public long Data;
        // NOT a field of IngameState on this client, and not read by anything: the true address
        // of ServerData does not occur anywhere in the first 0x2000 bytes of this object. It lives
        // inside IngameData instead (IngameDataOffsets.ServerData, measured at 0x968). Kept only so
        // that the shape of the struct stays comparable with the reference's.
        [FieldOffset(0x378)] public long ServerData;
        [FieldOffset(0x78)] public long IngameUi;
        [FieldOffset(0x4A0)] public long UIRoot;
        [FieldOffset(0x4E8)] public long UIHover;
        [FieldOffset(0x51C)] public float UIHoverX;
        [FieldOffset(0x520)] public float UIHoverY;
        [FieldOffset(0x4E8)] public long UIHoverTooltip;
        [FieldOffset(0x4E0)] public float CurentUElementPosX;
        [FieldOffset(0x4E4)] public float CurentUElementPosY;
        [FieldOffset(0x98)] public long EntityLabelMap;
        [FieldOffset(0x568)] public int DiagnosticInfoType;
        [FieldOffset(0x798)] public long LatencyRectangle;
        [FieldOffset(0xC28)] public long FrameTimeRectangle;
        [FieldOffset(0xE70)] public long FPSRectangle;
        [FieldOffset(0x554)] public float TimeInGame;
        [FieldOffset(0x558)] public float TimeInGameF;
        [FieldOffset(0xF4C)] public int Camera;
        [FieldOffset(0x524)] public int MouseXGlobal;
        [FieldOffset(0x524)] public int MouseYGlobal;
        [FieldOffset(0x524)] public float MouseXInGame;
        [FieldOffset(0x528)] public float MouseYInGame;
    }
}
