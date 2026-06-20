using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;
public class AtlasInfluencedMap : RemoteMemoryObject
{
    public AtlasNode Node => (AtlasNode)(object)this;
    public List<ItemMod> Mods => (List<ItemMod>)(this + 16);
}