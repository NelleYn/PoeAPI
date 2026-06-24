using System.Runtime.InteropServices;

namespace ExileCore.PoEMemory.Elements.Village;
public class CurrencyExchangeStock : StructuredRemoteMemoryObject<CurrencyExchangeStock.CurrencyExchangeStockOffsets>
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct CurrencyExchangeStockOffsets
    {
        [FieldOffset(0)]
        public ushort Get;
        [FieldOffset(2)]
        public ushort Give;
        [FieldOffset(8)]
        public int ListedCount;
    }

    public int Get => (int)this;
    public int Give => (int)this;
    public int ListedCount => (int)this;

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}