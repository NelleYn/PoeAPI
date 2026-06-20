using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Components;
public class MapKey : Component
{
    public MapKeyDat Info
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public byte Tier => (byte)(int)this;
}