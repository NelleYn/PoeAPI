using System;
using System.Runtime.CompilerServices;
using System.Text;
using Serilog.Events;
using SharpDX;

namespace ExileCore;
public record DebugMessage
{
    [CompilerGenerated]
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(DebugMessage).TypeHandle;
        }
    }

    public string Text
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public float? Duration
    {
        [CompilerGenerated]
        get
        {
            return (float? )this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public Color? Color
    {
        [CompilerGenerated]
        get
        {
            return (Color? )this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public LogEventLevel Level
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (LogEventLevel)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public DebugMessage(string Text)
    {
        _ = 2;
    }

    [CompilerGenerated]
    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    public override int GetHashCode()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    public virtual bool Equals(DebugMessage? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected DebugMessage(DebugMessage original)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out string Text)
    {
        Text = (string)(object)this;
    }
}