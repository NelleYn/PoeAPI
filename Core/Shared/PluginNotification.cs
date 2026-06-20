using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.Shared;
public record PluginNotification
{
    [CompilerGenerated]
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(PluginNotification).TypeHandle;
        }
    }

    public string Category
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

    public string Id
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

    public PluginNotification(string Category, string Id, string Text)
    {
    }

    [CompilerGenerated]
    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        return true;
    }

    [CompilerGenerated]
    public override int GetHashCode()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    public virtual bool Equals(PluginNotification? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected PluginNotification(PluginNotification original)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out string Category, out string Id, out string Text)
    {
        Category = (string)(object)this;
        Id = (string)(object)this;
        Text = (string)(object)this;
    }
}