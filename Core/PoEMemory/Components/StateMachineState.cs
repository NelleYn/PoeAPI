using System.Runtime.CompilerServices;

namespace ExileCore.PoEMemory.Components;
public class StateMachineState
{
    public string Name
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }
    }

    public long Value
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I8, but got O
            return (long)this;
        }
    }

    public StateMachineState(string name, long value)
    {
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}