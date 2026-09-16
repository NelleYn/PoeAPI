using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;
using GameOffsets.Native;
using SharpDX;

// GameOffsets.Components and ExileCore.PoEMemory.Components both declare Chest, Life, Player and a
// dozen more; importing the former wholesale would make every one of those names ambiguous here.
// Only the component header is needed, so only the component header is imported.
using ComponentHeader = GameOffsets.Components.ComponentHeader;

namespace ExileCore.PoEMemory.MemoryObjects
{
    public class Entity : RemoteMemoryObject
    {
        /// <summary>
        /// Component objects already materialized for this entity, one per component type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CONCURRENT, NOT LOCKED, AND THAT IS A CHOICE. This map is written by
        /// <see cref="GetComponent{T}"/> and <see cref="GetComponentFromMemory{T}"/> — the two
        /// hottest accessors in the class, reached from every plugin thread — and emptied by
        /// <see cref="ResetForNewIdentity"/>. As a plain <see cref="Dictionary{TKey,TValue}"/> all
        /// three ran with no mutual exclusion whatsoever while <see cref="GetComponents"/> next door
        /// took <c>locker</c>. That is not a stale-data problem, it is a corruption one: a Dictionary
        /// resize racing a lookup can spin forever inside TryGetValue or throw out of it, on
        /// whichever thread happened to ask.
        /// </para>
        /// <para>
        /// WHY NOT <c>lock (locker)</c> AT THE THREE POINTS. <c>locker</c> is the same lock
        /// <see cref="GetComponents"/> holds WHILE IT READS PROCESS MEMORY, and
        /// <see cref="GetComponent{T}"/> reaches GetComponents through <see cref="CacheComp"/> — so
        /// locking the accessor would queue every component read of every thread behind one remote
        /// memory scan. The map also carries no invariant spanning two entries: each entry is an
        /// independent memoization of <c>GetObject&lt;T&gt;(address)</c>, and two threads racing on
        /// one key produce equivalent objects. Per-entry atomicity is exactly what is needed, and it
        /// is exactly what ConcurrentDictionary provides, with lock-free reads on that hot path.
        /// </para>
        /// <para>
        /// WHAT THIS DOES NOT FIX, said plainly rather than left to be discovered: a writer that has
        /// already read its address out of <see cref="CacheComp"/> can still land after a
        /// <c>Clear()</c> and leave one entry belonging to the previous identity behind. Locking the
        /// three points would not close that window either — the address is read before the lock
        /// would be taken — so it is not an argument between the two options, and the owner
        /// re-checks in the accessors are what catches such an entry today.
        /// </para>
        /// </remarks>
        private readonly ConcurrentDictionary<Type, Component> _cacheComponents = new ConcurrentDictionary<Type, Component>();

        private readonly CachedValue<bool> _hiddenCheckCache;
        private Vector3 _boundsCenterPos = Vector3.Zero;
        private Dictionary<string, long> _cacheComponents2;
        private long? _componentLookup;
        private long _componentScanRetryAtMs;
        private bool _componentScanLogged;
        private PositionedCrossCheck _positionedCheck;
        private float _distancePlayer = float.MaxValue;
        private EntityOffsets? _entityOffsets;
        private Vector2 _gridPos = Vector2.Zero;
        private long? _id;
        private uint? _inventoryId;
        private bool _isAlive;
        private bool _isDead;
        private CachedValue<bool> _isHostile;
        private bool _isOpened;
        private bool _isTargetable;
        private string _metadata;
        private string _path;
        private Vector3 _pos = Vector3.Zero;
        private MonsterRarity? _rarity;
        private string _renderName = "Empty";
        private Dictionary<GameStat, int> _stats;
        private readonly ValidCache<List<Buff>> buffCache;
        private bool isHidden;
        private readonly object locker = new object();
        private int pathReadErrorTimes;

        /// <summary>
        /// Hard ceiling on the number of metadata lookup slots read for one entity by the
        /// independent control <see cref="CountNonEmptyLookupSlots"/>. Measured 2026-09-16 on a live
        /// client: the slot capacity is a power of two and was 8, 16 or 32 on every entity seen.
        /// </summary>
        /// <remarks>
        /// Tripping the cap means the pointer is stale or the offset is wrong — NOT that this entity
        /// is unusually rich. The control therefore reports failure instead of truncating: a
        /// truncated count would quietly disagree with the component count and turn a working
        /// cross-check into a source of false alarms.
        /// </remarks>
        private const int MaxComponentSlots = 32;

        /// <summary>
        /// Hard ceiling on the number of component pointers read for one entity. The largest
        /// composition measured on 2026-09-16 was 14 components (13 on a monster, 12 on the player),
        /// so this is double the observed maximum. Exceeding it is a bail-out, not a clamp, for the
        /// same reason as <see cref="MaxComponentSlots"/>.
        /// </summary>
        private const int MaxComponents = 32;

        /// <summary>
        /// Minimum interval, in milliseconds, between two component-scan attempts on one entity
        /// address. THE BUDGET IS TIME, NOT ATTEMPTS, and that is the whole point of the number.
        /// </summary>
        /// <remarks>
        /// <para>
        /// WHY A PER-ATTEMPT BUDGET WAS WRONG. <c>ParseType</c> calls <c>HasComponent&lt;&gt;</c>
        /// about fourteen times in a row, and every one of those calls goes through
        /// <see cref="CacheComp"/>. With a three-attempt lifetime budget, ONE unlucky read — an
        /// entity caught mid-update, which is a normal event — burned the whole budget inside a
        /// single <c>ParseType</c> call and left that entity permanently without components until
        /// its address happened to change. A transient fault became a permanent one, which is the
        /// exact inversion of what a retry budget is for.
        /// </para>
        /// <para>
        /// WHY A TIME BUDGET IS STILL A BUDGET. The fourteen calls of one <c>ParseType</c>, and every
        /// accessor call in the frames that follow, collapse into AT MOST ONE scan per entity per
        /// interval — so the cost of a permanently broken entity is bounded at four bounded scans a
        /// second instead of dozens per frame, and a transient failure heals by itself a quarter of
        /// a second later instead of never. The bound matters: unbounded re-walking of stale
        /// pointers is the cheap form of the runaway that once drove this process to ~4 GiB.
        /// </para>
        /// <para>
        /// 250 ms is a CHOICE, not a measurement: it is long enough to swallow a whole frame's worth
        /// of accessor calls at any frame rate this overlay runs at, and short enough that a
        /// recovering entity is back within a quarter second.
        /// </para>
        /// </remarks>
        private const int ComponentScanRetryIntervalMs = 250;

