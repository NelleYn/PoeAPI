using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.PoEMemory.FilesInMemory;
public record CachedStatDescriptionSection
{
    [CompilerGenerated]
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(CachedStatDescriptionSection).TypeHandle;
        }
    }

    public List<(int Min, int Max)> StatRanges
    {
        [CompilerGenerated]
        get
        {
            return (List<(int Min, int Max)>)(object)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public List<StatHandling> StatConversionTypes
    {
        [CompilerGenerated]
        get
        {
            return (List<StatHandling>)(object)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public string String
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

    public CachedStatDescriptionSection(List<(int Min, int Max)> StatRanges, List<StatHandling> StatConversionTypes, string String)
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
    public virtual bool Equals(CachedStatDescriptionSection? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected CachedStatDescriptionSection(CachedStatDescriptionSection original)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out List<(int Min, int Max)> StatRanges, out List<StatHandling> StatConversionTypes, out string String)
    {
        StatRanges = (List<(int Min, int Max)>)(object)this;
        StatConversionTypes = (List<StatHandling>)(object)this;
        String = (string)(object)this;
    }
}