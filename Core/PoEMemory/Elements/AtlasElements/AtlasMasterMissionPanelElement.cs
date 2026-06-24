using System;
using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements.AtlasElements;
public class AtlasMasterMissionPanelElement : Element
{
    public Element AtlasMasterMissionInfoIcon => (Element)0;
    public Element AtlasMasterMissions => (Element)(object)new int[2]
    {
        1,
        0
    };

    [Obsolete]
    public Dictionary<MasterMissionColour, int> EinharMissions
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Dictionary<MasterMissionColour, int> AlvaMissions
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Dictionary<MasterMissionColour, int> NikoMissions
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Dictionary<MasterMissionColour, int> JunMissions
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Dictionary<MasterMissionColour, int> KiracMissions
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}