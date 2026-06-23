# Caching

> Cache types that let plugins read an expensive value at most once per a chosen lifetime window. See the [API reference index](README.md).

Every property on a memory-backed object (an [`Entity`](entities.md), a component, a UI element) is read live from the Path of Exile process with `ReadProcessMemory`. That copy is comparatively expensive, and a HUD touches the same values many times per frame. The cache types in `ExileCore.Shared.Cache` solve this: each one wraps a value-producing `Func<T>` and only re-invokes it when its lifetime policy says the cached copy is stale. Between refreshes, reads are free.

This page documents the public cache types and the two top-level cache objects (`GameController.Cache` / `RemoteMemoryObject.Cache`). Namespace: `ExileCore.Shared.Cache`.

## `CachedValue<T>` — the base contract

All policy caches (`FrameCache`, `TimeCache`, etc.) derive from `CachedValue<T>`. The base class holds the factory and the value; each subclass only implements `Update`, which decides whether to recompute.

```csharp
public abstract class CachedValue<T> : CachedValue
{
    protected CachedValue(Func<T> func);          // null func throws ArgumentNullException

    public T Value { get; }                        // lazy read; recomputes if the policy is stale
    public T RealValue { get; }                    // always invokes func, bypassing the cache
    public void ForceUpdate();                      // make the next Value read recompute
    public event CacheUpdateEvent OnUpdate;         // raised on every refresh, with the new value

    protected abstract bool Update(bool force);     // policy hook; true => recompute
}
```

Reading `Value`:

- If the policy (or a pending `ForceUpdate`) says recompute, it calls `func()`, stores the result, fires `OnUpdate`, and returns it.
- Otherwise it returns the last stored value — except before the very first refresh, where it falls through to `func()` directly each time until the policy first ticks.

The non-generic base `CachedValue` exposes static bookkeeping shared by all caches: `TotalCount` and `LifeCount` (instances ever created / currently alive) and `Latency` (a `float`, default `25`, in milliseconds) which the engine keeps in sync with the game's latency and which `LatancyCache` reads. You set the policy by choosing which subclass to instantiate; you never override `Update` yourself in plugin code.

## Cache types

| Type | Lifetime / invalidation | Use when |
| --- | --- | --- |
| `FrameCache<T>` | Once per rendered frame (`Core.FramesCount` changes) | A value read multiple times within a frame but recomputed every frame (most component reads). |
| `FramesCache<T>` | Once every `waitFrames` frames | Per-frame is too frequent; refresh every N frames. |
| `AreaCache<T>` | Until the area instance changes (`AreaInstance.CurrentHash`) | Value is constant for the current zone. |
| `TimeCache<T>` | At most once per `waitMilliseconds` | Throttle an expensive scan to a fixed time budget. |
| `LatancyCache<T>` | At most once per current game latency (floored at `minLatency`) | Refresh no faster than data can actually change over the network. |
| `ConditionalCache<T>` | Whenever a supplied `Func<bool>` returns true | Invalidation depends on custom state. |
| `StaticValueCache<T>` | Computed exactly once, then never again | Value never changes for the object's lifetime. |
| `ValidCache<T>` | Whenever the bound `Entity.IsValid` is true | Value tied to an entity that may become invalid. |

### `FrameCache<T>`

```csharp
public FrameCache(Func<T> func);
```

Recomputes once per rendered frame, keyed on the engine's global frame counter `Core.FramesCount`. This is the workhorse the engine uses for almost every component struct read.

### `FramesCache<T>`

```csharp
public FramesCache(Func<T> func, uint waitFrames = 1); // derives from FrameCache<T>
```

Like `FrameCache` but only refreshes after `waitFrames` frames have elapsed. Use it for values that are still per-frame-ish but cheap to let drift, e.g. chest state.

### `AreaCache<T>`

```csharp
public AreaCache(Func<T> func);
```

Recomputes whenever `AreaInstance.CurrentHash` changes (the engine writes the current area hash there each tick). Ideal for anything constant within a zone.

### `TimeCache<T>`

