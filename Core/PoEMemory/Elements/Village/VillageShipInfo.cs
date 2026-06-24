using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory.Village;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageShipInfo : RemoteMemoryObject
{
    public VillageShippingPort TargetPort => (VillageShippingPort)(this + 64);

    public Dictionary<VillageExport, int> Exports
    {
        get
        {
            while (this != null)
            {
            }

            while (this != null)
            {
            }

            return (Dictionary<VillageExport, int>)(object)this;
        }
    }

    private int[] RawExportList => (int[])(object)this;
}