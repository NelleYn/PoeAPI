namespace ExileCore.PoEMemory.Elements;
public class AzmeriData : RemoteMemoryObject
{
    public const int FuelDataPtrOffset = 344;
    public const int CurrentFuelOffset = 216;
    public const int MaxFuelOffset = 220;
    public uint CurrentFuel
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public uint MaxFuel
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public float RemainingFuelFraction => (float)(double)this / (float)(double)this;
}