namespace ExileCore.PoEMemory.Elements;
public class ExpeditionDetonator : Element
{
    public ExpeditionDetonatorInfo Info => (ExpeditionDetonatorInfo)(object)this;
    public int RemainingExplosives => (int)new int[3];
    public Element RevertExplosiveButton => (Element)(object)new int[3]
    {
        0,
        0,
        1
    };
    public Element ToggleExplosivePlacementButton => (Element)(object)new int[2]
    {
        0,
        1
    };
}