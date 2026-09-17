using System;
using ExileCore.Shared.Cache;
using GameOffsets;
using SharpDX;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class Camera : RemoteMemoryObject
    {
        private static Vector2 oldplayerCord;
        private readonly CachedValue<CameraOffsets> _cachedValue;

        public Camera()
        {
            _cachedValue = new FrameCache<CameraOffsets>(() => M.Read<CameraOffsets>(Address));
        }

        public CameraOffsets CameraOffsets => _cachedValue.Value;
        public int Width => CameraOffsets.Width;
        public int Height => CameraOffsets.Height;

        // Derived at the point of use, NOT pushed in by the cache's OnUpdate event. The event form
        // was a silent-zero waiting to happen: it leaves the two halves at 0 until the cache first
        // refreshes, and CachedValue.Value has a branch that returns a freshly computed value
        // WITHOUT firing OnUpdate at all. A zero here produces exactly the failure this camera was
        // just repaired from -- WorldToScreen returning the same point for every input, quietly.
        private float HalfWidth => CameraOffsets.Width * 0.5f;
        private float HalfHeight => CameraOffsets.Height * 0.5f;

        public Vector2 Size => new Vector2(Width, Height);
        public float ZNear => CameraOffsets.ZNear;
        public float ZFar => CameraOffsets.ZFar;
        public Vector3 Position => CameraOffsets.Position;
        public string PositionString => Position.ToString();

        //cameraarray 0x17c
        private Matrix Matrix => CameraOffsets.MatrixBytes;

        public unsafe Vector2 WorldToScreen(Vector3 vec /*, Entity Entity*/)
        {
            try
            {
                /*
                var localPlayer = TheGame.IngameState.Data.LocalPlayer;
                var isplayer = localPlayer.Address == Entity.Address && localPlayer.IsValid;
                var isMoving = false;
                if (isplayer) isMoving = Entity.GetComponent<Actor>().isMoving;
*/

                Vector2 result;
                var cord = *(Vector4*) &vec;
                cord.W = 1;
                cord = Vector4.Transform(cord, Matrix);
                cord = Vector4.Divide(cord, cord.W);
                result.X = (cord.X + 1.0f) * HalfWidth;
                result.Y = (1.0f - cord.Y) * HalfHeight;
                /*
                if (!isplayer) return result;
                if (isMoving)
                {
                    if (Math.Abs(oldplayerCord.X - result.X) < 50 || Math.Abs(oldplayerCord.X - result.Y) < 50)
                        result = oldplayerCord;
                    else
                        oldplayerCord = result;
                }
                else oldplayerCord = result;
*/

                return result;
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"Camera WorldToScreen {ex}");
            }

            return Vector2.Zero;
        }
    }
}
