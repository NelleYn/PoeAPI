using System;
using System.Collections;
using System.Collections.Generic;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;

namespace ExileCore;

/// <summary>
/// Bundle of state and settings passed to the entity-collection routine: the working buffers,
/// caches, threading options and feature toggles used while gathering entities from game memory.
/// </summary>
public class EntityCollectSettingsContainer
{
    /// <summary>Stack of newly collected entities awaiting processing.</summary>
    public Stack<Entity> Simple { get; set; }

    /// <summary>Queue of entity ids scheduled for removal.</summary>
    public Queue<uint> KeyForDelete { get; set; }

    /// <summary>The shared id-to-entity cache.</summary>
    public Dictionary<uint, Entity> EntityCache { get; set; }

    /// <summary>The multi-thread manager used for parallel collection.</summary>
    public MultiThreadManager MultiThreadManager { get; set; }

    /// <summary>Whether server-side entities should be parsed.</summary>
    public Func<ToggleNode> ParseServer { get; set; }

    /// <summary>Provides the current entity count.</summary>
    public Func<long> EntitiesCount { get; set; }

    /// <summary>The current entity version counter.</summary>
    public uint EntitiesVersion { get; set; }

    /// <summary>Whether the collection needs to run again.</summary>
    public bool NeedUpdate { get; set; } = true;

    /// <summary>Threshold above which collection is parallelized.</summary>
    public ToggleNode CollectEntitiesInParallelWhenMoreThanX { get; set; }

    /// <summary>Debug timing information for the collection routine.</summary>
    public DebugInformation DebugInformation { get; set; }

    /// <summary>Set to request the current collection pass abort (e.g. on area change).</summary>
    public bool Break { get; set; }

    /// <summary>Whether entity parsing runs across multiple threads.</summary>
    public Func<bool> ParseEntitiesInMultiThread { get; set; }
}

/// <summary>
/// Owns the live set of game entities. Runs coroutines that collect entities from memory and
/// refresh the typed/valid lookup collections, and raises add/remove events as entities change.
/// </summary>
public class EntityListWrapper
{
    private readonly CoreSettings _settings;
    private readonly int coroutineTimeWait = 100;
    private readonly Dictionary<uint, Entity> entityCache;
    private readonly GameController gameController;
    private readonly Queue<uint> keysForDelete = new Queue<uint>(24);
    private readonly Coroutine parallelUpdateDictionary;
    private readonly Stack<Entity> Simple = new Stack<Entity>(512);
    private readonly Coroutine updateEntity;
    private readonly EntityCollectSettingsContainer entityCollectSettingsContainer;
    private static EntityListWrapper _instance;

