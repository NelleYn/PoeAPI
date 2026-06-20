using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements;
public class SocialElement : Element
{
    public Dictionary<SocialTabTypes, Element> SocialTabs
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Dictionary<SocialTabTypes, Element> SocialPanels
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public PartyTabElement PartyTab
    {
        get
        {
            _ = 2;
            return (PartyTabElement)(object)new int[4]
            {
                0,
                1,
                0,
                1
            };
        }
    }
}