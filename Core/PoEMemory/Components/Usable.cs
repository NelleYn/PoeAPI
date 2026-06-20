namespace ExileCore.PoEMemory.Components;
public class Usable : Component
{
    public string UseActionString
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 16;
            (new int[1])[0] = 24;
            return (string)(object)this;
        }
    }

    public int UseAction
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0012: Expected I4, but got O
            _ = this + 16;
            return (int)new int[1]
            {
                16
            };
        }
    }
}