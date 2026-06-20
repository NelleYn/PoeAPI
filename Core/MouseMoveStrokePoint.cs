using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore;
public sealed record MouseMoveStrokePoint
{
    [CompilerGenerated]
    private Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(MouseMoveStrokePoint).TypeHandle;
        }
    }

    public Vector2 Point
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

    public TimeSpan Delay
    {
        [CompilerGenerated]
        get
        {
            return (TimeSpan)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public MouseMoveStrokePoint(Vector2 Point, TimeSpan Delay)
    {
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
    public bool Equals(MouseMoveStrokePoint? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    private MouseMoveStrokePoint(MouseMoveStrokePoint original)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out Vector2 Point, out TimeSpan Delay)
    {
        Point = (Vector2)this;
        Delay = (TimeSpan)this;
    }
}