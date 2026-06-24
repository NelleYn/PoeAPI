using System.Runtime.CompilerServices;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.Components;
public class HarvestInfrastructureMod
{
    public int ModLevel
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (int)this;
        }
    }

    public string ModName
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }
    }

    internal HarvestInfrastructureMod(HarvestInfrastructureModUnmanaged data, IMemory m)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}