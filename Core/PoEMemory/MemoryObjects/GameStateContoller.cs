using System;
using System.Collections.Generic;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class TheGame : RemoteMemoryObject
    {
        //I hope this caching will works fine
        private static long PreGameStatePtr = -1;
        private static long LoginStatePtr = -1;
        private static long SelectCharacterStatePtr = -1;
        private static long WaitingStatePtr = -1;
        private static long InGameStatePtr = -1;
        private static long LoadingStatePtr = -1;
        private static long EscapeStatePtr = -1;
        private static TheGame Instance;
        private readonly CachedValue<int> _AreaChangeCount;
        private readonly CachedValue<bool> _inGame;
        public readonly Dictionary<string, GameState> AllGameStates;
        private readonly int CurrentAreaHashOff;
        private readonly int DataOff;
        private bool initialized = false;

        public TheGame(IMemory m, Cache cache)
        {
            pM = m;
            pCache = cache;
            pTheGame = this;
            Instance = this;
            Address = m.Read<long>(m.BaseOffsets[OffsetsName.GameStateOffset] + m.AddressOfProcess);
            _AreaChangeCount = new TimeCache<int>(() => M.Read<int>(M.AddressOfProcess + M.BaseOffsets[OffsetsName.AreaChangeCount]), 50);

            AllGameStates = ReadGameStates(Address);

            PreGameStatePtr = AllGameStates["PreGameState"].Address;
            LoginStatePtr = AllGameStates["LoginState"].Address;
            SelectCharacterStatePtr = AllGameStates["SelectCharacterState"].Address;
            WaitingStatePtr = AllGameStates["WaitingState"].Address;
            InGameStatePtr = AllGameStates["InGameState"].Address;
            LoadingStatePtr = AllGameStates["LoadingState"].Address;
            EscapeStatePtr = AllGameStates["EscapeState"].Address;
            LoadingState = AllGameStates["AreaLoadingState"].AsObject<AreaLoadingState>();
            IngameState = AllGameStates["InGameState"].AsObject<IngameState>();

            _inGame = new FrameCache<bool>(
                () => IngameState.Address != 0 && IngameState.Data.Address != 0 && IngameState.ServerData.Address != 0 && !IsLoading /*&&
                                                 IngameState.ServerData.IsInGame*/);

            Files = new FilesContainer(m);
            DataOff = Extensions.GetOffset<IngameStateOffsets>(nameof(IngameStateOffsets.Data));
            CurrentAreaHashOff = Extensions.GetOffset<IngameDataOffsets>(nameof(IngameDataOffsets.CurrentAreaHash));
        }

        public FilesContainer Files { get; set; }
        public AreaLoadingState LoadingState { get; }
        public IngameState IngameState { get; }
        public IList<GameState> CurrentGameStates => M.ReadDoublePtrVectorClasses<GameState>(Address + 0x8, IngameState);
        public IList<GameState> ActiveGameStates => M.ReadDoublePtrVectorClasses<GameState>(Address + 0x20, IngameState, true);
        public bool IsPreGame => GameStateActive(PreGameStatePtr);
        public bool IsLoginState => GameStateActive(LoginStatePtr);
        public bool IsSelectCharacterState => GameStateActive(SelectCharacterStatePtr);
        public bool IsWaitingState => GameStateActive(WaitingStatePtr); //This happens after selecting character, maybe other cases
        public bool IsInGameState => GameStateActive(InGameStatePtr); //In game, with selected character
        public bool IsLoadingState => GameStateActive(LoadingStatePtr);
        public bool IsEscapeState => GameStateActive(EscapeStatePtr);
        public bool IsLoading => LoadingState.IsLoading;
        public int AreaChangeCount => _AreaChangeCount.Value;
        public bool InGame => _inGame.Value;

        public uint CurrentAreaHash
        {
            get
            {
                var hash = M.Read<uint>(IngameState.Address + DataOff, CurrentAreaHashOff);
                return hash;
            }
        }

        public void Init()
        {
        }

        private static bool GameStateActive(long stateAddress)
        {
            var gameStateController = Instance;
            if (gameStateController == null) return false;
            var M = gameStateController.M;
            var address = Instance.Address + 0x20;
            var start = M.Read<long>(address);

            //var end = Read<long>(address + 0x8);
            var last = M.Read<long>(address + 0x10);

            var length = (int) (last - start);
            var bytes = M.ReadMem(start, length);

            for (var readOffset = 0; readOffset < length; readOffset += 16)
            {
                var pointer = BitConverter.ToInt64(bytes, readOffset);
                if (stateAddress == pointer) return true;
            }

            return false;
        }

        /// <summary>
        /// Upper bound on nodes walked while reading the game-state map. The map holds a handful of
        /// entries (13 in the reference client), so this is generous by three orders of magnitude.
        ///
        /// It exists because the walk is driven entirely by pointers read out of the game: when the
        /// offsets no longer match the running client, <see cref="GameStateHashNode.IsNull"/> reads a
        /// byte of unrelated memory and almost never says "null", so Previous/Next keep yielding fresh
        /// garbage addresses and both the stack and the dictionary grow without end. Observed
        /// 2026-09-14 against a client this fork's signatures do not match: two processes reached
        /// ~4 GiB working set each before being killed. Same guard, and the same wording of the log,
        /// as ServerInventory.ReadHashMap.
        /// </summary>
        private const int MaxHashMapNodes = 512;

        /// <summary>Offset of the game-state array inside the controller object.</summary>
        private const int GameStateArrayOffset = 0x48;

        /// <summary>Stride of one entry: a pair of pointers to two base subobjects of one state.</summary>
        private const int GameStateArrayStride = 0x10;

        /// <summary>
        /// Reads the game states as an ARRAY indexed by <see cref="GameStateTypes"/>.
        ///
        /// The client used to expose them as a std::map keyed by the state name, which is what
        /// <see cref="ReadHashMap"/> below walks. That map is gone from the client observed
        /// 2026-09-14: at controller+0x48 there is now a plain array of 12 entries, stride 0x10,
        /// each entry a pair (state + 0x10, state) — the two base subobjects of one state object
        /// under multiple inheritance. Twelve is exactly <see cref="GameStateTypes"/> minus its
        /// synthetic <c>GameNotLoaded</c>, and the array ends at 0x48 + 12 * 0x10 = 0x108, right
        /// where unrelated data begins.
        ///
        /// The index order is not assumed: it was confirmed against the client's own list of
        /// ACTIVE states (controller+0x20), whose single entry pointed at slot 4 while the client
        /// was in game — matching <c>GameStateTypes.InGameState == 4</c>.
        ///
        /// The address stored per state is <c>state + 0x10</c>, because that is what the active
        /// list holds and what <see cref="GameStateActive"/> compares against.
        ///
        /// Names, not indices, are the key of the returned dictionary, so every existing caller
        /// (<c>AllGameStates["InGameState"]</c> and friends) keeps working unchanged.
        /// </summary>
        private Dictionary<string, GameState> ReadGameStates(long controller)
        {
            var result = new Dictionary<string, GameState>();

            var moduleBase = M.AddressOfProcess;
            var moduleEnd = moduleBase + M.Process.MainModule.ModuleMemorySize;

            foreach (GameStateTypes type in Enum.GetValues(typeof(GameStateTypes)))
            {
                var index = (int)type;

                // GameNotLoaded is synthetic: the client has no object for it.
                if (index < 0 || index >= 12) continue;

                var entry = M.Read<long>(controller + GameStateArrayOffset + index * GameStateArrayStride);
                if (entry == 0) continue;

                // A real state object starts with a vtable pointer into the game module. Checking it
                // keeps a stale layout from filling the dictionary with plausible-looking garbage —
                // the failure mode this whole subsystem was rewritten to escape.
                var primary = entry - 0x10;
                var vtable = M.Read<long>(primary);

                if (vtable < moduleBase || vtable >= moduleEnd)
                {
                    DebugWindow.LogError(
                        $"GameState '{type}' (index {index}) at 0x{primary:X} has no vtable inside the " +
                        "game module: the game-state array layout does not match this client.");
                    continue;
                }

                result[type.ToString()] = GetObject<GameState>(entry);
            }

            return result;
        }

        /// <summary>
        /// Walks a std::map keyed by a native string. NO LONGER USED for game states — the client
        /// dropped that map (see <see cref="ReadGameStates"/>). Kept because it documents the old
        /// layout for the next patch-day comparison, and because its bound is the record of a real
        /// incident: without it, a walk over a stale pointer grew the process to ~4 GiB.
        /// </summary>
        private Dictionary<string, GameState> ReadHashMap(long pointer)
        {
            var result = new Dictionary<string, GameState>();

            var stack = new Stack<GameStateHashNode>();
            var startNode = ReadObject<GameStateHashNode>(pointer);
            var item = startNode.Root;
            stack.Push(item);

            var limitMax = MaxHashMapNodes;

            while (stack.Count != 0)
            {
                var node = stack.Pop();

                if (!node.IsNull)
                    result[node.Key] = node.Value1;

                var prev = node.Previous;

                if (!prev.IsNull)
                    stack.Push(prev);

                var next = node.Next;

                if (!next.IsNull)
                    stack.Push(next);

                if (limitMax-- < 0)
                {
                    // Visible, not silent: an unbounded walk here means the offsets are wrong, and
                    // that is worth saying out loud rather than returning a half-read map.
                    DebugWindow.LogError(
                        $"Fixed possible memory leak (GameStateContoller.ReadHashMap): walked over " +
                        $"{MaxHashMapNodes} nodes from 0x{pointer:X}; game state offsets likely do not " +
                        "match the running client.");
                    break;
                }
            }

            return result;
        }

        private class GameStateHashNode : RemoteMemoryObject
        {
            public GameStateHashNode Previous => ReadObject<GameStateHashNode>(Address);
            public GameStateHashNode Root => ReadObject<GameStateHashNode>(Address + 0x8);
            public GameStateHashNode Next => ReadObject<GameStateHashNode>(Address + 0x10);

            //public readonly byte Unknown;
            public bool IsNull => M.Read<byte>(Address + 0x19) != 0;

            //private readonly byte byte_0;
            //private readonly byte byte_1;
            public string Key => M.ReadNativeString(Address + 0x20);

            //public readonly int Useless;
            public GameState Value1 => ReadObject<GameState>(Address + 0x40);

            //public readonly long Value2;
        }
    }

    public class GameState : RemoteMemoryObject
    {
        private string stateName;
        public string StateName => stateName ?? (stateName = M.ReadNativeString(Address + 0x10));

        public override string ToString()
        {
            return StateName;
        }
    }

    public class AreaLoadingState : GameState
    {
        //This is actualy pointer to loading screen stuff (image, etc), but should works fine.
        public bool IsLoading => M.Read<long>(Address + 0xD8) == 1;
        public string AreaName => M.ReadStringU(M.Read<long>(Address + 0x1F0));

        public override string ToString()
        {
            return $"{AreaName}, IsLoading: {IsLoading}";
        }
    }
}
