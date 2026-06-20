using ExileCore.PoEMemory.MemoryObjects.Heist;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class HeistContract : Component
{
    private readonly CachedValue<HeistContractComponentOffsets> _ContractData;
    private readonly CachedValue<HeistContractObjectiveOffsets> _ObjectivesData;
    private readonly CachedValue<HeistContractRequirementOffsets> _RequirementData;
    private HeistContractObjectiveOffsets Objectives => (HeistContractObjectiveOffsets)this;
    private HeistContractRequirementOffsets Requirements => (HeistContractRequirementOffsets)this;
    public BaseItemType TargetItem => (BaseItemType)(object)this;
    public string Client => (string)1;
    public HeistJobRecord RequiredJob => (HeistJobRecord)(object)this;
    public byte RequiredJobLevel => (byte)(int)this;
}