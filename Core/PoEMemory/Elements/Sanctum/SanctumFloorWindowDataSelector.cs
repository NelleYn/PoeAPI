using System.Runtime.CompilerServices;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.Sanctum;
public class SanctumFloorWindowDataSelector : RemoteMemoryObject
{
    private readonly CachedValue<SanctumFloorWindowDataOffsets> _cachedValue;
    public bool IsOutsidePtr
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

    public SanctumFloorData FloorData
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int Gold => (int)this;
    public int CurrentResolve => (int)this;
    public int MaxResolve => (int)this;
    public int Inspiration => (int)this;
}