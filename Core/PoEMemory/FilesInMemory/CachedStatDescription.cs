using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.FilesInMemory;
public record CachedStatDescription
{
    [CompilerGenerated]
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(CachedStatDescription).TypeHandle;
        }
    }

    public List<GameStat> Stats
    {
        [CompilerGenerated]
        get
        {
            return (List<GameStat>)(object)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public List<CachedStatDescriptionSection> Sections
    {
        [CompilerGenerated]
        get
        {
            return (List<CachedStatDescriptionSection>)(object)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public CachedStatDescription(List<GameStat> Stats, List<CachedStatDescriptionSection> Sections)
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
    public virtual bool Equals(CachedStatDescription? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected CachedStatDescription(CachedStatDescription original)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out List<GameStat> Stats, out List<CachedStatDescriptionSection> Sections)
    {
        Stats = (List<GameStat>)(object)this;
        Sections = (List<CachedStatDescriptionSection>)(object)this;
    }
}