        /// <summary>
        /// Whether a component whose owner pointer does not point back at this entity is DROPPED
        /// (true) or merely counted and logged (false). Default false.
        /// </summary>
        /// <remarks>
        /// <para>
        /// THE OFFSET IS NOW MEASURED, AND THE CHECK STILL DOES NOT DECIDE ANYTHING. The 2026-09-16
        /// zone survey confirmed by content that <c>[component + 0x08]</c> holds the address of the
        /// component's own entity for EVERY component it resolved, so the check is sound. What is
        /// deliberate is the reaction to a mismatch.
        /// </para>
        /// <para>
        /// WHY A MISMATCH IS TREATED AS TRANSIENT — HYPOTHESIS, LABELLED AS ONE. The working
        /// explanation is that the component array was read while the client was rebuilding it, which
        /// would make the mismatch momentary and self-healing. NOTHING HAS BEEN MEASURED THAT SHOWS
        /// THIS. No pass has watched a mismatching component and seen it come back correct, and no
        /// pass has caught the client mid-rebuild. It is a guess that happens to be the reason this
        /// flag is off, which makes it the first thing to test if
        /// <see cref="ComponentModelAudit.OwnerMismatches"/> ever runs high.
        /// </para>
        /// <para>
        /// WHAT LEAVING THE FLAG OFF ACTUALLY COSTS, stated accurately because the previous version of
        /// this comment stated it wrongly. That version claimed every consumer inside this class that
        /// could be misled already re-checks <c>OwnerAddress</c>, so keeping a foreign component "costs
        /// nothing". THAT IS FALSE. The accessors that do re-check are <see cref="GridPos"/>,
        /// <see cref="Stats"/> and <see cref="IsAlive"/> — and, since the pass that corrected this
        /// comment, <see cref="Pos"/>, <see cref="BoundsCenterPos"/> and <see cref="RenderName"/>. The
        /// accessors that DO NOT re-check are <see cref="IsOpened"/> (Chest and Targetable),
        /// <see cref="IsTargetable"/> (Targetable), <see cref="IsHostile"/> (Positioned),
        /// <see cref="Rarity"/> (ObjectMagicProperties) and <see cref="IsHidden"/> /
        /// <see cref="Buffs"/> (Life). Nor does anything outside this class: <see cref="GetComponent{T}"/>
        /// hands a plugin the component with no owner check at all.
        /// </para>
        /// <para>
        /// SO THE CHOICE IS BETWEEN TWO WRONG ANSWERS, not between a safe one and a risky one.
        /// Filtering turns a foreign component into a MISSING one, and a missing component is
        /// indistinguishable from an entity that genuinely lacks it — which silently rewrites
        /// <c>Type</c>, because <see cref="ParseType"/> is a chain of <see cref="HasComponent{T}"/>
        /// calls and its answer is cached. Not filtering hands the foreign component to the unguarded
        /// accessors listed above. The flag stays OFF because the first failure is silent and the
        /// second is counted: every mismatch lands in
        /// <see cref="ComponentModelAudit.OwnerMismatches"/>, which is the evidence that should decide
        /// this question. Flip the flag the day that counter stops being near zero — it is one
        /// assignment.
        /// </para>
        /// </remarks>
        public static bool ComponentOwnerCheckIsFilter { get; set; }

        public Entity()
        {
            _hiddenCheckCache = new LatancyCache<bool>(() =>
            {
                if (IsValid)
                {
                    // HasComponent<Life>() answering true does NOT promise GetComponent<Life>() a
                    // component: the two read CacheComp separately, and an address change between
                    // them resets it. The old one-liner dereferenced that null.
                    var life = HasComponent<Life>() ? GetComponent<Life>() : null;
                    isHidden = life != null && life.HasBuff("hidden_monster");
                }

                return isHidden;
            }, 50);

            buffCache = this.ValidCache(() => GetComponent<Life>()?.Buffs);
        }

        public static Entity Player { get; set; }

        public float DistancePlayer
        {
            get
            {
                if (Player == null)
                    return _distancePlayer;

                if (IsValid)
                {
                    _distancePlayer = Player.GridPos.Distance(GridPos);
                    return _distancePlayer;
                }

                _distancePlayer = Player.GridPos.Distance(GridPos);
                return _distancePlayer;
            }
        }

        public EntityOffsets EntityOffsets
        {
            get
            {
                if (_entityOffsets != null)
                    return _entityOffsets.Value;

                if (Address != 0)
                    _entityOffsets = M.Read<EntityOffsets>(Address);

                if (_entityOffsets != null) return _entityOffsets.Value;
                IsValid = false;
                return default;
            }
        }

        public EntityType Type { get; private set; }
        public LeagueType League { get; private set; } = LeagueType.General;
        /// <summary>
        /// Base address of this entity's component pointer array — i.e. the first of
        /// <see cref="EntityOffsets.ComponentCount"/> consecutive component pointers.
        /// </summary>
        /// <remarks>
        /// Kept as a <c>long</c> for API compatibility: it is the same number the previous
        /// single-field <c>EntityOffsets.ComponentList</c> held. What changed is that the count
        /// beside it is now readable too — see <see cref="EntityOffsets.ComponentsArray"/>.
        /// </remarks>
        public long ComponentList => EntityOffsets.ComponentsArray.First;

        public bool IsHidden => _hiddenCheckCache.Value;

        /// <summary>One-line dump of the raw entity layout, for diagnostics only.</summary>
        public string Debug =>
            $"Details: {EntityOffsets.EntityDetails:X} " +
            $"Comps: {EntityOffsets.ComponentsArray.First:X}..{EntityOffsets.ComponentsArray.Last:X} " +
            $"({EntityOffsets.ComponentCount}) PositionedPtr: {EntityOffsets.PositionedPtr:X} " +
            $"Check: {_positionedCheck}";
        public uint Version { get; set; }
        public bool IsValid { get; set; }

        public bool IsAlive
        {
            get
            {
                if (!IsValid)
                    return _isAlive;

                var life = GetComponent<Life>();

                if (life == null || life.OwnerAddress != Address)
                {
                    if (_distancePlayer < 70)
                        _isAlive = false;

                    return _isAlive;
                }

                _isAlive = life.CurHP > 0;
                return _isAlive;
            }
        }

        public Vector3 Pos
        {
            get
            {
                if (!IsValid)
                    return _pos;

                var render = GetComponent<Render>();

                if (render == null)
                    return _pos;

                // OWNER RE-CHECK, exactly as GridPos does it. With ComponentOwnerCheckIsFilter off a
                // component whose owner pointer names ANOTHER entity stays in the map, and this
                // getter would then publish that entity's coordinates as this one's. Returning the
                // last position known to belong to this entity is wrong in a way the caller can
                // survive; returning someone else's is not.
                if (render.OwnerAddress != Address)
                    return _pos;

                _pos.X = render.X;
                _pos.Y = render.Y;
                _pos.Z = render.Z + render.Bounds.Z;
                return _pos;
            }
        }

        public System.Numerics.Vector3 PosNum => Pos.ToVector3Num();

        public Vector3 BoundsCenterPos
        {
            get
            {
                if (!IsValid)
                    return _boundsCenterPos;

                var render = GetComponent<Render>();

                if (render == null)
                    return _boundsCenterPos;

                // Same owner re-check as Pos, and for the same reason: this value is an interaction
                // target, so a foreign one aims the consumer at the wrong entity.
                if (render.OwnerAddress != Address)
                    return _boundsCenterPos;

                _boundsCenterPos = render.InteractCenter;
                return _boundsCenterPos;
            }
        }

        public Vector2 GridPos
        {
            get
            {
                if (!IsValid)
                    return _gridPos;

                var positioned = GetComponent<Positioned>();

                if (positioned == null)
                    return _gridPos;

                if (positioned.OwnerAddress != Address)
                    return _gridPos;

                _gridPos = positioned.GridPos;
                return _gridPos;
            }
        }

        public System.Numerics.Vector2 GridPosNum => GridPos.ToVector2Num();

        public string RenderName
        {
            get
            {
                if (!IsValid)
                    return _renderName;

                var render = GetComponent<Render>();

                if (render == null)
                    return _renderName;

                // Same owner re-check as Pos. A foreign render name is the worst of the three: it is
                // what consumers match entities BY, so one bad frame can attach another monster's
                // name to this object and everything downstream follows it.
                if (render.OwnerAddress != Address)
                    return _renderName;

                _renderName = render.Name;
                return _renderName;
            }
        }

        public MonsterRarity Rarity => (MonsterRarity) (_rarity = _rarity ?? (GetComponent<ObjectMagicProperties>()?.Rarity ?? MonsterRarity.White));

