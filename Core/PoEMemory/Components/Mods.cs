using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.PoEMemory.Models;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing an item's modifiers, rarity, level requirements, and human-readable stats.
/// </summary>
public class Mods : Component
{
    private readonly CachedValue<ModsComponentOffsets> _cachedValue;

    /// <summary>Initializes a new instance of the <see cref="Mods"/> class.</summary>
    public Mods()
    {
        _cachedValue = new FrameCache<ModsComponentOffsets>(() => M.Read<ModsComponentOffsets>(Address));
    }

    /// <summary>Gets the raw mods offsets struct (cached per frame).</summary>
    public ModsComponentOffsets ModsStruct => _cachedValue.Value;

    /// <summary>Gets the unique item name, or an empty string when the item is not unique.</summary>
    public string UniqueName =>
        Address != 0
            ? Cache.StringCache.Read($"{nameof(Mods)}{ModsStruct.UniqueName + 0x8}",
                () => M.ReadStringU(M.Read<long>(ModsStruct.UniqueName + 0x8, 0x30)) +
                      M.ReadStringU(M.Read<long>(ModsStruct.UniqueName + 0x18, 0x30)))
            : string.Empty;

    /// <summary>Gets a value indicating whether the item is identified.</summary>
    public bool Identified => Address != 0 && ModsStruct.Identified;

    /// <summary>Gets the item rarity.</summary>
    public ItemRarity ItemRarity => Address != 0 ? (ItemRarity) ModsStruct.ItemRarity : ItemRarity.Normal;

    /// <summary>Gets a hash useful for caching the item by its mods.</summary>
    //Usefull for cache items
    public long Hash => ModsStruct.implicitMods.GetHashCode() ^ ModsStruct.explicitMods.GetHashCode() ^ ModsStruct.GetHashCode();

    /// <summary>Gets the combined list of implicit and explicit modifiers on the item.</summary>
    public List<ItemMod> ItemMods
    {
        get
        {
            var implicitMods = GetMods(ModsStruct.implicitMods.First, ModsStruct.implicitMods.Last);
            var explicitMods = GetMods(ModsStruct.explicitMods.First, ModsStruct.explicitMods.Last);
            return implicitMods.Concat(explicitMods).ToList();
        }
    }

    /// <summary>Gets the item level.</summary>
    public int ItemLevel => Address != 0 ? ModsStruct.ItemLevel /*M.Read<int>(Address + 0x42c) */ : 1;

    /// <summary>Gets the level required to use the item.</summary>
    public int RequiredLevel => Address != 0 ? ModsStruct.RequiredLevel /*M.Read<int>(Address + 0x430)*/ : 1;

    /// <summary>Gets a value indicating whether the item is usable.</summary>
    public bool IsUsable => Address != 0 && M.Read<byte>(Address + 0x370) == 1;

    /// <summary>Gets a value indicating whether the item is mirrored.</summary>
    public bool IsMirrored => Address != 0 && M.Read<byte>(Address + 0x371) == 1;

    /// <summary>Gets the number of fractured modifiers on the item.</summary>
    public int CountFractured => M.Read<byte>(Address + 0x89);

    /// <summary>Gets a value indicating whether the item is synthesised.</summary>
    public bool Synthesised => M.Read<byte>(Address + 0x437) == 1;

    /// <summary>Gets a value indicating whether the item has any fractured modifiers.</summary>
    public bool HaveFractured => Address != 0 && CountFractured > 0;

    /// <summary>Gets the parsed item stats for this item.</summary>
    public ItemStats ItemStats => new ItemStats(Owner);

    /// <summary>Gets the human-readable list of all item stats.</summary>
    public List<string> HumanStats => GetStats(ModsStruct.GetStats);

    /// <summary>Gets the human-readable list of crafted stats.</summary>
    public List<string> HumanCraftedStats => GetStats(ModsStruct.GetCraftedStats);

    /// <summary>Gets the human-readable list of implicit stats.</summary>
    public List<string> HumanImpStats => GetStats(ModsStruct.GetImplicitStats);

    /// <summary>Gets the human-readable list of fractured stats.</summary>
    public List<string> FracturedStats => GetStats(ModsStruct.GetFracturedStats);

    private List<string> GetStats(NativePtrArray array)
    {
        var readPointersArray = M.ReadPointersArray(array.First, array.Last, ModsComponentOffsets.HumanStats);
        var result = new List<string>();

        foreach (var pointer in readPointersArray)
        {
            result.Add(Cache.StringCache.Read($"{nameof(Mods)}{pointer}", () => M.ReadStringU(pointer)));
        }

        return result;
    }

    private List<ItemMod> GetMods(long startOffset, long endOffset)
    {
        var list = new List<ItemMod>();

        if (Address == 0)
            return list;

        var begin = startOffset;
        var end = endOffset;
        var count = (end - begin) / 0x28;

        if (count > 12)
            return list;

        for (var i = begin; i < end; i += 0x28)
        {
            list.Add(GetObject<ItemMod>(i));
        }

        return list;
    }
}
