// Partial extension that restores a nested type missing from the modernized source.
using System.Numerics;

namespace ExileCore.PoEMemory.MemoryObjects;

partial class Camera
{
    /// <summary>
    /// An immutable snapshot of the camera's view/projection matrix and half-viewport size,
    /// usable for thread-safe world-to-screen projection independent of the live camera.
    /// </summary>
    public sealed record CameraSnapshot(Matrix4x4 Matrix, Vector2 HalfSize)
    {
        /// <summary>Projects a world-space position to screen-space pixels using this snapshot.</summary>
        public Vector2 WorldToScreen(Vector3 vec)
        {
            var cord = new Vector4(vec, 1.0f);
            cord = Vector4.Transform(cord, Matrix);
            cord /= cord.W;
            return new Vector2((cord.X + 1.0f) * HalfSize.X, (1.0f - cord.Y) * HalfSize.Y);
        }
    }
}
