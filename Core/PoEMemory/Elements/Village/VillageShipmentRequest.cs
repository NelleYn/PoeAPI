using ExileCore.PoEMemory.FilesInMemory.Village;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageShipmentRequest : RemoteMemoryObject
{
    public VillageExport ResourceType => (VillageExport)(object)this;
    public int RequestedAmount => (object)this & (object)this;
    public int DeliveredAmount => this & ((object)this >> 28);
    public byte ResourceTypeIndex => (byte)((this + 7) & 0xF);
    public byte RequestMarkup => (byte)((this + 7 >> 4) & 0xF);
}