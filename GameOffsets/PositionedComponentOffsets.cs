using System.Runtime.InteropServices;
using GameOffsets.Native;
using SharpDX;

namespace GameOffsets
{
    // Offsets verified against client 328.8 via an in-process Marshal.OffsetOf dump.
    // System.Numerics.Vector2 fields are declared as SharpDX.Vector2 here: the two types
    // are layout-identical (two sequential floats), and this keeps the Core API SharpDX-based.
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct PositionedComponentOffsets
    {
        [FieldOffset(0x8)] public long OwnerAddress;
        [FieldOffset(0x1E0)] public byte Reaction;
        [FieldOffset(0x1E5)] public int Size;
        [FieldOffset(0x208)] public Vector2i RawVelocity;
        [FieldOffset(0x23C)] public float SpeedReverseFactor;
        [FieldOffset(0x244)] public Vector2 PrevPosition;
        [FieldOffset(0x250)] public Vector2 TravelStart;
        [FieldOffset(0x268)] public Vector2 TravelOffset;
        [FieldOffset(0x284)] public float TravelProgress;
        [FieldOffset(0x294)] public Vector2i GridPosition;
        [FieldOffset(0x29C)] public float Rotation;
        [FieldOffset(0x2B0)] public float Scale;
        [FieldOffset(0x2B8)] public Vector2 WorldPosition;
    }
}
