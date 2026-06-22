using System.Runtime.InteropServices;
using SharpDX;

namespace GameOffsets;

/// <summary>
/// Maps the in-game camera: viewport dimensions, far clip plane, world position
/// and the view/projection matrix used to translate world coordinates to screen
/// space. Verified against client 328.8 via an in-process Marshal.OffsetOf dump.
/// </summary>
/// <remarks>
/// In 328.8 these values live in a nested CameraOffsetsInner at 0xA8; they are flattened
/// here at absolute offsets (0xA8 + inner offset) so the camera projection code is unchanged.
/// System.Numerics.Matrix4x4/Vector3 are read as the layout-identical SharpDX.Matrix/Vector3.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CameraOffsets
{
    /// <summary>Camera view/projection matrix used for world-to-screen projection.</summary>
    [FieldOffset(0x1A8)] public Matrix MatrixBytes;

    /// <summary>Camera world position.</summary>
    [FieldOffset(0x21C)] public Vector3 Position;

    /// <summary>Far clipping plane distance.</summary>
    [FieldOffset(0x2BC)] public float ZFar;

    /// <summary>Viewport width in pixels.</summary>
    [FieldOffset(0x318)] public int Width;

    /// <summary>Viewport height in pixels.</summary>
    [FieldOffset(0x31C)] public int Height;
}