```csharp
public TimeCache(Func<T> func, long waitMilliseconds);
public void NewTime(long newTime); // change the interval at runtime
```

Refreshes at most once per `waitMilliseconds`. The most common cache for plugin-level throttling of an expensive computation.

### `LatancyCache<T>`

```csharp
public LatancyCache(Func<T> func, int minLatency = 10);
```

Note the (original) spelling `LatancyCache`. Refreshes no more often than `CachedValue.Latency` (the live game latency) allows, but never faster than `minLatency` ms. Use it for state that genuinely cannot change faster than a server round-trip.

### `ConditionalCache<T>`

```csharp
public ConditionalCache(Func<T> func, Func<bool> cond);
```

Recomputes whenever `cond()` returns true.

### `StaticValueCache<T>`

```csharp
public StaticValueCache(Func<T> func);
```

Computes the value on the first read and never again. Use for values that are immutable once known (e.g. the `WorldArea` behind a `Map` component).

### `ValidCache<T>`

```csharp
public ValidCache(Entity entity, Func<T> func);
```

Refreshes while the bound `entity.IsValid` is true. Convenience extension: `entity.ValidCache(func)` (from `ExileCore.Shared.Helpers.Extensions`). Used internally for entity buff lists, which must drop when the entity goes invalid.

## The static memory caches: `Cache`

Separate from the per-value caches above is the single `Cache` object that backs raw memory deduplication. It is reachable as `GameController.Cache` and, statically, as `RemoteMemoryObject.Cache` (both refer to the same instance the `GameController` constructs).

```csharp
public class Cache
{
    public Cache();              // calls CreateCache()
    public void CreateCache();   // (re)allocates all the static caches below
    public void TryClearCache(); // trims every static cache

    public IStaticCache<RemoteMemoryObject> StaticCacheElements   { get; }
    public IStaticCache<RemoteMemoryObject> StaticCacheComponents { get; }
    public IStaticCache<RemoteMemoryObject> StaticEntityCache     { get; }
    public IStaticCache<RemoteMemoryObject> StaticEntityListCache { get; }
    public IStaticCache<RemoteMemoryObject> StaticServerEntityCache { get; }
    public IStaticCache<string> StringCache                       { get; }
}
```

Each entry is a `StaticCache<T>` implementing `IStaticCache<T>`: an address-keyed `MemoryCache` with a sliding-expiration policy plus hit/miss statistics (`ReadCache`, `ReadMemory`, `Coeff`, `CoeffString`). Its core method is:

```csharp
T Read(string addr, Func<T> func); // return cached value for addr, else compute, store, and return
```

`StringCache` (the `IStaticCache<string>` above) is the one plugins most often see indirectly: string properties such as `Render.Name`, `MinimapIcon.Name`, and mod names are read through `Cache.StringCache.Read(key, () => M.ReadStringU(...))`, so a given string is decoded from memory only once.

`TryClearCache()` simply calls `UpdateCache()` (a `Trim`) on every static cache. The `GameController` invokes it on area change — roughly every third frame it checks for an area refresh and, if one happened, trims the static caches:

```csharp
// GameController.Tick()
if (Core.FramesCount % 3 == 0 && Area.RefreshState())
    debClearCache.TickAction(() => { RemoteMemoryObject.Cache.TryClearCache(); });
```

The same `Tick` also keeps `CachedValue.Latency` synced (`if (InGame) CachedValue.Latency = Game.IngameState.CurLatency;`), which is what drives `LatancyCache`. The per-frame `FrameCache`/`FramesCache` caches need no explicit clearing — they invalidate themselves by comparing against `Core.FramesCount`.

> Note: there is also a separate `StaticStringCache` class (an `IntPtr`-keyed dictionary with time-based eviction). It is not part of the `Cache` object's surface; the active string cache used by the engine is the `IStaticCache<string> StringCache` above.

## How the engine uses caches

The pattern throughout [`PoEMemory`](memory.md) is: a memory object wraps its raw offset struct in a `FrameCache` so all of its properties share one read per frame. Examples:

