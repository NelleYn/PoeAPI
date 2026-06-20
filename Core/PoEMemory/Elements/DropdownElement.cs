using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements;
public class DropdownElement : Element
{
    private const long RememberedSelectionOffset = 1200L;
    private const long PendingSelectionOffset = 1216L;
    private const long IsOpenedOffset = 1232L;
    private const long OptionsOffset = 1592L;
    public int RememberedSelection => (int)(this + (long)this);
    public int PendingSelection => (int)(this + (long)this);
    public bool IsOpened => (int)(this + (long)this) > 0;

    public List<DropdownElementOption> Options
    {
        get
        {
            //IL_0004: Unknown result type (might be due to invalid IL or missing references)
            _ = this + (long)this;
            return (List<DropdownElementOption>)(object)this;
        }
    }
}