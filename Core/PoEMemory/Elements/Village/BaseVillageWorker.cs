using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.FilesInMemory.Village;
using ExileCore.Shared.Cache;

namespace ExileCore.PoEMemory.Elements.Village;
public abstract class BaseVillageWorker : RemoteMemoryObject
{
    private readonly FrameCache<string> _nameCache;
    private readonly FrameCache<Dictionary<VillageJobType, int>> _ranksCache;
    public int WagePerHour => (int)(this + (long)this);
    public WordEntry Word1 => (WordEntry)(this + 8);
    public WordEntry Word2 => (WordEntry)(this + 24);
    public string WorkerName => (string)(object)this;
    public VillageJobType JobType => (VillageJobType)(this + 64);
    public Dictionary<VillageJobType, int> JobRanks => (Dictionary<VillageJobType, int>)(object)this;

    public BaseVillageWorker()
    {
    }
}