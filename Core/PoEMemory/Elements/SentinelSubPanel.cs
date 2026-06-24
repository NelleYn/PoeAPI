using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
public class SentinelSubPanel : Element
{
    public SentinelData SentinelData => (SentinelData)(object)this;

    public Entity SentinelItem
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}