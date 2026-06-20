// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.PoEMemory.MemoryObjects;
partial class Camera
{
    public sealed record CameraSnapshot
    {
        [CompilerGenerated]
        private Type EqualityContract
        {
            [CompilerGenerated]
            get
            {
                return (Type)typeof(CameraSnapshot).TypeHandle;
            }
        }

        public Matrix4x4 Matrix
        {
            [CompilerGenerated]
            get
            {
                return (Matrix4x4)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public Vector2 HalfSize
        {
            [CompilerGenerated]
            get
            {
                return (Vector2)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public CameraSnapshot(Matrix4x4 Matrix, Vector2 HalfSize)
        {
        }

        public Vector2 WorldToScreen(Vector3 vec)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        private bool PrintMembers(StringBuilder builder)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public override int GetHashCode()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public bool Equals(CameraSnapshot? other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        private CameraSnapshot(CameraSnapshot original)
        {
        }

        [CompilerGenerated]
        public void Deconstruct(out Matrix4x4 Matrix, out Vector2 HalfSize)
        {
            Matrix = (Matrix4x4)this;
            HalfSize = (Vector2)this;
        }
    }
}