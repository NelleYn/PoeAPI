using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
public class SyndicatePanel : Element
{
    private const int EventChildOffset1 = 984;
    private const int EventChildOffset2 = 976;
    private const int StatesOffset = 960;
    public BetrayalEventData EventElement
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element TextElement
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string EventText
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public BetrayalSyndicateLeadersData SyndicateLeadersData => (BetrayalSyndicateLeadersData)(this + (this + (long)this));

    public List<BetrayalSyndicateState> SyndicateStates
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public BetrayalEventData BetrayalEventData
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}