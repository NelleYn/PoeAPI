using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class ExpeditionAreaData : RemoteMemoryObject
{
    public const int StructSize = 192;
    private readonly CachedValue<ExpeditionAreaDataOffsets> _cachedValue;
    internal ExpeditionAreaDataOffsets ExpeditionAreaDataStruct => (ExpeditionAreaDataOffsets)this;

    public string Name
    {
        get
        {
            //IL_000f: Expected O, but got I4
            (new int[2])[1] = 8;
            return (string)1;
        }
    }

    public string Faction
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0013: Expected O, but got I4
            _ = this + 16;
            (new int[1])[0] = 8;
            return (string)1;
        }
    }

    public List<ItemMod> ImplicitMods => (List<ItemMod>)(object)this;

    private List<ItemMod> GetMods(long startOffset, long endOffset)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}