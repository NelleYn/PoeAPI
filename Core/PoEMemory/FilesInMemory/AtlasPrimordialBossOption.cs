namespace ExileCore.PoEMemory.FilesInMemory;
public class AtlasPrimordialBossOption : RemoteMemoryObject
{
    public int OptionValue => (int)this;

    public string Name
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 88;
            return (string)1;
        }
    }

    public ClientString Description => (ClientString)(this + 40);
    public ClientString DescriptionActive => (ClientString)(this + 56);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}