    /// <summary>Creates the wrapper, wires up the area-change handler and starts the collection coroutines.</summary>
    public EntityListWrapper(GameController gameController, CoreSettings settings, MultiThreadManager multiThreadManager)
    {
        _instance = this;
        this.gameController = gameController;
        _settings = settings;

        entityCache = new Dictionary<uint, Entity>(1000);
        gameController.Area.OnAreaChange += AreaChanged;
        EntitiesVersion = 0;

        updateEntity =
            new Coroutine(RefreshState, new WaitTime(coroutineTimeWait), null, "Update Entity")
                {Priority = CoroutinePriority.High, SyncModWork = true};

        var collectEntitiesDebug = new DebugInformation("Collect Entities");

        entityCollectSettingsContainer = new EntityCollectSettingsContainer();
        entityCollectSettingsContainer.Simple = Simple;
        entityCollectSettingsContainer.KeyForDelete = keysForDelete;
        entityCollectSettingsContainer.EntityCache = entityCache;
        entityCollectSettingsContainer.MultiThreadManager = multiThreadManager;
        entityCollectSettingsContainer.ParseServer = () => settings.ParseServerEntities;
        entityCollectSettingsContainer.ParseEntitiesInMultiThread = () => settings.ParseEntitiesInMultiThread;
        entityCollectSettingsContainer.EntitiesCount = () => gameController.IngameState.Data.EntitiesCount;
        entityCollectSettingsContainer.EntitiesVersion = EntitiesVersion;
        entityCollectSettingsContainer.CollectEntitiesInParallelWhenMoreThanX = settings.CollectEntitiesInParallelWhenMoreThanX;
        entityCollectSettingsContainer.DebugInformation = collectEntitiesDebug;

        IEnumerator Test()
        {
            while (true)
            {
                yield return gameController.IngameState.Data.EntityList.CollectEntities(entityCollectSettingsContainer);
                yield return new WaitTime(1000 / settings.EntitiesUpdate);
                parallelUpdateDictionary.UpdateTicks((uint) (parallelUpdateDictionary.Ticks + 1));
            }
        }

        parallelUpdateDictionary = new Coroutine(Test(), null, "Collect entites") {SyncModWork = true};
        UpdateCondition(1000 / settings.EntitiesUpdate);

        settings.EntitiesUpdate.OnValueChanged += (sender, i) => { UpdateCondition(1000 / i); };

        var enumValues = typeof(EntityType).GetEnumValues();
        ValidEntitiesByType = new Dictionary<EntityType, List<Entity>>(enumValues.Length);

        foreach (EntityType enumValue in enumValues)
        {
            ValidEntitiesByType[enumValue] = new List<Entity>(8);
        }

        PlayerUpdate += (sender, entity) => Entity.Player = entity;
    }

    /// <summary>All currently cached entities.</summary>
    public ICollection<Entity> Entities => entityCache.Values;

    /// <summary>The current entity version counter.</summary>
    public uint EntitiesVersion { get; }

    /// <summary>The local player entity.</summary>
    public Entity Player { get; private set; }

    /// <summary>Entities that passed validity checks during the last refresh.</summary>
    public List<Entity> OnlyValidEntities { get; } = new List<Entity>(500);

    /// <summary>Entities that failed validity checks during the last refresh.</summary>
    public List<Entity> NotOnlyValidEntities { get; } = new List<Entity>(500);
    /// <summary>Invalid entities keyed by id from the last refresh.</summary>
    public Dictionary<uint, Entity> NotValidDict { get; } = new Dictionary<uint, Entity>(500);

    /// <summary>Valid entities grouped by their <see cref="EntityType"/>.</summary>
    public Dictionary<EntityType, List<Entity>> ValidEntitiesByType { get; }

    /// <summary>Starts the entity-update and parallel-collection coroutines.</summary>
    public void StartWork()
    {
        Core.MainRunner.Run(updateEntity);
        Core.ParallelRunner.Run(parallelUpdateDictionary);
    }

    private void UpdateCondition(int coroutineTimeWait = 50)
    {
        parallelUpdateDictionary.UpdateCondtion(new WaitTime(coroutineTimeWait));
        updateEntity.UpdateCondtion(new WaitTime(coroutineTimeWait));
    }

    /// <summary>Raised when a gameplay-relevant entity is added.</summary>
    public event Action<Entity> EntityAdded;

    /// <summary>Raised when any entity is added.</summary>
    public event Action<Entity> EntityAddedAny;

    /// <summary>Raised when an entity is ignored.</summary>
    public event Action<Entity> EntityIgnored;

    /// <summary>Raised when an entity is removed.</summary>
    public event Action<Entity> EntityRemoved;

