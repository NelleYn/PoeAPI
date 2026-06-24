using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory.Ultimatum;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;
public class UltimatumChoicePanel : Element
{
    private readonly CachedValue<UltimatumPanelOffsets> _cache;
    public List<UltimatumModifier> Modifiers => (List<UltimatumModifier>)(object)this;

    public List<UltimatumChoiceElement> ChoiceElements
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int SelectedChoice => (int)this;
}