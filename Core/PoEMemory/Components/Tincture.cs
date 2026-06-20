using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Components;
public class Tincture : Component
{
    public TinctureDat TinctureDat
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 16;
            return (TinctureDat)(object)new int[1]
            {
                16
            };
        }
    }
}