        public bool IsOpened
        {
            get
            {
                if (!IsValid)
                    return _isOpened;

                var chest = GetComponent<Chest>();

                if (chest == null)
                    return _isOpened;

                var targetable = GetComponent<Targetable>();

                if (targetable == null)
                    return _isOpened;

                _isOpened = !targetable.isTargetable || chest.IsOpened;
                return _isOpened;
            }
        }

        public bool IsDead
        {
            get
            {
                if (!IsValid)
                    return _isDead;

                _isDead = !_isAlive;
                return _isDead;
            }
        }

        public Dictionary<GameStat, int> Stats
        {
            get
            {
                if (!IsValid)
                    return _stats;

                var stats = GetComponent<Stats>();

                if (stats == null)
                    return _stats;

                if (stats.OwnerAddress != Address)
                {
                    // NULL IS A NORMAL ANSWER HERE, not an exceptional one: GetComponentFromMemory
                    // returns null whenever the component map cannot be read, whenever the entity is
                    // inside the ComponentScanRetryIntervalMs cooldown, and whenever the map simply
                    // has no key for this type. Dereferencing it threw NullReferenceException on
                    // whichever thread happened to read Stats. The same guard already stands in
                    // CheckComponentForValid; this is that guard.
                    stats = GetComponentFromMemory<Stats>();

                    if (stats == null || stats.OwnerAddress != Address)
                        return _stats;
                }

                var statsStatDictionary = stats.StatDictionary;

                if (statsStatDictionary.Count == 0 && (_stats == null || _stats.Count != 0))
                    return _stats;

                _stats = statsStatDictionary;

                return _stats;
            }
        }

        public bool IsTargetable
        {
            get
            {
                if (!IsValid)
                {
                    if (_isTargetable && DistancePlayer < 100)
                        _isTargetable = false;

                    return _isTargetable;
                }

                var targetable = GetComponent<Targetable>();
                _isTargetable = targetable != null && targetable.isTargetable;
                return _isTargetable;
            }
        }

        public List<Buff> Buffs => buffCache.Value;
        private string CachePath { get; set; }

        public string Path
        {
            get
            {
                if (_path == null)
                {
                    if (EntityOffsets.EntityDetails == 0)
                    {
                        if (CachePath == null)
                        {
                            IsValid = false;
                            return null;
                        }

                        return CachePath;
                    }

                    var p = M.Read<PathEntityOffsets>(EntityOffsets.EntityDetails);

                    if (p.Path.Ptr == 0)
                    {
                        if (CachePath == null)
                        {
                            IsValid = false;
                            return null;
                        }

                        return CachePath;
                    }

                    _path = Cache.StringCache.Read($"{p.Path.Ptr}{p.Length}", () => p.ToString(M));

                    if (!_path.StartsWith("Metadata"))
                    {
                        _path = M.Read<PathEntityOffsets>(EntityOffsets.EntityDetails).ToString(M);

                        Cache.StringCache.Remove($"{p.Path.Ptr}{p.Length}");
                    }

                    if (_path.Length > 0 && _path[0] != 'M')
                    {
                        pathReadErrorTimes++;
                        IsValid = false;
                        _path = null;

                        if (pathReadErrorTimes > 10)
                        {
                            _path = "ERROR PATH";
                            DebugWindow.LogError("Entity path error.");
                        }
                    }
                    else
                        CachePath = _path;
                }

                return _path;
            }
        }

        public string Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    if (Path != null)
                    {
                        var splitIndex = Path.IndexOf("@", StringComparison.Ordinal);

                        if (splitIndex != -1)
                            _metadata = Path.Substring(0, splitIndex);
                        else
                            return Path;
                    }
                }

