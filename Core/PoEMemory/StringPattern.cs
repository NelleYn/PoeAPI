using System.Runtime.CompilerServices;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory;
public class StringPattern : IPattern
{
    private string _mask;
    public string Name
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }
    }

    public byte[] Bytes
    {
        [CompilerGenerated]
        get
        {
            return (byte[])(object)this;
        }
    }

    public bool[] Mask
    {
        [CompilerGenerated]
        get
        {
            return (bool[])(object)this;
        }
    }

    public int StartOffset
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

    public int PatternOffset
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (int)this;
        }
    }

    string IPattern.Mask
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public StringPattern(string pattern, string name)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}