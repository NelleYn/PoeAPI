using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements;

public class NpcDialog : Element
{
    public string NpcName => GetChildAtIndex(1)?.GetChildAtIndex(3)?.Text;
    public Element NpcLineWrapper => GetChildAtIndex(0)?.GetChildAtIndex(2);
    public List<NpcLine> NpcLines => GetNpcLines();
    public bool IsLoreTalkVisible => NpcLines.Count == 0 && IsVisible;

    private List<NpcLine> GetNpcLines()
    {
        var npcLines = new List<NpcLine>();
        if (NpcLineWrapper?.Children == null)
        {
            DebugWindow.LogError("NpcLineWrapper?.Children is null, check offsets");
            return npcLines;
        }

        foreach (var line in NpcLineWrapper.Children)
        {
            try
            {
                npcLines.Add(new NpcLine(line));
            }
            catch
            {
                continue;
            }
        }

        return npcLines;
    }
}
