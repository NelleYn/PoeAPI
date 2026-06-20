namespace ExileCore.PoEMemory.FilesInMemory;
public class AtlasPrimordialAltarChoice : RemoteMemoryObject
{
    public ModsDat.ModRecord Mod => (ModsDat.ModRecord)(object)this;
    public AtlasPrimordialAltarChoiceType Type => (AtlasPrimordialAltarChoiceType)(this + 16);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}