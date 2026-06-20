using System;
using System.Runtime.CompilerServices;

namespace ExileCore.Shared.Attributes;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class SubmenuAttribute : Attribute
{
    public bool CollapsedByDefault
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (byte)(int)this != 0;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public bool EnableSelfDrawCollapsing
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (byte)(int)this != 0;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public bool EnableCollapsing
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (byte)(int)this != 0;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public string RenderMethod
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public SubmenuAttribute()
    {
        _ = 0;
        _ = 0;
        _ = 1;
    }
}