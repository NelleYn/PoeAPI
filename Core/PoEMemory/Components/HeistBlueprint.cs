using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects.Heist;
using ExileCore.Shared.Cache;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;
public class HeistBlueprint : Component
{
    public class Wing : RemoteMemoryObject
    {
        public List<(HeistJobRecord, int)> Jobs => (List<(HeistJobRecord, int)>)(object)this;
        public List<HeistChestRewardTypeRecord> RewardRooms => (List<HeistChestRewardTypeRecord>)(this + 32);
        public List<HeistNpcRecord> Crew => (List<HeistNpcRecord>)(this + 56);

        private List<(HeistJobRecord, int)> GetJobs(long source)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        private List<HeistChestRewardTypeRecord> GetRooms(long source)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        private List<HeistNpcRecord> GetCrew(long source)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private readonly CachedValue<HeistBlueprintComponentOffsets> _CachedBlueprint;
    public HeistBlueprintComponentOffsets BlueprintStruct => (HeistBlueprintComponentOffsets)this;
    public byte AreaLevel => (byte)(int)this;
    public bool IsConfirmed => (nint)this == 1;
    public List<Wing> Wings => (List<Wing>)(object)this;

    private List<Wing> GetWings(NativePtrArray source)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}