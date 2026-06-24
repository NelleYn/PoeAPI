using System.Runtime.InteropServices;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory.Elements.Village;
public class CurrencyExchangePanelOrderElement : Element
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Offsets
    {
        [FieldOffset(904)]
        public int OrderId;
    }

    private CachedValue<Offsets> _cache;
}