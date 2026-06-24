namespace ExileCore.PoEMemory.Components;
public class SentinelDrone : Component
{
    public int TimesUsed => this + 32;

    public int MaxUses
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0017: Expected I4, but got O
            _ = this + 16;
            return (int)new int[2]
            {
                16,
                32
            };
        }
    }
}