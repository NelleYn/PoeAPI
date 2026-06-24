using System;
using ExileCore.PoEMemory.MemoryObjects.Heist;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;
public class HeistEquipment : Component
{
    private readonly Lazy<HeistEquipmentOffsets> _HeistEquipmentItem;
    private readonly CachedValue<BaseItemType> _ItemBase;
    private readonly CachedValue<HeistJobRecord> _Job;
    public BaseItemType ItemBase => (BaseItemType)(object)this;
    public HeistJobRecord RequiredJob => (HeistJobRecord)(object)this;
    public int JobMinimumLevel => (int)this;

    public HeistJobE RequiredJobE
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public HeistEquipment()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}