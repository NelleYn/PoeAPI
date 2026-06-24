using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageShipmentScreen : Element
{
    [StructLayout(LayoutKind.Explicit)]
    private struct PortStorageStruct
    {
        [FieldOffset(0)]
        public int PortIndex;
        [FieldOffset(8)]
        public long PortElementPtr;
    }

    public int OpenedShipIndex => (int)(this + (long)this);
    public bool ShipIsOpened => (int)(this + (long)this) > 0;
    public Element ShipmentConfigurationElement => (Element)(object)new int[2]
    {
        3,
        1
    };

    public Dictionary<int, Element> PortElementsByIndex
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}