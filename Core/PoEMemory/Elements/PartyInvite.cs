namespace ExileCore.PoEMemory.Elements;
public class PartyInvite : SocialPartyMember
{
    public Element AcceptButton => (Element)(object)new int[3]
    {
        0,
        2,
        0
    };
    public Element DeclineButton => (Element)(object)new int[3]
    {
        0,
        2,
        1
    };
}