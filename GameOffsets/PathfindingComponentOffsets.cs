using System.Runtime.InteropServices;
using GameOffsets.Native;

namespace GameOffsets
{
    // DestinationNodes/WantMoveToPosition/StayTime verified against client 328.8 via an in-process
    // Marshal.OffsetOf dump. ClickToNextPosition/WasInThisPosition/IsMoving are not present in the
    // 328.8 dump; their offsets are carried over and are UNVERIFIED for 328.8 (movement state can
    // also be read from the Actor component).
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct PathfindingComponentOffsets
    {
        [FieldOffset(0x518)] public int DestinationNodes;
        [FieldOffset(0x550)] public Vector2i WantMoveToPosition;
        [FieldOffset(0x55C)] public float StayTime;

        [FieldOffset(0x28)] public Vector2i ClickToNextPosition;
        [FieldOffset(0x30)] public Vector2i WasInThisPosition;
        [FieldOffset(0x470)] public byte IsMoving;
    }
}
