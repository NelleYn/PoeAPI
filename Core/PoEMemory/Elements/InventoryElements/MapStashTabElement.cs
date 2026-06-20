using System.Collections.Generic;
using System.Linq;

namespace ExileCore.PoEMemory.Elements.InventoryElements
{
    /// <summary>
    /// Describes the stored count and tier information for a single map type in the map stash tab.
    /// </summary>
    public class MapSubInventoryInfo
    {
        /// <summary>The number of stored maps of this type.</summary>
        public int Count;

        /// <summary>The display name of the map.</summary>
        public string MapName;

        /// <summary>The map tier.</summary>
        public int Tier;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Tier:{Tier} Count:{Count} MapName:{MapName}";
        }
    }

    /// <summary>
    /// Identifies a map sub-inventory by its metadata path and map type.
    /// </summary>
    public class MapSubInventoryKey
    {
        /// <summary>The metadata path of the map.</summary>
        public string Path;

        /// <summary>The map type.</summary>
        public MapType Type;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Path:{Path} Type:{Type}";
        }
    }

    /// <summary>
    /// Enumerates the kinds of maps tracked by the map stash tab.
    /// </summary>
    public enum MapType
    {
        /// <summary>A normal map.</summary>
        Normal,

        /// <summary>A shaped map.</summary>
        Shaped,

        /// <summary>Reserved/unknown map type.</summary>
        Unknown2,

        /// <summary>Reserved/unknown map type.</summary>
        Unknown3,

        /// <summary>A unique map.</summary>
        Unique
    }

    /// <summary>
    /// UI element for the map stash tab, exposing stored map counts both from memory and from the rendered UI tree.
    /// </summary>
    public class MapStashTabElement : Element
    {
        private long mapListStartPtr => Address != 0 ? M.Read<long>(Address + 0x9D8) : 0x00;
        private long mapListEndPtr => Address != 0 ? M.Read<long>(Address + 0x9D8 + 0x08) : 0x00;

        /// <summary>
        /// Gets the total number of map sub-inventories stored in the tab.
        /// </summary>
        public int TotalInventories => (int) ((mapListEndPtr - mapListStartPtr) / 0x10);

        /// <summary>
        /// Gets the stored maps keyed by path and type, read directly from memory.
        /// </summary>
        public Dictionary<MapSubInventoryKey, MapSubInventoryInfo> MapsCount => GetMapsCount();

        /// <summary>
        /// Gets the stored maps as a name-to-count map, ordered by tier.
        /// </summary>
        public Dictionary<string, string> MapsCountByName => GetMapsCount2();

        /// <summary>
        /// Gets the stored maps as a tier-to-count map, read from the rendered UI.
        /// </summary>
        public Dictionary<string, string> MapsCountByTier => GetMapsCountFromUi();

        /// <summary>
        /// Gets the contents of the currently selected cell, keyed by item name.
        /// </summary>
        public Dictionary<string, string> CurrentCell => GetCurrentCell();

        private Dictionary<MapSubInventoryKey, MapSubInventoryInfo> GetMapsCount()
        {
            var result = new Dictionary<MapSubInventoryKey, MapSubInventoryInfo>();
            MapSubInventoryInfo subInventoryInfo = null;
            MapSubInventoryKey subInventoryKey = null;

            for (var i = 0; i < TotalInventories; i++)
            {
                subInventoryInfo = new MapSubInventoryInfo();
                subInventoryKey = new MapSubInventoryKey();
                subInventoryInfo.Tier = SubInventoryMapTier(i);
                subInventoryInfo.Count = SubInventoryMapCount(i);
                subInventoryInfo.MapName = SubInventoryMapName(i);
                subInventoryKey.Path = SubInventoryMapPath(i);
                subInventoryKey.Type = SubInventoryMapType(i);
                result.Add(subInventoryKey, subInventoryInfo);
            }

            return result;
        }

        private Dictionary<string, string> GetMapsCount2()
        {
            var maps = GetMapsCount();
            var result = new Dictionary<string, string>();

            foreach (var mapSubInventoryInfo in maps.OrderBy(x => x.Value.Tier))
            {
                var shaped = mapSubInventoryInfo.Key.Type == MapType.Shaped ? "Shaped" : "";
                var name = $"{mapSubInventoryInfo.Value.Tier}: {shaped} {mapSubInventoryInfo.Value.MapName}";
                var info = $"{mapSubInventoryInfo.Value.Count}";
                result[name] = info;
            }

            return result;
        }

        private int SubInventoryMapTier(int index)
        {
            return M.Read<int>(mapListStartPtr + index * 0x10, 0x00);
        }

        private int SubInventoryMapCount(int index)
        {
            return M.Read<int>(mapListStartPtr + index * 0x10, 0x08);
        }

        private MapType SubInventoryMapType(int index)
        {
            return (MapType) M.Read<int>(mapListStartPtr + index * 0x10, 0x1C);
        }

        private string SubInventoryMapPath(int index)
        {
            return M.ReadStringU(M.Read<long>(mapListStartPtr + index * 0x10, 0x28, 0x00));
        }

        private string SubInventoryMapName(int index)
        {
            return M.ReadStringU(M.Read<long>(mapListStartPtr + index * 0x10, 0x28, 0x20));
        }

        private Dictionary<string, string> GetCurrentCell()
        {
            var cell = Children[2].Children[0].Children[0].Children;
            var result = new Dictionary<string, string>();

            foreach (var element in cell)
            {
                var name = element?.Tooltip?.Children?[0].Children[0].Children[3].Text;

                if (name == null)
                {
                    var tooltipText = element.Tooltip?.Text;
                    name = tooltipText != null ? tooltipText.Substring(0, tooltipText.IndexOf('\n')) : "Error";
                }

                var count = element.Children[4].Text;
                result.Add(name, count);
            }

            return result;
        }

        private Dictionary<string, string> GetMapsCountFromUi()
        {
            var Rows = Children[0].Children.Concat(Children[1].Children);
            var result = new Dictionary<string, string>();

            foreach (var element in Rows)
            {
                result.Add(element.Children[0].Text, element.Children[1].Text);
            }

            return result;
        }
    }
}
