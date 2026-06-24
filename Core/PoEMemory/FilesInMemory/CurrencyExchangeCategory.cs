namespace ExileCore.PoEMemory.FilesInMemory;
public class CurrencyExchangeCategory : RemoteMemoryObject
{
    public string Name => (string)1;

    public string DisplayName
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got I4
            _ = this + 8;
            return (string)1;
        }
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}