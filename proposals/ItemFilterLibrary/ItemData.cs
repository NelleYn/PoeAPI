// EXPERIMENTAL candidate ported from exApiTools/ItemFilter — see proposals/ItemFilterLibrary/README.md. Not part of the build.

using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.ItemFilterLibrary;

/// <summary>
/// A flat, query-friendly snapshot of a single in-memory item, projected from an <see cref="Entity"/>.
/// This is the object a compiled <see cref="ItemQuery"/> rule runs against: every public member here is
/// nameable inside a <c>.ifl</c> rule expression (e.g. <c>Rarity == ItemRarity.Unique</c>,
/// <c>SocketInfo.LargestLinkSize &gt;= 6</c>, <c>ModsInfo.ItemMods.Any(Name == "...")</c>).
///
/// Ported from <c>exApiTools/ItemFilter/ItemData.cs</c> and rewritten against this fork's Core API.
/// See the README for the members that differ from upstream (this fork has no
/// <c>Entity.TryGetComponent</c>, no per-category mod lists, and no influence flags).
/// </summary>
public partial class ItemData
{
    /// <summary>Builds a snapshot from an item <see cref="Entity"/> (inventory / equipped / stash item).</summary>
    public ItemData(Entity queriedItem, GameController gc)
    {
        Entity = queriedItem;
        ParseItem(queriedItem, gc);
    }

    /// <summary>Builds a snapshot from a ground item via its <see cref="LabelOnGround"/>.</summary>
    public ItemData(LabelOnGround queriedItem, GameController gc)
        : this(queriedItem?.ItemOnGround, gc)
    {
    }

    /// <summary>Builds a snapshot from a UI inventory slot (<see cref="NormalInventoryItem.Item"/>).</summary>
    public ItemData(NormalInventoryItem queriedItem, GameController gc)
        : this(queriedItem?.Item, gc)
    {
    }

    /// <summary>The underlying item entity. <see cref="ItemFilter"/> guards on <c>Entity?.IsValid</c>.</summary>
    public Entity Entity { get; }

    // ---- from Entity ----
    public string Path { get; private set; } = string.Empty;

    // ---- from BaseItemType (Files.BaseItemTypes.Translate) ----
    public string ClassName { get; private set; } = string.Empty;
    public string BaseName { get; private set; } = string.Empty;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public List<string> Tags { get; private set; } = new List<string>();

    // ---- from Base ----
    public string Name { get; private set; } = string.Empty;
    public bool IsCorrupted { get; private set; }
    public bool IsElder { get; private set; }
    public bool IsShaper { get; private set; }

    // ---- from Mods ----
    public ItemRarity Rarity { get; private set; } = ItemRarity.Normal;
    public bool IsIdentified { get; private set; }
    public int ItemLevel { get; private set; }
    public int RequiredLevel { get; private set; }
    public string UniqueName { get; private set; } = string.Empty;
    public int CountFractured { get; private set; }
    public bool IsFractured { get; private set; }
    public bool IsSynthesised { get; private set; }
    public bool IsMirrored { get; private set; }

    // ---- from Quality ----
    public int ItemQuality { get; private set; }

    // ---- nested projections ----
    public ModsData ModsInfo { get; private set; } =
        new ModsData(new List<ItemMod>(), ItemRarity.Normal, false, 0, 0, string.Empty, 0, false, false);

    public SocketData SocketInfo { get; private set; } =
        new SocketData(0, 0, new List<int[]>(), new List<string>(), new List<string>());

    public StackData StackInfo { get; private set; } = new StackData(0, 0);

    private void ParseItem(Entity item, GameController gc)
    {
        if (item == null)
            return;

        Path = item.Path ?? string.Empty;

        var baseItemType = gc?.Files?.BaseItemTypes?.Translate(item.Path);
        if (baseItemType != null)
        {
            ClassName = baseItemType.ClassName ?? string.Empty;
            BaseName = baseItemType.BaseName ?? string.Empty;
            Width = baseItemType.Width;
            Height = baseItemType.Height;
            Tags = baseItemType.Tags != null ? baseItemType.Tags.ToList() : new List<string>();
        }

        var baseComponent = item.GetComponent<Base>();
        if (baseComponent != null)
        {
            Name = baseComponent.Name ?? string.Empty;
            IsCorrupted = baseComponent.isCorrupted;
            IsElder = baseComponent.isElder;
            IsShaper = baseComponent.isShaper;
        }

        var mods = item.GetComponent<Mods>();
        if (mods != null)
        {
            Rarity = mods.ItemRarity;
            IsIdentified = mods.Identified;
            ItemLevel = mods.ItemLevel;
            RequiredLevel = mods.RequiredLevel;
            UniqueName = mods.UniqueName ?? string.Empty;
            CountFractured = mods.CountFractured;
            IsFractured = mods.CountFractured > 0;
            IsSynthesised = mods.Synthesised;
            IsMirrored = mods.IsMirrored;

            ModsInfo = new ModsData(
                mods.ItemMods ?? new List<ItemMod>(),
                mods.ItemRarity,
                mods.Identified,
                mods.ItemLevel,
                mods.RequiredLevel,
                UniqueName,
                mods.CountFractured,
                mods.Synthesised,
                mods.IsMirrored);
        }

        var sockets = item.GetComponent<Sockets>();
        if (sockets != null)
        {
            var socketedGemPaths = sockets.SocketedGems
                .Where(g => g?.GemEntity != null)
                .Select(g => g.GemEntity.Path)
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            SocketInfo = new SocketData(
                sockets.LargestLinkSize,
                sockets.NumberOfSockets,
                sockets.Links ?? new List<int[]>(),
                sockets.SocketGroup ?? new List<string>(),
                socketedGemPaths);
        }

        var stack = item.GetComponent<Stack>();
        if (stack != null)
        {
            var maxStackSize = stack.Info?.MaxStackSize ?? 0;
            StackInfo = new StackData(stack.Size, maxStackSize);
        }

        var quality = item.GetComponent<Quality>();
        if (quality != null)
            ItemQuality = quality.ItemQuality;
    }
}
