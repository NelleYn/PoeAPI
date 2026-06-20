using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements;
public class PartyElement : Element
{
    private const long InformationOffset = 848L;
    public Dictionary<string, PartyElementPlayerInfo> Information
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<PartyElementPlayerElement> PlayerElements => (List<PartyElementPlayerElement>)(object)new int[2];
}