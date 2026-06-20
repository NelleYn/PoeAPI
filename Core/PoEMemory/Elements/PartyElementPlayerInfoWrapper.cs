namespace ExileCore.PoEMemory.Elements;
public class PartyElementPlayerInfoWrapper : RemoteMemoryObject
{
    public string PlayerName => (string)(object)this;
    public PartyElementPlayerInfo Info => (PartyElementPlayerInfo)32;
}