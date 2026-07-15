namespace ExileCore.PoEMemory.Elements;
public class DropdownElementOption : RemoteMemoryObject
{
    public string Name
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 16;
            return (string)(object)this;
        }
    }

    public override string ToString()
    {
        return Name;
    }
}