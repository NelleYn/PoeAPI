namespace ExileCore.PoEMemory.Elements;
public class ChallengePanelTabContainerTabInfo : RemoteMemoryObject
{
    public Element TabElement => (Element)0;

    public string Title
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
        return Title;
    }
}