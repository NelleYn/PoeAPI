namespace ExileCore.PoEMemory.Elements;
public class PartyElementPlayerElement : Element
{
    private const int PlayerNameOffset = 888;
    private const int ZoneNameOffset = 1096;
    private const int TeleportButtonOffset = 992;
    public string PlayerName
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string ZoneName
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element TeleportButton => this;
}