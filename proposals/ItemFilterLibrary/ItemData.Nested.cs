// EXPERIMENTAL candidate ported from exApiTools/ItemFilter — see proposals/ItemFilterLibrary/README.md. Not part of the build.

using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace ExileCore.ItemFilterLibrary;

/// <summary>
/// Nested projections referenced by <see cref="ItemData"/>. Ported from
/// <c>exApiTools/ItemFilter/ItemData.Nested.cs</c>. Every field is populated only from members that
/// exist on this fork's Core (see README). Upstream IFL additionally exposes per-category mod lists
/// (<c>ExplicitMods</c>/<c>ImplicitMods</c>/<c>EnchantedMods</c>/…), map/charge/attribute projections
/// and influence data; those are omitted here because the backing members are absent from this fork.
/// </summary>
public partial class ItemData
{
    /// <summary>Mod-related projection. This fork's <c>Mods</c> exposes only the combined
    /// <see cref="ItemMods"/> list (implicit + explicit), so no per-affix-category lists are provided.</summary>
    public class ModsData
    {
        public ModsData(
            List<ItemMod> itemMods,
            ItemRarity itemRarity,
            bool identified,
            int itemLevel,
            int requiredLevel,
            string uniqueName,
            int countFractured,
            bool synthesised,
            bool isMirrored)
        {
            ItemMods = itemMods ?? new List<ItemMod>();
            ItemRarity = itemRarity;
            Identified = identified;
            ItemLevel = itemLevel;
            RequiredLevel = requiredLevel;
            UniqueName = uniqueName ?? string.Empty;
            CountFractured = countFractured;
            Synthesised = synthesised;
            IsMirrored = isMirrored;
        }

        /// <summary>All mods (implicit + explicit) — each <see cref="ItemMod"/> exposes <c>Name</c>/<c>DisplayName</c>.</summary>
        public List<ItemMod> ItemMods { get; }
        public ItemRarity ItemRarity { get; }
        public bool Identified { get; }
        public int ItemLevel { get; }
        public int RequiredLevel { get; }
        public string UniqueName { get; }
        public int CountFractured { get; }
        public bool Synthesised { get; }
        public bool IsMirrored { get; }
    }

    /// <summary>Socket / link projection.</summary>
    public class SocketData
    {
        public SocketData(
            int largestLinkSize,
            int numberOfSockets,
            List<int[]> links,
            List<string> socketGroups,
            List<string> socketedGems)
        {
            LargestLinkSize = largestLinkSize;
            NumberOfSockets = numberOfSockets;
            Links = links ?? new List<int[]>();
            SocketGroups = socketGroups ?? new List<string>();
            SocketedGems = socketedGems ?? new List<string>();
        }

        public int LargestLinkSize { get; }

        /// <summary>Number of sockets. Exposed under both names; <c>SocketNumber</c> matches upstream IFL rules.</summary>
        public int NumberOfSockets { get; }
        public int SocketNumber => NumberOfSockets;

        /// <summary>Each entry is a linked group as socket-colour codes (1..6) — from <c>Sockets.Links</c>.</summary>
        public List<int[]> Links { get; }

        /// <summary>Colour-letter groups per link ("RGB", "RRW", …) — from <c>Sockets.SocketGroup</c>.</summary>
        public List<string> SocketGroups { get; }

        /// <summary>Metadata paths of socketed gems — from <c>Sockets.SocketedGems[].GemEntity.Path</c>.</summary>
        public List<string> SocketedGems { get; }
    }

    /// <summary>Stack-size projection.</summary>
    public class StackData
    {
        public StackData(int size, int maxStackSize)
        {
            Size = size;
            MaxStackSize = maxStackSize;
        }

        /// <summary>Current stack size — from <c>Stack.Size</c>. Exposed as <see cref="Count"/> too (upstream IFL name).</summary>
        public int Size { get; }
        public int Count => Size;

        /// <summary>Maximum stack size — from <c>Stack.Info.MaxStackSize</c> (<c>CurrencyInfo.MaxStackSize</c>).</summary>
        public int MaxStackSize { get; }
    }
}