                return _metadata;
            }
        }

        /// <summary>
        /// Address of the component lookup table shared by every entity with this entity's metadata,
        /// or 0 when it cannot be reached.
        /// </summary>
        /// <remarks>
        /// NOT ON THE RESOLUTION PATH. Components are named by their own vtable
        /// (<see cref="GetComponents"/>), which needs no metadata lookup at all; this chain is read
        /// only by the independent control <see cref="CountNonEmptyLookupSlots"/>. The live root is
        /// <c>[details + 0x28]</c> (<see cref="ComponentLookupOffsets.PointerOffsetInEntityDetails"/>);
        /// the chain the fork used before, <c>[details + 0x38] -&gt; +0x30</c>, is dead on this
        /// client — its middle link reads 0x0000000200000007, which is not a mapped address.
        /// <para>
        /// Both links are shape-checked rather than compared against zero:
        /// <c>Memory.Read&lt;T&gt;</c> hands back <c>default</c> on an unreadable address, and a
        /// wrong offset holds garbage far more often than it holds zero, so "!= 0" would let the
        /// garbage straight through.
        /// </para>
        /// </remarks>
        private long ComponentLookup
        {
            get
            {
                if (_componentLookup != null)
                    return _componentLookup.Value;

                var details = EntityOffsets.EntityDetails;

                if (!NativePointer.IsCanonical(details))
                    return (long) (_componentLookup = 0);

                var lookup = M.Read<long>(details + ComponentLookupOffsets.PointerOffsetInEntityDetails);

                if (!NativePointer.IsCanonical(lookup))
                    lookup = 0;

                return (long) (_componentLookup = lookup);
            }
        }

        /// <summary>
        /// Entity id, stable within the current area. Read through <see cref="EntityOffsets"/>, i.e.
        /// out of the already-cached struct; the literal offset lives in
        /// <see cref="EntityOffsets.IdOffset"/> and nowhere else in this file.
        /// </summary>
        public uint Id => (uint) (_id = _id ?? EntityOffsets.Id);

        /// <summary>Inventory slot id, where applicable. Read through <see cref="EntityOffsets"/>.</summary>
        public uint InventoryId => (uint) (_inventoryId = _inventoryId ?? EntityOffsets.InventoryId);

        /// <summary>
        /// This entity's components as component name to component address, or null when the entity
        /// could not be read.
        /// </summary>
        /// <remarks>
        /// The key is the C# type name (<c>typeof(T).Name</c>) — the same contract as before and the
        /// same contract the reference distribution exposes, so external readers of this dictionary
        /// are unaffected by the change of model underneath. What changed is where the name comes
        /// from: the component's OWN VTABLE, resolved through <see cref="ComponentVtables"/>.
        /// Components whose vtable has never been measured appear under a synthetic
        /// <c>"?rva0x..."</c> key (or <c>"?vt0x..."</c> when the vtable is not even inside the game
        /// module), which no <c>typeof(T).Name</c> can equal — so they are VISIBLE to any diagnostic
        /// that enumerates this dictionary, with their address as the value, and unreachable through
        /// <see cref="HasComponent{T}"/> / <see cref="GetComponent{T}"/>.
        /// </remarks>
        public Dictionary<string, long> CacheComp
        {
            get
            {
                if (_cacheComponents2 != null)
                    return _cacheComponents2;

                // Do NOT re-walk a broken entity on every single access: this property is read dozens
                // of times per entity per frame and a stale pointer walked in a loop is how this
                // process once grew to ~4 GiB. The budget is TIME and not a number of attempts, see
                // ComponentScanRetryIntervalMs for why that distinction is the whole fix.
                var now = Environment.TickCount64;

                if (now < _componentScanRetryAtMs)
                    return null;

                _cacheComponents2 = GetComponents();

                if (_cacheComponents2 == null)
                    _componentScanRetryAtMs = now + ComponentScanRetryIntervalMs;

                return _cacheComponents2;
            }
        }
        public bool IsHostile =>
            _isHostile?.Value ?? (_isHostile = new TimeCache<bool>(() => (GetComponent<Positioned>()?.Reaction & 0x7f) != 1, 100)).Value;
        private Dictionary<Type, object> PluginData { get; } = new Dictionary<Type, object>();
        public event EventHandler<Entity> OnUpdate;

        public override string ToString()
        {
            return $"<{Type}> ({Rarity}) {Metadata}: ({Address:X})";
        }

        public float Distance(Entity entity)
        {
            return GridPos.Distance(entity.GridPos);
        }

        /// <summary>
        /// Drops everything this object has cached ABOUT ONE IDENTITY, so the next read of any
        /// accessor has to go back to memory.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ONE LIST, TWO CALLERS, AND THAT IS THE POINT. The identity behind this object can change
        /// in exactly two places: <see cref="OnAddressChange"/> (the object was re-pointed at another
        /// entity) and <see cref="Check"/> (the SAME address now answers with a different entity id,
        /// i.e. the client recycled the object). Those two used to reset different sets — Check
        /// cleared four fields, OnAddressChange all of them — and Check then called <see cref="ParseType"/>
        /// immediately. ParseType is a chain of <see cref="HasComponent{T}"/> calls over
        /// <see cref="CacheComp"/>, which Check did NOT reset, so the NEW identity was classified
        /// from the PREVIOUS identity's component map and the answer was cached on this object as
        /// <see cref="Type"/>. A single list called from both callers BEFORE ParseType is the only
        /// arrangement in which the two paths cannot drift apart again.
        /// </para>
        /// <para>
        /// THE ID RESET IS WHAT MAKES Check(uint) A CHECK AT ALL. <see cref="Id"/> is
        /// <c>_id ?? EntityOffsets.Id</c>, so while <c>_id</c> survives, the property keeps handing
        /// out the id of the previous identity and comparing it against the caller's argument is a
        /// tautology. <c>_entityOffsets</c> is part of the same reset and on the Check path it is the
        /// load-bearing part: Id reads out of THAT cached struct, so clearing <c>_id</c> alone would
        /// simply re-derive the old id from the old struct. It is cleared rather than re-read here
        /// because only <see cref="OnAddressChange"/> knows the read is certainly worth doing.
        /// </para>
        /// <para>
        /// THE VALUE CACHES GO WITH THEM, and that half is newly load-bearing. <c>_rarity</c> now
        /// decides validity through <see cref="CheckRarity"/>; <c>_gridPos</c>,
        /// <c>_boundsCenterPos</c>, <c>_renderName</c> and <c>_stats</c> sit behind owner re-checks
        /// that RETURN THE CACHE when the check fails — so a value left over from another entity is
        /// not a stale number that heals next frame, it is precisely the answer those accessors are
        /// built to fall back to.
        /// </para>
        /// <para>
        /// <c>_isAlive</c>, <c>_isDead</c>, <c>_isOpened</c> and <c>_isTargetable</c> go to false and
        /// <c>_distancePlayer</c> to <see cref="float.MaxValue"/>: those are the values a freshly
        /// constructed Entity starts with, so the object is put back into "nothing known yet" rather
        /// than into another entity's answer, and false is the conservative direction for all four —
        /// nothing is alive, dead, opened or targetable until a component of THIS identity says so.
        /// <c>pathReadErrorTimes</c>, the scan cooldown and the once-per-address log gate are reset
        /// for the same reason the comment they replace gave: a new identity is a new chance, and
        /// those counters belong to the identity that failed, not to the object.
        /// </para>
        /// <para>
        /// WHAT IS DELIBERATELY NOT HERE. <see cref="Type"/> and <see cref="League"/> are left to the
        /// callers, because OnAddressChange re-parses Type only when it was already
        /// <see cref="EntityType.Error"/> — clearing Type here would silently start re-classifying
        /// every entity on every address change, which is a different change with different evidence
        /// behind it. <see cref="IsValid"/> is likewise the callers' decision. PluginData is not
        /// touched: it belongs to plugins, which never agreed that this object's identity is what
        /// keys their state.
        /// </para>
        /// </remarks>
        private void ResetForNewIdentity()
        {
            // Identity itself, and the strings derived from it.
            _id = null;
            _inventoryId = null;
            _path = null;
            _metadata = null;
            CachePath = null;
            pathReadErrorTimes = 0;

            // The raw struct every one of the above is read through.
            _entityOffsets = null;
            _componentLookup = null;

            // Components: the map, the materialized objects, and the control's verdict about them.
            _cacheComponents.Clear();
            _cacheComponents2 = null;
            _positionedCheck = PositionedCrossCheck.NotScanned;
            _componentScanRetryAtMs = 0;
            _componentScanLogged = false;

            // Values read THROUGH components. Every one of these is served back to the caller when
            // its component is missing or owned by somebody else.
            _pos = Vector3.Zero;
            _boundsCenterPos = Vector3.Zero;
            _gridPos = Vector2.Zero;
            _renderName = "Empty";
            _rarity = null;
            _stats = null;
            _isAlive = false;
            _isDead = false;
            _isOpened = false;
            _isTargetable = false;
            _distancePlayer = float.MaxValue;

            // The same values behind timed caches. _isHostile is dropped outright so the next read
            // rebuilds it; the two readonly caches cannot be dropped, so they are forced to
            // recompute instead of serving the previous identity for the rest of their window.
            _isHostile = null;
            isHidden = false;
            _hiddenCheckCache?.ForceUpdate();
            buffCache?.ForceUpdate();
        }

        protected override void OnAddressChange()
        {
            ResetForNewIdentity();

            // Re-read eagerly instead of leaving behind the null the reset wrote. An address change
            // is the one moment where this struct is certainly worth a read — almost every accessor
            // below needs it — whereas on the Check path nothing promises the new identity will be
            // touched at all, so there the lazy re-read in the property is the right cost.
            _entityOffsets = M.Read<EntityOffsets>(Address);

            if (Type == EntityType.Error)
            {
                // League is written by ParseType only on the branches that recognise a league, so a
                // League left over from an earlier classification would survive a run that
                // recognises none. Reclassifying means reclassifying both.
                League = LeagueType.General;
                Type = ParseType();
                if (Type != EntityType.Error) IsValid = true;
            }

            OnUpdate?.Invoke(this, this);
        }

        /// <summary>
        /// Confirms that this object really is the entity the caller looked it up as.
        /// </summary>
        /// <param name="entityId">The id the caller read straight out of the entity's memory.</param>
        /// <returns>True when the object may be treated as that entity.</returns>
        /// <remarks>
        /// The load-bearing comparison is <c>Id == entityId</c>, and it only means anything because
        /// <see cref="OnAddressChange"/> clears the cached id: otherwise <see cref="Id"/> replays the
        /// argument back at itself. The <c>_id != entityId</c> branch below is a SECONDARY NET for a
        /// caller that re-uses this object at the same address, where no address change fires; after
        /// an address change it cannot run, because the reset leaves nothing to compare.
        /// </remarks>
        public bool Check(uint entityId)
        {
            if (_id != null)
            {
                if (_id != entityId)
                {
                    // Logged BEFORE the reset, so the line still carries the old identity. The id is
                    // then cleared rather than assigned from the argument: writing the caller's
                    // number into the cache would re-create exactly the tautology this whole change
                    // removes, and the comparison below would pass without ever reading memory.
                    DebugWindow.LogMsg($"Was ID: {Id} New ID: {entityId} To Path: {Path}", 3);

                    // THE FULL RESET, AND BEFORE ParseType, NOT AFTER. This branch used to clear four
                    // fields and then classify, while ParseType is a chain of HasComponent<> calls
                    // over CacheComp — which was not among the four. The new identity was therefore
                    // typed from the old identity's component map, and Type is cached. The same list
                    // OnAddressChange uses is the only one that can be trusted here, because the two
                    // callers answer the same question: "nothing this object remembers is about the
                    // entity that is there now".
                    ResetForNewIdentity();

                    League = LeagueType.General;
                    Type = ParseType();
                }
            }

            if (Type != EntityType.Error)
            {
                if (Type == EntityType.Effect || Type == EntityType.Daemon) return true;

                return CacheComp != null && Id == entityId && CheckRarity();
            }

            return false;
        }

        private bool CheckRarity()
        {
            return Rarity >= MonsterRarity.White && Rarity <= MonsterRarity.Unique;
        }

        public void UpdatePointer(long newAddress)
        {
            Address = newAddress;
        }

        /// <summary>
        /// Reads this entity's component map: component name to component address.
        /// </summary>
        /// <returns>
        /// The map, or null when the entity could not be read. It NEVER returns a partially trusted
        /// map built from a pointer that failed its shape check — a half-map is indistinguishable
        /// from an entity that genuinely lacks a component, and that ambiguity is what this rewrite
        /// exists to remove.
        /// </returns>
        /// <remarks>
        /// <para>
        /// THE MODEL, measured on a live client on 2026-09-16 and confirmed by content:
        /// <code>
        /// components = entity + 0x10                 (std::vector of component pointers)
        /// vtable     = [component + 0x00]            (names the component's TYPE)
        /// owner      = [component + 0x08]            (must be this entity's address)
        /// type name  = ComponentVtables[vtable - IMemory.AddressOfProcess]
        /// </code>
        /// THE METADATA SLOT TABLE IS NOT READ AT ALL, and dropping it is the simplification, not a
        /// loss: its 32-bit key was measured NOT to identify a component type in either direction
        /// (see <see cref="ComponentVtables"/> for the evidence), while the vtable was measured to
        /// identify it in both. The table survives as an independent control only — see
        /// <see cref="CountNonEmptyLookupSlots"/>.
        /// </para>
        /// <para>
        /// THE ITERATION LIMIT IS STRUCTURAL, not a watchdog counter. The single block read is
        /// size-clamped BEFORE it happens (<see cref="MaxComponents"/>), and the only loop walks a
        /// local buffer of known length; there is no walk over foreign pointers to run away. Inside
        /// the loop each component costs exactly one 16-byte header read.
        /// </para>
        /// </remarks>
        private Dictionary<string, long> GetComponents()
        {
            lock (locker)
            {
                // 1. The entity's own component pointer array. This is the authority: its length
                //    matched the number of components the reference distribution reports, entity by
                //    entity (player 12, monster 13, chest 9, doodad 4).
                var comps = EntityOffsets.ComponentsArray;

                if (!NativePointer.IsCanonical(comps.First))
                    return FailComponentScan("component array pointer is not canonical");

                var compsBytes = comps.Last - comps.First;

                if (compsBytes <= 0 || (compsBytes & 7) != 0 || compsBytes > MaxComponents * 8)
                    return FailComponentScan($"component array span {compsBytes} is not 1..{MaxComponents} whole pointers");

                var componentCount = (int) (compsBytes / 8);

                // 2. The module base, because the table is keyed by RVA: an absolute vtable address
                //    is only valid for one launch of the client, ASLR moves it on the next.
                var moduleBase = M.AddressOfProcess;

                if (!NativePointer.IsCanonical(moduleBase))
                    return FailComponentScan($"module base {moduleBase:X} is not a canonical address");

                // 3. One bounded block read, no per-pointer syscall. The size is already clamped
                //    above, so a stale vector header cannot talk this read into a huge allocation.
                //    M.ReadStdVector is deliberately NOT used: its own guard allows up to 100000
                //    elements, three orders of magnitude past anything ever measured here.
                var pointerBlock = M.ReadMem(comps.First, componentCount * 8);

                if (pointerBlock.Length != componentCount * 8)
                    return FailComponentScan("short read of the component array");

                var pointers = MemoryMarshal.Cast<byte, long>(new ReadOnlySpan<byte>(pointerBlock));

                var result = new Dictionary<string, long>(componentCount, StringComparer.Ordinal);
                var skipped = 0;
                var ownerMismatches = 0;
                var unknown = 0;
                var duplicates = 0;

                // Allocated only when this entity actually carries a component whose vtable has never
                // been measured. Reported after the loop, see ReportUnmeasuredVtables.
                List<KeyValuePair<long, long>> unknownVtables = null;

                // 4. Walk EVERY component pointer. Order is the client's own index order, which is
                //    also the order the reference distribution lists them in.
                for (var i = 0; i < pointers.Length; i++)
                {
                    var address = pointers[i];

                    if (!NativePointer.IsCanonical(address))
                    {
                        skipped++;
                        continue;
                    }

                    // One 16-byte read yields both halves of the header: the vtable that names the
                    // type, and the owner pointer the cross-check below needs.
                    var header = M.Read<ComponentHeader>(address);

                    if (header.EntityPtr != Address)
                    {
                        ownerMismatches++;

                        // Advisory by default — see ComponentOwnerCheckIsFilter for why a measured
                        // check is still not allowed to delete components.
                        if (ComponentOwnerCheckIsFilter)
                        {
                            skipped++;
                            continue;
                        }
                    }

                    if (!NativePointer.IsCanonical(header.StaticPtr))
                    {
                        skipped++;
                        continue;
                    }

                    var rva = header.StaticPtr - moduleBase;
                    string name;

                    if (!ComponentVtables.IsPlausibleRva(rva))
                    {
                        // Not a gap in the table: a vtable outside the game module means this pointer
                        // or the module base is wrong. Different key, so the two never blur together.
                        name = ComponentVtables.ForeignVtableKey(header.StaticPtr);
                        unknown++;
                    }
                    else if (ComponentVtables.TryGetTypeName(rva, out var typeName))
                    {
                        name = typeName;
                    }
                    else
                    {
                        // The entity HAS this component; its vtable has simply never been measured.
                        // Record it under a key no typeof(T).Name can equal, so the gap is visible
                        // with its address attached instead of being dropped on the floor.
                        name = ComponentVtables.UnknownKey(rva);
                        unknown++;
                        (unknownVtables ??= new List<KeyValuePair<long, long>>())
                            .Add(new KeyValuePair<long, long>(rva, address));
                    }

                    // FIRST WINS, and a repeat is counted rather than overwritten. Two components of
                    // one type on one entity was never measured; if it happens, the diagnostic says
                    // so instead of the second address silently replacing the first.
                    if (result.ContainsKey(name))
                        duplicates++;
                    else
                        result.Add(name, address);
                }

                if (result.Count == 0)
                    return FailComponentScan($"{componentCount} component pointers read, none usable");

                // 5. THE MANDATORY BUILT-IN CONTROL. The entity carries a direct Positioned pointer
                //    at +0x98, and the survey matched it against the vtable-resolved Positioned on
                //    50 of 50 entities across seven kinds. If they disagree, the MODEL is wrong — not
                //    this entity. So: report, never repair. Preferring the direct pointer would hide
                //    the one cheap signal that proves or disproves the whole model.
                _positionedCheck = CrossCheckPositioned(result);

                ComponentModelAudit.Record(result.Count, unknown, skipped, ownerMismatches, duplicates, _positionedCheck);

                if (_positionedCheck == PositionedCrossCheck.Disagrees)
                {
                    LogComponentScanOnce(
                        $"vtable says Positioned is at {result[nameof(Positioned)]:X} but entity+0x98 says " +
                        $"{EntityOffsets.PositionedPtr:X} - the component model does not hold");
                }
                else if (skipped > 0 || duplicates > 0)
                {
                    LogComponentScanOnce(
                        $"{result.Count}/{componentCount} components mapped, {skipped} skipped, " +
                        $"{duplicates} duplicate type keys, {ownerMismatches} owner mismatches");
                }

                if (unknownVtables != null)
                    ReportUnmeasuredVtables(unknownVtables);

                return result;
            }
        }

        /// <summary>
        /// Compares the Positioned component found by vtable against the entity's own direct
        /// Positioned pointer at +0x98.
        /// </summary>
        /// <param name="map">The component map just built.</param>
        /// <returns>What the comparison established, including "there was nothing to compare".</returns>
        /// <remarks>
        /// TWO INDEPENDENT ROUTES TO ONE ADDRESS is what makes this worth anything: the left-hand
        /// side comes from the component array plus the vtable table, the right-hand side is a single
        /// field the client fills in itself. They can only agree by accident once; they agreed 50
        /// times out of 50 on 2026-09-16.
        /// </remarks>
        private PositionedCrossCheck CrossCheckPositioned(Dictionary<string, long> map)
        {
            var direct = EntityOffsets.PositionedPtr;

            if (!NativePointer.IsCanonical(direct))
                return PositionedCrossCheck.NoDirectPointer;

            if (!map.TryGetValue(nameof(Positioned), out var fromVtable))
                return PositionedCrossCheck.Missing;

            return fromVtable == direct ? PositionedCrossCheck.Agrees : PositionedCrossCheck.Disagrees;
        }

        /// <summary>
        /// Reports component vtables found on live entities that <see cref="ComponentVtables"/> has
        /// never measured — ONCE PER DISTINCT RVA FOR THE WHOLE PROCESS, with the address the
        /// component sits at.
        /// </summary>
        /// <param name="found">The unmeasured (vtable RVA, component address) pairs from one entity.</param>
        /// <remarks>
        /// <para>
        /// THE THROTTLE IS THE POINT. Until the remaining vtables are harvested, many entities carry
        /// one or two of them, so a per-entity line would emit hundreds of identical messages per
        /// zone and bury the two signals that actually matter — a hard scan failure, and a Positioned
        /// pointer that disagrees. Keyed by RVA, the output is bounded by the number of component
        /// types in the game and every line is a work item.
        /// </para>
        /// <para>
        /// EACH LINE IS THAT WORK ITEM: take the address, ask the reference distribution which
        /// component lives there, and add the (type name, RVA) pair to
        /// <see cref="ComponentVtables"/>. That is the whole harvest loop.
        /// </para>
        /// </remarks>
        private void ReportUnmeasuredVtables(List<KeyValuePair<long, long>> found)
        {
            for (var i = 0; i < found.Count; i++)
            {
                var pair = found[i];

                if (!ComponentModelAudit.NoteUnknownRva(pair.Key))
                    continue;

                DebugWindow.LogError(
                    $"ComponentVtables: unmeasured vtable RVA 0x{pair.Key:X} at component {pair.Value:X} " +
                    $"on {_path ?? "<path not read>"} (entity {Address:X}). Identify it and add the pair.");
            }
        }

        /// <summary>
        /// INDEPENDENT CONTROL, deliberately off the hot path: counts the non-empty slots of the
        /// metadata's component lookup table, which was measured equal to this entity's component
        /// count on every entity examined on 2026-09-16 (4=4, 9=9, 13=13, 14=14).
        /// </summary>
        /// <returns>
        /// The number of non-empty slots, or -1 when the table could not be read. -1 means "no
        /// opinion" and NEVER "zero components": a diagnostic must not report a failed read as a
        /// disagreement.
        /// </returns>
        /// <remarks>
        /// <para>
        /// WHY IT IS A SEPARATE METHOD AND NOT PART OF THE SCAN. As evidence it is worth real money;
        /// as a per-frame cost it is three extra dereferences and a second block read on every entity
        /// of every zone, forever, to re-establish a property of the MODEL that does not need
        /// re-establishing per entity. Diagnostics call it on a handful of entities; the scan never
        /// calls it.
        /// </para>
        /// <para>
        /// It reads the table that was refuted as a source of NAMES, and that is not a contradiction:
        /// the structure of the table is confirmed by content, only the meaning of its 32-bit key is
        /// not. Counting non-empty slots uses exactly the confirmed part and none of the refuted one.
        /// </para>
        /// </remarks>
        public int CountNonEmptyLookupSlots()
        {
            try
            {
                var lookupAddress = ComponentLookup;

                if (lookupAddress == 0)
                    return -1;

                var lookup = M.Read<ComponentLookupOffsets>(lookupAddress);

                if (!NativePointer.IsCanonical(lookup.Slots.First))
                    return -1;

                var slotBytes = lookup.Slots.Last - lookup.Slots.First;

                if (slotBytes <= 0 || (slotBytes & 7) != 0 || slotBytes > MaxComponentSlots * 8)
                    return -1;

                var slotCount = (int) (slotBytes / 8);
                var slotBlock = M.ReadMem(lookup.Slots.First, slotCount * 8);

                if (slotBlock.Length != slotCount * 8)
                    return -1;

                var slots = MemoryMarshal.Cast<byte, ComponentSlot>(new ReadOnlySpan<byte>(slotBlock));
                var used = 0;

                for (var i = 0; i < slots.Length; i++)
                {
                    if (!slots[i].IsEmpty)
                        used++;
                }

                return used;
            }
            catch (Exception e)
            {
                // A control that can take down its caller is worse than no control at all.
                DebugWindow.LogError($"Entity.CountNonEmptyLookupSlots {Address:X}: {e.GetType().Name}: {e.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Reports a component scan that could not produce a map, logs it once per address, marks the
        /// entity invalid and returns null so the caller can <c>return FailComponentScan(...)</c>.
        /// </summary>
        /// <param name="why">Why the scan could not continue, in words.</param>
        /// <returns>Always null.</returns>
        private Dictionary<string, long> FailComponentScan(string why)
        {
            LogComponentScanOnce(why);
            IsValid = false;
            return null;
        }

        /// <summary>
        /// Logs at most one component-scan line per entity address.
        /// </summary>
        /// <param name="message">What happened.</param>
        /// <remarks>
        /// Diagnostics must be audible and must not become the load: the previous implementation
        /// could log from inside its own retry loop. The path is read from the <c>_path</c> FIELD and
        /// not from the <c>Path</c> property on purpose — the property reads memory and can flip
        /// <c>IsValid</c>, which is the last thing a failure path should be doing.
        /// </remarks>
        private void LogComponentScanOnce(string message)
        {
            if (_componentScanLogged)
                return;

            _componentScanLogged = true;
            DebugWindow.LogError($"Entity.GetComponents {Address:X} {_path ?? "<path not read>"}: {message}. {Debug}");
        }

        public bool HasComponent<T>() where T : Component, new()
        {
            // CacheComp READ ONCE INTO A LOCAL. It is a property, not a field: it can rescan, and it
            // returns null as a normal outcome. `CacheComp != null && CacheComp.TryGetValue(...)`
            // evaluated it TWICE, so a concurrent OnAddressChange landing between the two evaluations
            // made the second one return null and the TryGetValue threw.
            var map = CacheComp;

            return map != null && map.TryGetValue(typeof(T).Name, out var address) && address != 0;
        }

        /// <summary>
        /// Why <see cref="HasComponent{T}"/> answered the way it did, so that a diagnostic can tell
        /// "we never measured this component type's vtable" apart from "this entity really has no
        /// such component". Both look identical through the normal accessors, on purpose — but they
        /// are completely different problems.
        /// </summary>
        public enum ComponentLookupState
        {
            /// <summary>The entity's component map could not be read at all.</summary>
            EntityNotReadable,

            /// <summary>
            /// The component type's vtable has never been measured, so this component is invisible to
            /// the fork no matter what the entity carries. Fix by measuring the vtable and adding it
            /// to <see cref="ComponentVtables"/> — see its pending-measurement list.
            /// </summary>
            TypeVtableNotMeasured,

            /// <summary>The map was read and this entity genuinely does not carry the component.</summary>
            EntityLacksComponent,

            /// <summary>The component is present and its address is in the map.</summary>
            Present,
        }

        /// <summary>
        /// Result of the built-in control: the Positioned component found by vtable against the
        /// entity's own direct Positioned pointer at +0x98.
        /// </summary>
        public enum PositionedCrossCheck
        {
            /// <summary>This entity's components have not been scanned yet.</summary>
            NotScanned,

            /// <summary>
            /// The direct pointer at +0x98 is not a plausible address, so there was nothing to
            /// compare against. NOT a disagreement, and never to be counted as one.
            /// </summary>
            NoDirectPointer,

            /// <summary>
            /// The entity has a direct Positioned pointer but no Positioned came out of the vtable
            /// walk. That is a MODEL failure, not a property of the entity.
            /// </summary>
            Missing,

            /// <summary>The two independent routes produced the same address. The expected state.</summary>
            Agrees,

            /// <summary>The two routes disagree — the component model does not hold.</summary>
            Disagrees,
        }

        /// <summary>
        /// What the built-in Positioned control said for THIS entity the last time its components
        /// were scanned. Reads no memory.
        /// </summary>
        public PositionedCrossCheck PositionedCheck => _positionedCheck;

        /// <summary>
        /// Process-wide tally of the component model's own evidence: how many components were
        /// resolved, how many vtables are still unmeasured, and — the number that matters — how often
        /// the vtable-resolved Positioned agreed with the entity's direct pointer at +0x98.
        /// </summary>
        /// <remarks>
        /// <para>
        /// WHY A STATIC TALLY AND NOT A LOG LINE. The model is a claim about EVERY entity, so the
        /// evidence is a ratio over entities, not an anecdote about one. A diagnostic prints
        /// <c>Summary()</c> in a single line after a zone has been walked and the claim is either
        /// upheld or dead on the spot. Log lines cannot be counted; these can.
        /// </para>
        /// <para>
        /// Counters only, no allocation on the hot path, and every update is interlocked because
        /// entity scans run on several threads. It is never read to make a decision — a counter that
        /// steers behaviour stops being evidence.
        /// </para>
        /// </remarks>
        public static class ComponentModelAudit
        {
            private static long _entitiesScanned;
            private static long _componentsResolved;
            private static long _componentsUnknown;
            private static long _pointersSkipped;
            private static long _ownerMismatches;
            private static long _duplicateTypeKeys;
            private static long _positionedAgrees;
            private static long _positionedDisagrees;
            private static long _positionedMissing;
            private static long _positionedNoPointer;

            private static readonly SortedSet<long> UnknownRvas = new SortedSet<long>();
            private static readonly object UnknownRvaLock = new object();

            /// <summary>Entities whose component map was built successfully.</summary>
            public static long EntitiesScanned => Interlocked.Read(ref _entitiesScanned);

            /// <summary>Components placed in a map, unmeasured ones included.</summary>
            public static long ComponentsResolved => Interlocked.Read(ref _componentsResolved);

            /// <summary>Components whose vtable is not in <see cref="ComponentVtables"/>.</summary>
            public static long ComponentsUnknown => Interlocked.Read(ref _componentsUnknown);

            /// <summary>Component pointers dropped for failing a shape check.</summary>
            public static long PointersSkipped => Interlocked.Read(ref _pointersSkipped);

            /// <summary>Components whose owner pointer did not point back at their entity.</summary>
            public static long OwnerMismatches => Interlocked.Read(ref _ownerMismatches);

            /// <summary>Second and later components claiming a type key already taken on one entity.</summary>
            public static long DuplicateTypeKeys => Interlocked.Read(ref _duplicateTypeKeys);

            /// <summary>Entities where the vtable-resolved Positioned equalled entity+0x98.</summary>
            public static long PositionedAgrees => Interlocked.Read(ref _positionedAgrees);

            /// <summary>Entities where it did not. ANY NON-ZERO VALUE HERE REFUTES THE MODEL.</summary>
            public static long PositionedDisagrees => Interlocked.Read(ref _positionedDisagrees);

            /// <summary>Entities with a direct Positioned pointer that the vtable walk did not find.</summary>
            public static long PositionedMissing => Interlocked.Read(ref _positionedMissing);

            /// <summary>Entities with no plausible pointer at +0x98, i.e. nothing to compare.</summary>
            public static long PositionedNoPointer => Interlocked.Read(ref _positionedNoPointer);

            /// <summary>
            /// The distinct unmeasured vtable RVAs seen so far, sorted. Each one is a work item for
            /// <see cref="ComponentVtables"/>.
            /// </summary>
            public static IReadOnlyList<long> UnknownVtableRvas
            {
                get
                {
                    lock (UnknownRvaLock)
                    {
                        return new List<long>(UnknownRvas);
                    }
                }
            }

            /// <summary>One line a diagnostic can print to decide whether the model holds.</summary>
            /// <returns>The tally in words and numbers.</returns>
            public static string Summary()
            {
                var rvas = UnknownVtableRvas;
                var rvaText = rvas.Count == 0 ? "none" : string.Empty;

                if (rvas.Count > 0)
                {
                    var parts = new List<string>(rvas.Count);

                    foreach (var rva in rvas)
                    {
                        parts.Add("0x" + rva.ToString("X"));
                    }

                    rvaText = string.Join(", ", parts);
                }

                return $"entities {EntitiesScanned}, components {ComponentsResolved} " +
                       $"(unmeasured vtables {ComponentsUnknown}), " +
                       $"Positioned vs entity+0x98: {PositionedAgrees} agree / {PositionedDisagrees} disagree / " +
                       $"{PositionedMissing} missing / {PositionedNoPointer} no pointer, " +
                       $"skipped {PointersSkipped}, owner mismatches {OwnerMismatches}, " +
                       $"duplicate type keys {DuplicateTypeKeys}, unmeasured RVAs: {rvaText}";
            }

            /// <summary>Clears the tally, so a diagnostic can measure one zone instead of a session.</summary>
            public static void Reset()
            {
                Interlocked.Exchange(ref _entitiesScanned, 0);
                Interlocked.Exchange(ref _componentsResolved, 0);
                Interlocked.Exchange(ref _componentsUnknown, 0);
                Interlocked.Exchange(ref _pointersSkipped, 0);
                Interlocked.Exchange(ref _ownerMismatches, 0);
                Interlocked.Exchange(ref _duplicateTypeKeys, 0);
                Interlocked.Exchange(ref _positionedAgrees, 0);
                Interlocked.Exchange(ref _positionedDisagrees, 0);
                Interlocked.Exchange(ref _positionedMissing, 0);
                Interlocked.Exchange(ref _positionedNoPointer, 0);

                lock (UnknownRvaLock)
                {
                    UnknownRvas.Clear();
                }
            }

            internal static void Record(int resolved, int unknown, int skipped, int ownerMismatches, int duplicates,
                PositionedCrossCheck positioned)
            {
                Interlocked.Increment(ref _entitiesScanned);
                Interlocked.Add(ref _componentsResolved, resolved);
                Interlocked.Add(ref _componentsUnknown, unknown);
                Interlocked.Add(ref _pointersSkipped, skipped);
                Interlocked.Add(ref _ownerMismatches, ownerMismatches);
                Interlocked.Add(ref _duplicateTypeKeys, duplicates);

                switch (positioned)
                {
                    case PositionedCrossCheck.Agrees:
                        Interlocked.Increment(ref _positionedAgrees);
                        break;
                    case PositionedCrossCheck.Disagrees:
                        Interlocked.Increment(ref _positionedDisagrees);
                        break;
                    case PositionedCrossCheck.Missing:
                        Interlocked.Increment(ref _positionedMissing);
                        break;
                    case PositionedCrossCheck.NoDirectPointer:
                        Interlocked.Increment(ref _positionedNoPointer);
                        break;
                }
            }

            /// <summary>Records an unmeasured RVA. True when this is the first sighting.</summary>
            /// <param name="rva">The vtable RVA that is not in the table.</param>
            /// <returns>True if it was not seen before, so the caller may log it exactly once.</returns>
            internal static bool NoteUnknownRva(long rva)
            {
                lock (UnknownRvaLock)
                {
                    return UnknownRvas.Add(rva);
                }
            }
        }

        /// <summary>
        /// Diagnostic counterpart of <see cref="HasComponent{T}"/>: says WHY the component is or is
        /// not available. Reads no memory beyond what <see cref="CacheComp"/> already cached.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <returns>The reason, as a <see cref="ComponentLookupState"/>.</returns>
        public ComponentLookupState GetComponentLookupState<T>() where T : Component, new()
        {
            if (!ComponentVtables.TryGetRva(typeof(T).Name, out _))
                return ComponentLookupState.TypeVtableNotMeasured;

            var map = CacheComp;

            if (map == null)
                return ComponentLookupState.EntityNotReadable;

            return map.TryGetValue(typeof(T).Name, out var address) && address != 0
                ? ComponentLookupState.Present
                : ComponentLookupState.EntityLacksComponent;
        }

        public T GetComponent<T>() where T : Component, new()
        {
            if (_cacheComponents.TryGetValue(typeof(T), out var result)) return (T) result;

            // Read once into a local, for the reason spelled out in HasComponent.
            var map = CacheComp;

            if (map != null && map.TryGetValue(typeof(T).Name, out var address))
            {
                var component = GetObject<T>(address);
                _cacheComponents[typeof(T)] = component;
                return component;
            }

            return null;
        }

        public bool CheckComponentForValid<T>() where T : Component, new()
        {
            var c = GetComponent<T>();

            // A missing component is not a valid one. This used to dereference null: the accessor is
            // documented to return null, and diagnostics must not crash their caller.
            if (c == null)
                return false;

            if (c.OwnerAddress != Address)
            {
                var componentFromMemory = GetComponentFromMemory<T>();

                return componentFromMemory != null && componentFromMemory.OwnerAddress == Address;
            }

            return true;
        }

        public T GetComponentFromMemory<T>() where T : Component, new()
        {
            // CacheComp is null for an entity whose components could not be read. Dereferencing it
            // here threw a NullReferenceException on whichever thread happened to ask.
            var map = CacheComp;

            if (map != null && map.TryGetValue(typeof(T).Name, out var address))
            {
                var component = GetObject<T>(address);
                _cacheComponents[typeof(T)] = component;
                return component;
            }

            return null;
        }

        private EntityType ParseType()
        {
            if (string.IsNullOrEmpty(Path)) return EntityType.Error;

            if (Path.StartsWith("Metadata/Effects/", StringComparison.Ordinal)) return EntityType.Effect;

            if (Path.StartsWith("Metadata/Monsters/Daemon/", StringComparison.Ordinal)) return EntityType.Daemon;

            if (Version > 0 && Id > int.MaxValue)
                return EntityType.ServerObject;

            if (HasComponent<Chest>())
            {
                if (Path.StartsWith("Metadata/Chests/DelveChests", StringComparison.Ordinal))
                {
                    League = LeagueType.Delve;
                    return EntityType.Chest;
                }

                if (Path.StartsWith("Metadata/Chests/Incursion", StringComparison.Ordinal))
                {
                    League = LeagueType.Incursion;
                    return EntityType.Chest;
                }

                if (Path.StartsWith("Metadata/Chests/Legion", StringComparison.Ordinal))
                {
                    League = LeagueType.Legion;
                    return EntityType.Chest;
                }

                return EntityType.Chest;
            }

            if (HasComponent<NPC>() && Path.StartsWith("Metadata/NPC", StringComparison.Ordinal))
                return EntityType.Npc;

            if (HasComponent<Monster>())
            {
                if (Path.StartsWith("Metadata/Monsters/LegionLeague/", StringComparison.Ordinal))
                    League = LeagueType.Legion;

                return EntityType.Monster;
            }

            if (HasComponent<Shrine>())
                return EntityType.Shrine;

            if (HasComponent<WorldItem>())
                return EntityType.WorldItem;

            if (HasComponent<Player>())
                return EntityType.Player;

            if (HasComponent<MinimapIcon>())
            {
                if (Path.Equals("Metadata/Terrain/Missions/Hideouts/Objects/HideoutCraftingBench", StringComparison.Ordinal))
                    return EntityType.CraftUnlock;

                if (HasComponent<AreaTransition>()) return EntityType.AreaTransition;

                if (Path.EndsWith("Waypoint", StringComparison.Ordinal)) return EntityType.Waypoint;

                if (HasComponent<Portal>()) return EntityType.TownPortal;

                if (HasComponent<Monolith>()) return EntityType.Monolith;

                if (HasComponent<Transitionable>() && Path.StartsWith("Metadata/MiscellaneousObjects/Abyss"))
                {
                    League = LeagueType.Abyss;
                    return EntityType.MiscellaneousObjects;
                }

                if (Path.Equals("Metadata/Terrain/Leagues/Legion/Objects/LegionInitiator", StringComparison.Ordinal))
                    return EntityType.LegionMonolith;

                if (Path.Equals("Metadata/MiscellaneousObjects/Stash", StringComparison.Ordinal)) return EntityType.Stash;
                if (Path.Equals("Metadata/MiscellaneousObjects/GuildStash", StringComparison.Ordinal)) return EntityType.GuildStash;

                if (Path.Equals("Metadata/MiscellaneousObjects/Delve/DelveCraftingBench", StringComparison.Ordinal))
                    return EntityType.DelveCraftingBench;

                if (Path.Equals("Metadata/MiscellaneousObjects/Breach/BreachObject", StringComparison.Ordinal)) return EntityType.Breach;
                if (Path.Equals("Metadata/Terrain/Leagues/Delve/Objects/DelveMineral")) return EntityType.Resource;

                return EntityType.IngameIcon;
            }

            if (HasComponent<Portal>()) return EntityType.Portal;

            if (HasComponent<HideoutDoodad>()) return EntityType.HideoutDecoration;

            if (HasComponent<Monolith>()) return EntityType.MiniMonolith;

            if (HasComponent<ClientBetrayalChoice>()) return EntityType.BetrayalChoice;

            if (HasComponent<RenderItem>()) return EntityType.Item;

            if (Path.StartsWith("Metadata/MiscellaneousObjects/Lights", StringComparison.Ordinal)) return EntityType.Light;

            if (Path.StartsWith("Metadata/Terrain", StringComparison.Ordinal)) return EntityType.Terrain;

            if (Path.StartsWith("Metadata/Pet", StringComparison.Ordinal)) return EntityType.Pet;

            return EntityType.None;
        }

        public T GetHudComponent<T>() where T : class
        {
            if (PluginData.TryGetValue(typeof(T), out var result)) return (T) result;
            return null;
        }

        public void SetHudComponent<T>(T data)
        {
            lock (locker)
            {
                PluginData[typeof(T)] = data;
            }
        }
    }
}
