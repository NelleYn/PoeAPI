using System;
using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Cache;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;
public class ItemInfoData : RemoteMemoryObject
{
    private readonly CachedValue<ItemInfoOffsets> _cachedValue;
    public ItemInfoOffsets ItemInfoDataStruct => (ItemInfoOffsets)this;
    public byte ItemCellsSizeX => (byte)(int)this;
    public byte ItemCellsSizeY => (byte)(int)this;
    public string Name => (string)(object)this;
    public string FlavourText => (string)(object)this;

    [Obsolete]
    public long BaseItemType => (long)this;
    public BaseItemType BaseItemTypeDat => (BaseItemType)(object)this;

    [Obsolete]
    public unsafe NativePtrArray Tags
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<TagsDat.TagRecord> TagsDat
    {
        get
        {
            _ = 16;
            return (List<TagsDat.TagRecord>)(object)this;
        }
    }
}