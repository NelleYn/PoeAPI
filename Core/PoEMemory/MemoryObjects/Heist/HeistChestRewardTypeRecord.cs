namespace ExileCore.PoEMemory.MemoryObjects.Heist;
public class HeistChestRewardTypeRecord : RemoteMemoryObject
{
    public string Id => (string)1;

    public string Art
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got I4
            _ = this + 8;
            return (string)1;
        }
    }

    public string Name
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 16;
            return (string)1;
        }
    }

    public int MinimumDropLevel => this + 40;
    public int MaximumDropLevel => this + 44;
    public int Weight => this + 48;

    public string RoomName
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 52;
            return (string)1;
        }
    }

    public int RequiredJobLevel => this + 60;

    public HeistJobRecord RequiredJob
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 68;
            return (HeistJobRecord)(object)new int[1]
            {
                8
            };
        }
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}