- `Element`, `Actor`, `Life`, `Mods`, `Render`, `Targetable`, `Pathfinding`, etc. each hold a `FrameCache<...Offsets>` over `M.Read<...>(Address)`.
- `Chest` uses a `FramesCache<...>(..., 3)` (chest state every 3 frames).
- `Element.GetClientRectCache` and `Entity.IsHostile` use a `TimeCache` (200 ms / 100 ms).
- `Entity` uses a `LatancyCache<bool>` for its hidden-monster check and a `ValidCache<List<Buff>>` for buffs.
- `Map` uses a `StaticValueCache<WorldArea>` for its world area.

So most of the savings happen for free: when you read `entity.GetComponent<Life>().CurHP` several times a frame, you pay one memory read.

## Example: throttling your own work

When *your* plugin does something expensive (a scan over the entity list, an item filter, building a label list), wrap it in a `TimeCache` (time budget) or `FrameCache` (once per frame) and read `.Value` from `Render`/`Tick`. This is exactly how the reference plugins do it.

From PickItV2 (`PickIt.cs`), verbatim:

```csharp
public PickIt()
{
    Name = "PickIt With Linq";
    _inventorySlotsWithItemIds = new FrameCache<int[,]>(() => GetContainer2DArrayWithItemIds(_inventoryItems));
    _chestLabels  = new TimeCache<List<LabelOnGround>>(UpdateChestList, 200);
    _portalLabel  = new TimeCache<LabelOnGround>(
        () => GetLabel(@"^Metadata/(MiscellaneousObjects|Effects/Microtransactions)/.*Portal"), 200);
}
```

`UpdateChestList` is a full scan of on-ground labels; behind the `TimeCache(200)` it runs at most five times a second no matter how often `_chestLabels.Value` is read. A minimal version in your own [plugin](plugins.md):

```csharp
public class MyPlugin : BaseSettingsPlugin<MySettings>
{
    private CachedValue<List<Entity>> _nearbyMonsters;

    public override bool Initialise()
    {
        // Recompute at most every 250 ms, no matter how often Value is read.
        _nearbyMonsters = new TimeCache<List<Entity>>(ScanMonsters, 250);
        return true;
    }

    private List<Entity> ScanMonsters() =>
        GameController.Entities
            .Where(e => e.Type == EntityType.Monster && e.IsAlive &&
                        e.DistancePlayer < 100)
            .ToList();

    public override void Render()
    {
        foreach (var monster in _nearbyMonsters.Value) // cheap between refreshes
            Graphics.DrawText(monster.RenderName, GameController.IngameState.Camera
                .WorldToScreen(monster.Pos), Color.Red);
    }
}
```

Swap `TimeCache` for `FrameCache<List<Entity>>(ScanMonsters)` if you want exactly one scan per rendered frame, or `FramesCache<...>(ScanMonsters, 3)` for every third frame. Call `ForceUpdate()` when external state (e.g. a settings change) must invalidate the cached result immediately.

## Source

- `Core/Shared/Cache/CachedValue.cs` — base contract (`Value`, `RealValue`, `ForceUpdate`, `OnUpdate`, `Latency`).
- `Core/Shared/Cache/FrameCache.cs`, `FramesCache.cs`, `AreaCache.cs`, `TimeCache.cs`, `LatancyCache.cs`, `ConditionalCache.cs`, `StaticValueCache.cs`, `ValidCache.cs` — the policy caches.
- `Core/Shared/Cache/Cache.cs`, `StaticCache.cs`, `StaticStringCache.cs`, `Core/Shared/Interfaces/IStaticCache.cs` — the static memory caches.
- `Core/PoEMemory/RemoteMemoryObject.cs` — `static Cache Cache` accessor.
- `Core/GameController.cs` — `Cache` instance, `TryClearCache` on area change, `Latency` sync, `TimeCache` map-corner examples.
- `Core/Shared/Helpers/Extensions.cs` — `entity.ValidCache(func)` extension.
- Real-world usage: exApiTools/PickItV2 (`PickIt.cs`), instantsc/Radar and other reference plugins.
