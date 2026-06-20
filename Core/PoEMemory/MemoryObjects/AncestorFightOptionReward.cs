using System;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.PoEMemory.FilesInMemory.Ancestor;

namespace ExileCore.PoEMemory.MemoryObjects;
public record AncestorFightOptionReward
{
    [CompilerGenerated]
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(AncestorFightOptionReward).TypeHandle;
        }
    }

    public AncestralTrialTribe FavorTribe
    {
        [CompilerGenerated]
        get
        {
            return (AncestralTrialTribe)(object)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public int FavorAmount
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (int)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public AncestorFightOptionReward(AncestralTrialTribe FavorTribe, int FavorAmount)
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
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    public override int GetHashCode()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    public virtual bool Equals(AncestorFightOptionReward? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected AncestorFightOptionReward(AncestorFightOptionReward original)
    {
    }

    [CompilerGenerated]
    public void Deconstruct(out AncestralTrialTribe FavorTribe, out int FavorAmount)
    {
        //IL_0006: Expected I4, but got O
        FavorTribe = (AncestralTrialTribe)(object)this;
        FavorAmount = (int)this;
    }
}