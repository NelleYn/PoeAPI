namespace ExileCore.PoEMemory.Elements;
public class PartyElementPlayerInfo : RemoteMemoryObject
{
    public bool IsInDifferentZone => this + 48 > 0;
}