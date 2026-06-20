using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;
public class AtlasInfluencedRegion : RemoteMemoryObject
{
    public int Quadrant => (int)this;
    public AtlasMissionType Type => (AtlasMissionType)(this + 8);

    public List<AtlasInfluencedMap> Maps
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_000a: Expected O, but got I4
            _ = this + 24;
            return (List<AtlasInfluencedMap>)48;
        }
    }
}