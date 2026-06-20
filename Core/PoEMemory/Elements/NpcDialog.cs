using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements;
public class NpcDialog : Element
{
    public string NpcName
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Element NpcLineWrapper => (Element)(object)new int[1]
    {
        1
    };
    public List<NpcLine> NpcLines => (List<NpcLine>)(object)this;

    public bool IsLoreTalkVisible
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private List<NpcLine> GetNpcLines()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}