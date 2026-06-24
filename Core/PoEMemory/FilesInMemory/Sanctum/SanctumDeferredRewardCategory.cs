using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.FilesInMemory.Sanctum;
public class SanctumDeferredRewardCategory : RemoteMemoryObject
{
    public BaseItemType BaseType => (BaseItemType)(object)this;

    public string CurrencyName
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 16;
            return (string)1;
        }
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}