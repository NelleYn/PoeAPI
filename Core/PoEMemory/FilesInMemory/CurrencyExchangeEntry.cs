using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.FilesInMemory;
public class CurrencyExchangeEntry : RemoteMemoryObject
{
    public BaseItemType BaseItemType => (BaseItemType)(object)this;
    public CurrencyExchangeCategory Category1 => (CurrencyExchangeCategory)(this + 16);
    public CurrencyExchangeCategory Category2 => (CurrencyExchangeCategory)(this + 32);
    public byte Byte1 => (byte)(this + 48);
    public byte Byte2 => (byte)(this + 49);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}