namespace ExileCore.PoEMemory.FilesInMemory.Sanctum;
public class SanctumPersistentEffect : RemoteMemoryObject
{
    public string Id => (string)1;

    public string ReadableName
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 40;
            return (string)1;
        }
    }

    public string Description
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 105;
            return (string)1;
        }
    }

    public override string ToString()
    {
        return Id;
    }
}