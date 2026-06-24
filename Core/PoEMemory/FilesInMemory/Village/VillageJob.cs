namespace ExileCore.PoEMemory.FilesInMemory.Village;
public class VillageJob : RemoteMemoryObject
{
    private string _name;
    private int? _discriminator;
    private VillageJobType _type;
    public VillageJobType Type
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int Discriminator
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string Name
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}