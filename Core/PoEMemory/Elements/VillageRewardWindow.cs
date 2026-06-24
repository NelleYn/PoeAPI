namespace ExileCore.PoEMemory.Elements;
public class VillageRewardWindow : Element
{
    private const int TabContainerOffset = 736;
    public VendorStashTabContainer TabContainer => (VendorStashTabContainer)(this + (long)this);
}