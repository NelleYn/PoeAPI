using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ExileCore.PoEMemory.FilesInMemory.Village;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageScreen : Element
{
    private readonly CachedValue<VillageInfo> _infoCache;
    private readonly CachedValue<int> _wageCache;
    private const int VillageInfoOffset = 720;
    public VillageResourceContainer ZoneLoadResources => (VillageResourceContainer)(object)this;
    public List<VillageWorker> Workers => (List<VillageWorker>)(object)this;
    public List<VillagePortRequest> PortRequests => (List<VillagePortRequest>)(object)this;
    public List<VillageShipInfo> ShipInfo => (List<VillageShipInfo>)(object)this;
    public List<VillageWorkerForSale> WorkersForSale => (List<VillageWorkerForSale>)(object)this;

    public Dictionary<VillageJob, List<VillageWorker>> WorkersByJob
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Dictionary<VillageJob, int> TotalSkillByJobType
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public Dictionary<GameStat, int> VillageStats => (Dictionary<GameStat, int>)(object)this;
    public List<TimeSpan> RemainingShipmentTimes => (List<TimeSpan>)(object)this;
    public TimeSpan TimeSinceSnapshot => (TimeSpan)this;

    public TimeSpan RemainingDisenchantmentTime
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private VillageJob DisenchantmentJob => (VillageJob)(object)"Disenchanting";
    private VillageJob IdleJob => (VillageJob)(object)"Idling";
    public int TotalWagePerHour => (int)this;

    public int CurrentGold
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public bool CanBeEmployed(VillageJob job)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}