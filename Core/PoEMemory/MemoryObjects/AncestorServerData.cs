using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;
public class AncestorServerData : RemoteMemoryObject
{
    private const int FightDataOffset = 456;
    public List<AncestorFightOption> Options
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}