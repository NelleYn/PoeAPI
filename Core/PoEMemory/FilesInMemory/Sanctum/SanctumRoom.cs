namespace ExileCore.PoEMemory.FilesInMemory.Sanctum;
public class SanctumRoom : RemoteMemoryObject
{
    private SanctumRoomType _roomType;
    public string Id => (string)1;

    public string Arm
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got I4
            _ = this + 8;
            return (string)1;
        }
    }

    public string Teleports
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 32;
            return (string)1;
        }
    }

    public SanctumRoomType RoomType
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