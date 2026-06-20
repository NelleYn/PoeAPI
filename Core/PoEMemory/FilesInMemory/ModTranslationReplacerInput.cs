using System;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.FilesInMemory;
public record ModTranslationReplacerInput
{
    [CompilerGenerated]
    protected virtual Type EqualityContract
    {
        [CompilerGenerated]
        get
        {
            return (Type)typeof(ModTranslationReplacerInput).TypeHandle;
        }
    }

    public GameStat Stat
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (GameStat)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public int RawValue
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

    public float ConvertedValue
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected F4, but got O
            return (float)this;
        }

        [CompilerGenerated]
        init
        {
        }
    }

    public string ConvertedValueString
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

    public ModTranslationReplacerInput(GameStat Stat, int RawValue, float ConvertedValue, string ConvertedValueString)
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
    public virtual bool Equals(ModTranslationReplacerInput? other)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    [CompilerGenerated]
    protected ModTranslationReplacerInput(ModTranslationReplacerInput original)
    {
    }

    [CompilerGenerated]
    public unsafe void Deconstruct(out GameStat Stat, out int RawValue, out float ConvertedValue, out string ConvertedValueString)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}