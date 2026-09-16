using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class IngameData : RemoteMemoryObject
    {
        private readonly CachedValue<IngameDataOffsets> _cacheStruct;
        private readonly CachedValue<AreaTemplate> _CurrentArea;
        private readonly CachedValue<WorldArea> _CurrentWorldArea;
        private readonly CachedValue<long> _EntitiesCount;
        private EntityList _EntityList;
        private readonly CachedValue<Entity> _localPlayer;
        private NativePtrArray cacheStats;
        private readonly Dictionary<GameStat, int> mapStats = new Dictionary<GameStat, int>();

        public IngameData()
        {
            _cacheStruct = new AreaCache<IngameDataOffsets>(() => M.Read<IngameDataOffsets>(Address));
            _localPlayer = new AreaCache<Entity>(() => GetObject<Entity>(_cacheStruct.Value.LocalPlayer));
            _CurrentArea = new AreaCache<AreaTemplate>(() => GetObject<AreaTemplate>(_cacheStruct.Value.CurrentArea));
            _CurrentWorldArea = new AreaCache<WorldArea>(() => TheGame.Files.WorldAreas.GetByAddress(CurrentArea.Address));
            var offset = Extensions.GetOffset<IngameDataOffsets>(nameof(IngameDataOffsets.EntitiesCount));
            _EntitiesCount = new FrameCache<long>(() => M.Read<long>(Address + offset));
        }

        public IngameDataOffsets DataStruct => _cacheStruct.Value;
        public long EntitiesCount => _EntitiesCount.Value;
        public AreaTemplate CurrentArea => _CurrentArea.Value;
        public WorldArea CurrentWorldArea => _CurrentWorldArea.Value;
        public int CurrentAreaLevel => _cacheStruct.Value.CurrentAreaLevel;
        public uint CurrentAreaHash => _cacheStruct.Value.CurrentAreaHash;
        public Entity LocalPlayer => _localPlayer.Value;
        public TerrainData Terrain => _cacheStruct.Value.Terrain;
        public long EntiteisTest => DataStruct.EntityList;
        public EntityList EntityList => _EntityList ?? (_EntityList = GetObject<EntityList>(DataStruct.EntityList));
        private long LabDataPtr => _cacheStruct.Value.LabDataPtr;

        /// <summary>
        /// Labyrinth layout, or <c>null</c> when there is none to read.
        /// </summary>
        /// <remarks>
        /// The offset is measured (see IngameDataOffsets.LabDataPtr) and the field holds an honest
        /// zero outside the Labyrinth, so the shape test below is no longer load-bearing — it is
        /// kept because it costs nothing and because the previous version of this property is
        /// exactly the failure this repository keeps meeting: the old offset held
        /// 0x92CFB39000000021, "not zero" accepted it, and the result was a LabyrinthData whose
        /// every read came back silently zero. That looks like data, which makes it worse than an
        /// exception.
        /// </remarks>
        public LabyrinthData LabyrinthData =>
            IsCanonicalPointer(LabDataPtr) ? GetObject<LabyrinthData>(LabDataPtr) : null;

        /// <summary>
        /// Whether a value has the shape of a user-mode heap pointer on x64: inside the user half of
        /// the address space and 8-aligned. Every offset measured on this client points at an
        /// 8-aligned address, so this rejects the usual garbage without pretending to validate it.
        /// </summary>
        private static bool IsCanonicalPointer(long value) =>
            value >= 0x10000L && value <= 0x7FFFFFFFFFFFL && (value & 7) == 0;

        public Dictionary<GameStat, int> MapStats
        {
            get
            {
                if (cacheStats.Equals(_cacheStruct.Value.MapStats)) return mapStats;
                mapStats.Clear();
                var statPtrStart = _cacheStruct.Value.MapStats.First;
                var statPtrEnd = _cacheStruct.Value.MapStats.Last;
                var key = 0;
                var value = 0;
                // A stat array is whole 8-byte (key, value) pairs and nothing else. The test runs on
                // the 64-bit span BEFORE it is narrowed, which is the whole point of doing it here:
                // a span of 0x1_0000_0010 truncates to 16 and would sail through every check that
                // looks only at the int. Anything ragged, negative or absurdly long means stale
                // pointers or a wrong offset, and the only safe answer is to read nothing — ReadMem
                // takes the length on trust, and this project has already paid for one unbounded read.
                var span = statPtrEnd - statPtrStart;

                if (span < 0 || span % 8 != 0 || span / 8 > 200)
                    return null;

                var total_stats = (int) span;

                var bytes = M.ReadMem(statPtrStart, total_stats);

                for (var i = 0; i < bytes.Length; i += 8)
                {
                    key = BitConverter.ToInt32(bytes, i);
                    value = BitConverter.ToInt32(bytes, i + 0x04);
                    mapStats[(GameStat) key] = value;
                }

                cacheStats = _cacheStruct.Value.MapStats;
                return mapStats;
            }
        }

        /// <summary>
        /// Open town portals in this area.
        /// </summary>
        /// <remarks>
        /// NOT MEASURED. 0x4B4 and 0x4BC are an older build's numbers, they bypass
        /// IngameDataOffsets entirely, and they are not even 8-aligned while every offset measured
        /// on this client is. On the area measured they read as two zeroes, so the result is an
        /// empty list — which reads as "no portals here" rather than "this offset is wrong", and
        /// that is exactly the failure this repository keeps paying for. The guard below at least
        /// stops a garbage pair from being handed to ReadStructsArray as a length.
        /// To close it: open a portal, ask tools/RefLive for the list the reference sees, and
        /// locate it the usual way.
        /// </remarks>
        public IList<PortalObject> TownPortals
        {
            get
            {
                var statPtrStart = M.Read<long>(Address + 0x4B4);
                var statPtrEnd = M.Read<long>(Address + 0x4BC);

                if (!IsCanonicalPointer(statPtrStart) || !IsCanonicalPointer(statPtrEnd) ||
                    statPtrEnd < statPtrStart)
                    return new List<PortalObject>();

                return M.ReadStructsArray<PortalObject>(statPtrStart, statPtrEnd, PortalObject.StructSize, TheGame);
            }
        }

        public class PortalObject : RemoteMemoryObject
        {
            public const int StructSize = 0x38;
            public string PlayerOwner => NativeStringReader.ReadString(Address + 0x08, M);
            public WorldArea Area => TheGame.Files.WorldAreas.GetAreaByAreaId(M.Read<int>(Address + 0x50));

            public override string ToString()
            {
                return $"{PlayerOwner} => {Area.Name}";
            }
        }
    }
}
