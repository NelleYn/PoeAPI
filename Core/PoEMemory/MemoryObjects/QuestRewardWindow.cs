using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;
public class QuestRewardWindow : Element
{
    public Element CancelButton => (Element)3;
    public Element SelectOneRewardString => (Element)0;

    public IList<(Entity, Element)> GetPossibleRewards()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}