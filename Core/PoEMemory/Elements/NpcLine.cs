using System.Runtime.CompilerServices;

namespace ExileCore.PoEMemory.Elements;
public class NpcLine
{
    public Element Element
    {
        [CompilerGenerated]
        get
        {
            return (Element)(object)this;
        }
    }

    public string Text
    {
        [CompilerGenerated]
        get
        {
            return (string)(object)this;
        }
    }

    public NpcLine(Element element)
    {
        while (element != null)
        {
        }

        throw "element";
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}