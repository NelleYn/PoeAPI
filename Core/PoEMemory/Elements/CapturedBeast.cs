namespace ExileCore.PoEMemory.Elements;
public class CapturedBeast : Element
{
    private const int BeastIdOffset = 920;
    private const int BeastLevelOffset = 938;
    private const long NameOffset = 1264L;
    public Element ReleaseButton => (Element)3;
    public short BeastId => (short)(int)(this + (long)this);
    public byte BeastLevel => (byte)(int)(this + (long)this);

    public string Name
    {
        get
        {
            //IL_0003: Unknown result type (might be due to invalid IL or missing references)
            _ = this + (long)this;
            return (string)(object)this;
        }
    }
}