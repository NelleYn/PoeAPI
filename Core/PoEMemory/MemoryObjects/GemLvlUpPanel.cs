using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;
public class GemLvlUpPanel : Element
{
    public Element LevelUpAllGemsButton => (Element)(object)new int[3];

    public List<GemLevelUpElement> GemsToLvlUp
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<(Entity, GemLevelUpElement)> Gems
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element LvlUpButtonForGem(Element gem)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public bool MeetRequirementForGem(Element gem)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public Element TextForGem(Element gem)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}