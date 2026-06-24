using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.FilesInMemory;
public class TinctureDat : RemoteMemoryObject
{
    public BaseItemType BaseItemType => (BaseItemType)(object)this;
    public int DebuffInterval => this + 16;
    public int Cooldown => this + 20;

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}