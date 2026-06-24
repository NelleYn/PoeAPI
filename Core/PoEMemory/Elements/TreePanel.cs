using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements;
public class TreePanel : Element
{
    private const int RefundButtonOffset = 1080;
    private const int CanvasElementOffset = 1208;
    public Element RefundButton => this;
    public Element CanvasElement => this;

    public List<TreePassiveElement> Passives
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}