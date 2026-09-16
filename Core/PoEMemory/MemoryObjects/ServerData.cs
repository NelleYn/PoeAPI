using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.FilesInMemory.Atlas;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class ServerData : RemoteMemoryObject
    {
        /// <summary>
        /// Turns a field name of <see cref="ServerDataOffsets"/> into an offset FROM
        /// <see cref="RemoteMemoryObject.Address"/>.
        /// </summary>
        /// <remarks>
        /// <c>Extensions.GetOffset</c> returns the offset INSIDE the blitted window (see
        /// <see cref="ServerDataOffsets.StructBase"/>), so the window base has to be added back
        /// before the number can be used as "Address + offset". Forgetting that is not a
        /// hypothetical: the stash-tab readers below did exactly that and were addressing 0x5000
        /// below the field they name. Every conversion now goes through here so there is one place
        /// to get it right.
        /// </remarks>
        private static int FromAddress(string fieldName) =>
            Extensions.GetOffset<ServerDataOffsets>(fieldName) + ServerDataOffsets.StructBase;

        private static readonly int NetworkStateOff = FromAddress(nameof(ServerDataOffsets.NetworkState));

        private readonly CachedValue<ServerDataOffsets> _cachedValue;
        private readonly CachedValue<int> _latency;
        private readonly List<Player> result = new List<Player>();

        public ServerData()
        {
            _cachedValue = new FrameCache<ServerDataOffsets>(() => M.Read<ServerDataOffsets>(Address + ServerDataOffsets.StructBase));

            // Latency lies far outside the blitted window and is therefore its own 4-byte read
            // rather than a struct field - see ServerDataOffsets.LATENCY both for the provenance of
            // 0xC490 and for why widening the struct to reach it would have been the wrong trade.
            // Cached per frame like the struct, so a plugin polling this in a loop does not turn
            // into one ReadProcessMemory call per poll.
            _latency = new FrameCache<int>(() => Address == 0 ? 0 : M.Read<int>(Address + ServerDataOffsets.LATENCY));
        }

        public ServerDataOffsets ServerDataStruct => _cachedValue.Value;
        public ushort TradeChatChannel => ServerDataStruct.TradeChatChannel;
        public ushort GlobalChatChannel => ServerDataStruct.GlobalChatChannel;
        public byte MonsterLevel => ServerDataStruct.MonsterLevel;

        //if 51 - more than 50 monsters remaining (no exact number)
        //if 255 - not supported for current map (town or scenary map)
        public CharacterClass PlayerClass => (CharacterClass) (ServerDataStruct.PlayerClass & 0xF);
        public byte MonstersRemaining => ServerDataStruct.MonstersRemaining;
        public ushort CurrentSulphiteAmount => _cachedValue.Value.CurrentSulphiteAmount;
        public int CurrentAzuriteAmount => _cachedValue.Value.CurrentAzuriteAmount;

        public IList<Player> NearestPlayers
        {
            get
            {
                if (Address == 0) return null;
                var startPtr = ServerDataStruct.NearestPlayers.First;
                var endPtr = ServerDataStruct.NearestPlayers.Last;
                startPtr += 16; //Don't ask me why. Just skipping first 2

                //Sometimes wrong offsets and read 10000000+ objects
                if (startPtr < Address || (endPtr - startPtr) / 16 > 50)
                    return result;

                result.Clear();

                for (var addr = startPtr; addr < endPtr; addr += 16) //16 because we are reading each second pointer (pointer vectors)
                {
                    result.Add(ReadObject<Player>(addr));
                }

                return result;
            }
        }

        public int GetBeastCapturedAmount(BestiaryCapturableMonster monster)
        {
            return M.Read<int>(Address + 0x5240 + monster.Id * 4);
        }

        #region PlayerData

        public ushort LastActionId => ServerDataStruct.LastActionId;
        public int CharacterLevel => ServerDataStruct.CharacterLevel;
        public int PassiveRefundPointsLeft => ServerDataStruct.PassiveRefundPointsLeft;
        public int FreePassiveSkillPointsLeft => ServerDataStruct.FreePassiveSkillPointsLeft;
        public int QuestPassiveSkillPoints => ServerDataStruct.QuestPassiveSkillPoints;
        public int TotalAscendencyPoints => ServerDataStruct.TotalAscendencyPoints;
        public int SpentAscendencyPoints => ServerDataStruct.SpentAscendencyPoints;
        public PartyAllocation PartyAllocationType => (PartyAllocation) ServerDataStruct.PartyAllocationType;
        public string League => ServerDataStruct.League.ToString(M);
        public PartyStatus PartyStatusType => (PartyStatus) this.ServerDataStruct.PartyStatusType;
        public bool IsInGame => NetworkState == NetworkStateE.Connected;
        public NetworkStateE NetworkState => (NetworkStateE) this.ServerDataStruct.NetworkState;
        /// <summary>
        /// Round-trip latency to the game server in milliseconds, exactly as read - NOT clamped,
        /// NOT defaulted. Callers that pace themselves by it must decide for themselves what to do
        /// with an implausible number; <see cref="IngameState.CurLatency"/> is the one that does.
        /// Reads 0 while <see cref="RemoteMemoryObject.Address"/> is 0, i.e. out of game.
        /// </summary>
        /// <remarks>
        /// The offset moved from 0x6CA0 to <see cref="ServerDataOffsets.LATENCY"/> (0xC490) on
        /// 2026-09-16. The old one read 0x128CBA26 on the live client, which is not a latency under
        /// any interpretation; the new one was measured. The full provenance, including the honest
        /// weakness of that measurement, is on ServerDataOffsets.LATENCY.
        /// </remarks>
        public int Latency => _latency.Value;
        public string Guild => NativeStringReader.ReadString(M.Read<long>(Address + 0x70E0), M);
        public BetrayalData BetrayalData => GetObject<BetrayalData>(M.Read<long>(Address + 0x3C8, 0x718));

        public IList<ushort> SkillBarIds
        {
            get
            {
                if (Address == 0) return new List<ushort>();
                
                var readAddr = _cachedValue.Value.SkillBarIds;

                var res = new List<ushort>
                {
                    readAddr.SkillBar1,
                    readAddr.SkillBar2,
                    readAddr.SkillBar3,
                    readAddr.SkillBar4,
                    readAddr.SkillBar5,
                    readAddr.SkillBar6,
                    readAddr.SkillBar7,
                    readAddr.SkillBar8,
                    readAddr.SkillBar9,
                    readAddr.SkillBar10,
                    readAddr.SkillBar11,
                    readAddr.SkillBar12,
                    readAddr.SkillBar13
                };

                return res;
            }
        }

        public IList<ushort> PassiveSkillIds
        {
            get
            {
                if (Address == 0) 
                    return null;
                var fisrPtr = ServerDataStruct.PassiveSkillIds.First;
                var endPtr = ServerDataStruct.PassiveSkillIds.Last;
                var totalStats = (int) (endPtr - fisrPtr);
                var bytes = M.ReadMem(fisrPtr, totalStats);
                var res = new List<ushort>();

                if (totalStats < 0 || totalStats > 500)
                    return new List<ushort>();

                for (var i = 0; i < bytes.Length; i += 2)
                {
                    var id = BitConverter.ToUInt16(bytes, i);
                    res.Add(id);
                }

                return res;
            }
        }

        #endregion

        #region Stash Tabs

        // public IList<ServerStashTab> PlayerStashTabs => GetStashTabs(Extensions.GetOffset<ServerDataOffsets>("PlayerStashTabsStart"), Extensions.GetOffset<ServerDataOffsets>("PlayerStashTabsEnd"));
        // These two take an offset FROM Address, so they go through FromAddress. Before that they
        // passed the raw window-relative number and were reading Address + 0x1CB0 / Address + 0x23E0
        // for fields the table declares at Address + 0x6CB0 / Address + 0x73E0 - 0x5000 short, in
        // memory that has nothing to do with stash tabs.
        //
        // What this fixes and what it does NOT: the code now reads the address it names. The named
        // offsets themselves are upstream legacy and were NOT measured by the 2026-09-16 survey, so
        // a stash list that comes back looking sensible is still not evidence that 0x6CB0 is right.
        public IList<ServerStashTab> PlayerStashTabs =>
            GetStashTabs(FromAddress(nameof(ServerDataOffsets.PlayerStashTabs)),
                FromAddress(nameof(ServerDataOffsets.PlayerStashTabs)) + 0x8);
        public IList<ServerStashTab> GuildStashTabs =>
            GetStashTabs(FromAddress(nameof(ServerDataOffsets.GuildStashTabs)),
                FromAddress(nameof(ServerDataOffsets.GuildStashTabs)) + 0x8);

        private IList<ServerStashTab> GetStashTabs(int offsetBegin, int offsetEnd)
        {
            var firstAddr = M.Read<long>(Address + offsetBegin);
            var lastAddr = M.Read<long>(Address + offsetEnd);

            if (firstAddr <= 0 || lastAddr <= 0) return null;
            
            var len = lastAddr - firstAddr;
            if (len <= 0 || len > 2048 || firstAddr <= 0 || lastAddr <= 0)
                return new List<ServerStashTab>();

            var tabs = M.ReadStructsArray<ServerStashTab>(firstAddr, lastAddr, ServerStashTab.StructSize, TheGame);

            //Skipping hidden tabs of premium maps tab (read notes in StashTabController.cs)
            //tabs.RemoveAll(x => (x.Flags & InventoryTabFlags.Hidden) == InventoryTabFlags.Hidden);
            return new List<ServerStashTab>(tabs);

            // return new List<IServerStashTab>(tabs.Where(x=> (x.Flags & InventoryTabFlags.Hidden) != 0));
        }

        #endregion

        #region Inventories

        public IList<InventoryHolder> PlayerInventories
        {
            get
            {
                var firstAddr = ServerDataStruct.PlayerInventories.First;
                var lastAddr = ServerDataStruct.PlayerInventories.Last;
                if (firstAddr == 0) return null;

                //Sometimes wrong offsets and read 10000000+ objects
                if (firstAddr == 0 || (lastAddr - firstAddr) / InventoryHolder.StructSize > 1024)
                    return new List<InventoryHolder>();

                return M.ReadStructsArray<InventoryHolder>(firstAddr, lastAddr, InventoryHolder.StructSize, this).ToList();
            }
        }

        public IList<InventoryHolder> NPCInventories
        {
            get
            {
                var firstAddr = ServerDataStruct.NPCInventories.First;
                var lastAddr = ServerDataStruct.NPCInventories.Last;
                if (firstAddr == 0) return null;
        

                //Sometimes wrong offsets and read 10000000+ objects
                if (firstAddr == 0 || (lastAddr - firstAddr) / InventoryHolder.StructSize > 1024)
                    return new List<InventoryHolder>();

                return M.ReadStructsArray<InventoryHolder>(firstAddr, lastAddr, InventoryHolder.StructSize, TheGame).ToList();
            }
        }

        public IList<InventoryHolder> GuildInventories
        {
            get
            {
                var firstAddr = ServerDataStruct.GuildInventories.First;
                var lastAddr = ServerDataStruct.GuildInventories.Last;
                if (firstAddr == 0) return null;
           

                //Sometimes wrong offsets and read 10000000+ objects
                if (firstAddr == 0 || (lastAddr - firstAddr) / InventoryHolder.StructSize > 1024)
                    return new List<InventoryHolder>();

                return M.ReadStructsArray<InventoryHolder>(firstAddr, lastAddr, InventoryHolder.StructSize, TheGame).ToList();
            }
        }

        #region Utils functions

        public ServerInventory GetPlayerInventoryBySlot(InventorySlotE slot)
        {
            foreach (var inventory in PlayerInventories)
            {
                if (inventory.Inventory.InventSlot == slot)
                    return inventory.Inventory;
            }

            return null;
        }

        public ServerInventory GetPlayerInventoryByType(InventoryTypeE type)
        {
            foreach (var inventory in PlayerInventories)
            {
                if (inventory.Inventory.InventType == type)
                    return inventory.Inventory;
            }

            return null;
        }

        public ServerInventory GetPlayerInventoryBySlotAndType(InventoryTypeE type, InventorySlotE slot)
        {
            foreach (var inventory in PlayerInventories)
            {
                if (inventory.Inventory.InventType == type && inventory.Inventory.InventSlot == slot)
                    return inventory.Inventory;
            }

            return null;
        }

        #endregion

        #endregion

        #region Completed Areas

        public IList<WorldArea> CompletedAreas => GetAreas(ServerDataStruct.CompletedMaps);
        public IList<WorldArea> ShapedMaps => new List<WorldArea>();// GetAreas(ServerDataStruct.ShapedAreas);
        public IList<WorldArea> BonusCompletedAreas => GetAreas(ServerDataStruct.BonusCompletedAreas);
        public IList<WorldArea> ElderGuardiansAreas => new List<WorldArea>();// GetAreas(ServerDataStruct.ElderGuardiansAreas);
        public IList<WorldArea> MasterAreas => new List<WorldArea>();// GetAreas(ServerDataStruct.MasterAreas);
        public IList<WorldArea> ShaperElderAreas => new List<WorldArea>();// GetAreas(ServerDataStruct.ElderInfluencedAreas);
        private IList<WorldArea> GetAreas(long address)
        {
            if (Address == 0 || address == 0)
                return new List<WorldArea>();

            var res = new List<WorldArea>();
            var size = M.Read<int>(Address - 0x8);
            var listStart = M.Read<long>(address);
            var error = 0;

            if (listStart == 0 || size == 0)
                return res;

            for (var addr = M.Read<long>(listStart); addr != listStart; addr = M.Read<long>(addr))
            {
                if (addr == 0) return res;
                var byAddress = TheGame.Files.WorldAreas.GetByAddress(M.Read<long>(addr + 0x18));

                if (byAddress != null)
                    res.Add(byAddress);

                if (--size < 0) break;
                error++;

                //Sometimes wrong offsets and read 10000000+ objects
                if (error > 1024)
                {
                    res = new List<WorldArea>();
                    break;
                }
            }

            return res;
        }

        #endregion

        #region Atlas

        public byte GetAtlasRegionUpgradesByRegion(int regionId)
        {
            return M.Read<byte>(Address + ServerDataOffsets.ATLAS_REGION_UPGRADES + regionId);
        }

        public byte GetAtlasRegionUpgradesByRegion(AtlasRegion region)
        {
            return M.Read<byte>(Address + ServerDataOffsets.ATLAS_REGION_UPGRADES + region.Index);
        }

        #endregion
    }
}
