using System.Collections;
using System.Runtime.CompilerServices;

namespace ExileCore.Shared;
public class BuildTarget
{
    public string Name
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

    public string File
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

    public bool Succeeded
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

    public IEnumerable Outputs
    {
        [CompilerGenerated]
        get
        {
            return (IEnumerable)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }
}