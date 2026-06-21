using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore;
public sealed record MouseMoveStroke
{
    [CompilerGenerated]
    private Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(MouseMoveStroke).TypeHandle;
        }
    }

    public List<MouseMoveStrokePoint> Points { get; init; }

    public MouseMoveStroke(List<MouseMoveStrokePoint> Points)
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
        return true;
    }

    [CompilerGenerated]
    public override int GetHashCode()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    public bool Equals(MouseMoveStroke? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    private MouseMoveStroke(MouseMoveStroke original)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out List<MouseMoveStrokePoint> Points)
    {
        Points = (List<MouseMoveStrokePoint>)(object)this;
    }
}