    private void AreaChanged(AreaInstance area)
    {
        try
        {
            entityCollectSettingsContainer.Break = true;
            var dataLocalPlayer = gameController.Game.IngameState.Data.LocalPlayer;

            if (Player == null)
            {
                if (dataLocalPlayer.Path.StartsWith("Meta"))
                {
                    Player = dataLocalPlayer;
                    Player.IsValid = true;
                    PlayerUpdate?.Invoke(this, Player);
                }
            }
            else
            {
                if (Player.Address != dataLocalPlayer.Address)
                {
                    if (dataLocalPlayer.Path.StartsWith("Meta"))
                    {
                        Player = dataLocalPlayer;
                        Player.IsValid = true;
                        PlayerUpdate?.Invoke(this, Player);
                    }
                }
            }

            entityCache.Clear();
            OnlyValidEntities.Clear();
            NotOnlyValidEntities.Clear();

            foreach (var e in ValidEntitiesByType)
            {
                e.Value.Clear();
            }
        }
        catch (Exception e)
        {
            DebugWindow.LogError($"{nameof(EntityListWrapper)} -> {e}");
        }
    }

    private void UpdateEntityCollections()
    {
        OnlyValidEntities.Clear();
        NotOnlyValidEntities.Clear();
        NotValidDict.Clear();

        foreach (var e in ValidEntitiesByType)
        {
            e.Value.Clear();
        }

        while (keysForDelete.Count > 0)
        {
            var key = keysForDelete.Dequeue();

            if (entityCache.TryGetValue(key, out var entity))
            {
                EntityRemoved?.Invoke(entity);
                entityCache.Remove(key);
            }
        }

        foreach (var entity in entityCache)
        {
            var entityValue = entity.Value;

            if (entityValue.IsValid)
            {
                OnlyValidEntities.Add(entityValue);
                ValidEntitiesByType[entityValue.Type].Add(entityValue);
            }
            else
            {
                NotOnlyValidEntities.Add(entityValue);
                NotValidDict[entityValue.Id] = entityValue;
            }
        }
    }

    /// <summary>
    /// Drains the freshly-collected entity stack into the cache (raising add events) and rebuilds
    /// the valid/invalid lookup collections.
    /// </summary>
    public void RefreshState()
    {
        if (gameController.Area.CurrentArea == null || entityCollectSettingsContainer.NeedUpdate ||
            !Player.IsValid)
            return;

        while (Simple.Count > 0)
        {
            var entity = Simple.Pop();

            if (entity == null)
            {
                DebugWindow.LogError($"{nameof(EntityListWrapper)}.{nameof(RefreshState)} entity is null. (Very strange).");
                continue;
            }

            var entityId = entity.Id;
            if (entityCache.TryGetValue(entityId, out _)) continue;

            if (entityId >= int.MaxValue && !_settings.ParseServerEntities)
                continue;

            if (entity.Type == EntityType.Error)
                continue;

            if (entity.League == LeagueType.Legion)
            {
                if (entity.Stats == null)
                    continue;
            }

            EntityAddedAny?.Invoke(entity);
            if ((int) entity.Type >= 100) EntityAdded?.Invoke(entity);

            entityCache[entityId] = entity;
        }

        UpdateEntityCollections();
        entityCollectSettingsContainer.NeedUpdate = true;
    }

    /// <summary>Raised when the local player entity changes.</summary>
    public event EventHandler<Entity> PlayerUpdate;

    /// <summary>Looks up a cached entity by id, or null if not present.</summary>
    public static Entity GetEntityById(uint id)
    {
        return _instance.entityCache.TryGetValue(id, out var result) ? result : null;
    }

    /// <summary>Resolves the on-screen label text for the given entity by walking the entity-label map.</summary>
    public string GetLabelForEntity(Entity entity)
    {
        var hashSet = new HashSet<long>();
        var entityLabelMap = gameController.Game.IngameState.EntityLabelMap;
        var num = entityLabelMap;

        while (true)
        {
            hashSet.Add(num);
            if (gameController.Memory.Read<long>(num + 0x10) == entity.Address) break;

            num = gameController.Memory.Read<long>(num);
            if (hashSet.Contains(num) || num == 0 || num == -1) return null;
        }

        return gameController.Game.ReadObject<EntityLabel>(num + 0x18).Text;
    }
}
