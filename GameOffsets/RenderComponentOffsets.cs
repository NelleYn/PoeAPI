using System.Runtime.InteropServices;
using GameOffsets.Native;
using SharpDX;

namespace GameOffsets
{
    // Offsets verified against client 328.8 via an in-process Marshal.OffsetOf dump.
    // Vector3 fields are System.Numerics.Vector3 in the client; SharpDX.Vector3 is
    // layout-identical and keeps the Core API SharpDX-based.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct RenderComponentOffsets
    {
        [FieldOffset(0x120)] public Vector3 Pos;
        [FieldOffset(0x12C)] public Vector3 Bounds;
        [FieldOffset(0x148)] public NativeStringU Name;
        [FieldOffset(0x168)] public Vector3 Rotation;
        [FieldOffset(0x184)] public float Height;
    }
}
