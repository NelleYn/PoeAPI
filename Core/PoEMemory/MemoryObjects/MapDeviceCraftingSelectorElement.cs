using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;
public class MapDeviceCraftingSelectorElement : Element
{
    public bool OptionIsSelected => (int)(this + (long)this) > 0;
    public int SelectedOptionIndex => (int)(this + (long)this);

    public List<Element> Options
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}