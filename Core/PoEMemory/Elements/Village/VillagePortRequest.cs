using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillagePortRequest : RemoteMemoryObject
{
    public List<VillageShipmentRequest> Requests
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_000f: Expected O, but got I4
            _ = this + 24;
            while (null != null)
            {
            }

            return (List<VillageShipmentRequest>)8;
        }
    }
}