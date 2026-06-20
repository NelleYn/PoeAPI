using System.Collections.Generic;
using System.Linq.Expressions;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageInfo : StructuredRemoteMemoryObject<VillageInfoOffsets>
{
    private static readonly int InitialResourcesOffset;
    private static readonly int ShipInfoOffset;
    private static readonly int PortRequestOffset;
    private readonly FrameCache<List<VillageWorker>> _workersCache;
    public VillageResourceContainer ZoneLoadResources => (VillageResourceContainer)(this + (long)this);
    public List<VillageWorker> Workers => (List<VillageWorker>)(object)this;

    public List<VillagePortRequest> PortRequests
    {
        get
        {
            //IL_0003: Unknown result type (might be due to invalid IL or missing references)
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_000a: Unknown result type (might be due to invalid IL or missing references)
            _ = this + (long)this + (long)this + 120;
            _ = 24;
            return null;
        }
    }

    public List<VillageShipInfo> ShipInfo
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<VillageWorkerForSale> WorkersForSale => (List<VillageWorkerForSale>)(object)this;

    public Dictionary<GameStat, int> VillageStats
    {
        get
        {
            while (this != null)
            {
            }

            while (this != null)
            {
            }

            return (Dictionary<GameStat, int>)(object)this;
        }
    }

    static VillageInfo()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}