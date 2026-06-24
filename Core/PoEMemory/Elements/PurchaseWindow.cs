using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements;
public class PurchaseWindow : Element
{
    private readonly CachedValue<PurchaseWindowOffsets> _cache;
    public Element CloseButton => (Element)2;
    public VendorStashTabContainer TabContainer => (VendorStashTabContainer)(object)this;
}