using System;
using System.Numerics;
using ExileCore.Shared.Cache;
using SharpDX;

namespace ExileCore.PoEMemory.Elements;
public class SubMap : Element
{
    private readonly CachedValue<float> _mapScale;
    private readonly CachedValue<System.Numerics.Vector2> _mapCenter;
    public const double CameraAngle = 0.6754424205218056;
    public static readonly float CameraAngleCos;
    public static readonly float CameraAngleSin;
    [Obsolete]
    public SharpDX.Vector2 Shift => (SharpDX.Vector2)(this + (long)this);
    public System.Numerics.Vector2 ShiftNum => (System.Numerics.Vector2)(this + (long)this);

    [Obsolete]
    public SharpDX.Vector2 DefaultShift => (SharpDX.Vector2)(this + (long)this);
    public System.Numerics.Vector2 DefaultShiftNum => (System.Numerics.Vector2)(this + (long)this);
    public float Zoom => this + (long)this;
    public float MapScale => (float)this;
    public System.Numerics.Vector2 MapCenter => (System.Numerics.Vector2)this;

    static SubMap()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}