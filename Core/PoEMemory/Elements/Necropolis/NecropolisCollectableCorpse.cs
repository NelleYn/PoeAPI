using System.Collections.Generic;
using ExileCore.PoEMemory.Components;

namespace ExileCore.PoEMemory.Elements.Necropolis;
public class NecropolisCollectableCorpse : Element
{
    private const long FirstCorpseDictOffset = 584L;
    private const int SecondCorpseDictOffset = 352;
    public Dictionary<uint, NecropolisCorpse> CorpsesByEntityId
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}