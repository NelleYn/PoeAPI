using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;
public class TooltipItemFrameElement : Element
{
    private readonly CachedValue<TooltipItemFrameElementOffsets> _cachedValue;
    public string CopyText => (string)1;
    public bool IsAdvancedTooltipText => (byte)(int)this != 0;
}