namespace ExileCore.PoEMemory.MemoryObjects;
public class RemotePlayerInfo : RemoteMemoryObject
{
    public string AccountName => (string)(object)this;

    public string CharacterName
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 64;
            return (string)(object)this;
        }
    }

    public string League
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 96;
            return (string)(object)this;
        }
    }

    public WorldArea Area => (WorldArea)(this + (long)this);

    public override string ToString()
    {
        return $"{CharacterName} ({AccountName})";
    }
}