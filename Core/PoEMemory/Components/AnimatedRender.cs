using System.Numerics;

namespace ExileCore.PoEMemory.Components;
public class AnimatedRender : Component
{
    public Matrix4x4 Matrix => (Matrix4x4)(this + (long)this);
    public Matrix4x4 Matrix2 => (Matrix4x4)(this + (long)this);
    public Matrix4x4 Matrix3 => (Matrix4x4)(this + (long)this);

    public unsafe Vector3 Scale
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public unsafe Vector3 Scale2
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public unsafe Vector3 Scale3
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}