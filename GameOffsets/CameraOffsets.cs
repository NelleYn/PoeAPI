using System.Runtime.InteropServices;
using SharpDX;

namespace GameOffsets;

/// <summary>
/// Maps the in-game camera: viewport dimensions, far clip plane, world position
/// and the view/projection matrix used to translate world coordinates to screen
/// space.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CameraOffsets
{
    /// <summary>Viewport width in pixels.</summary>
    [FieldOffset(0x4)] public int Width;

    /// <summary>Viewport height in pixels.</summary>
    [FieldOffset(0x8)] public int Height;

    /// <summary>Far clipping plane distance.</summary>
    [FieldOffset(0x1C8)] public float ZFar;

    /// <summary>Camera world position.</summary>
    [FieldOffset(0xD4)] public Vector3 Position;

    /// <summary>Camera view/projection matrix used for world-to-screen projection.</summary>
    //First value is changing when we change the screen size (ratio)
    //4 bytes before the matrix doesn't change
    [FieldOffset(0x7C)] public Matrix MatrixBytes;
}
