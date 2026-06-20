namespace ExileCore.PoEMemory.FilesInMemory;
public class MiscAnimatedDat : RemoteMemoryObject
{
    public string Id => (string)1;

    public string AOFile
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got I4
            _ = this + 8;
            return (string)1;
        }
    }

    public int BaseSize => this + 36;
    public uint Hash => (uint)(this + 40);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}