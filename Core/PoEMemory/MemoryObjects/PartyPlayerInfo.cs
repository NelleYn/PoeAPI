namespace ExileCore.PoEMemory.MemoryObjects;
public class PartyPlayerInfo : RemoteMemoryObject
{
    public PartyPlayerInfoType Type => (PartyPlayerInfoType)this;
    public RemotePlayerInfo PlayerInfo => (RemotePlayerInfo)8;
}