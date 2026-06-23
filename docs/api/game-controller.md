# GameController — the root API object

> `GameController` is the single object every plugin receives; from it you reach the game state, entities, files, memory, window, and settings.

See the [API reference index](README.md) for the full plugin-author reference.

## How a plugin gets it

Every plugin derives from `BaseSettingsPlugin<TSettings>`, and the host calls `SetApi(...)` once during load, which stores the controller on the public `GameController` property:

```csharp
// Core/BaseSettingsPlugin.cs
public GameController GameController { get; private set; }

public void SetApi(GameController gameController, Graphics graphics, PluginManager pluginManager)
{
    GameController = gameController;
    // ...
}
```

So inside any plugin method you simply read `GameController.<member>`. The type lives in namespace `ExileCore` (`ExileCore.GameController`).

`GameController` is a façade: it owns the per-frame `Tick()` that refreshes foreground/area/loading state, but plugins almost never call `Tick()` — they read the members below.

## Members

| Member | Type | What it unlocks | Deeper doc |
| --- | --- | --- | --- |
| `Game` | `TheGame` | Root game state; `Game.IngameState`, `Game.Files`, `Game.InGame`, `Game.IsLoading`, raw memory readers (`ReadObject<T>`). | [ingame-state.md](ingame-state.md) |
| `IngameState` | `IngameState` | Shortcut for `Game.IngameState` — UI elements, world `Data`, `Camera`. | [ingame-state.md](ingame-state.md) |
| `Area` | `AreaController` | Current `AreaInstance` (`Area.CurrentArea`) and the `OnAreaChange` event. | this doc (see below) |
| `Window` | `GameWindow` | Window rectangle/handle for screen-space math (`GetWindowRectangle()`), input anchoring. | [input.md](input.md) |
| `Files` | `FilesContainer` | Parsed game data files (`.dat` tables, stats, mods). Same object as `Game.Files`. | [files-in-memory.md](files-in-memory.md) |
| `Player` | `Entity` | The local player entity; read `Player.Pos`/`GridPos`, `Player.GetComponent<T>()`. Backed by `EntityListWrapper.Player`. | [entities.md](entities.md) |
| `Entities` | `ICollection<Entity>` | All currently cached entities (`EntityListWrapper.Entities`). | [entities.md](entities.md) |
| `EntityListWrapper` | `EntityListWrapper` | Filtered entity views and add/remove events (see below). | [entities.md](entities.md) |
| `Memory` | `IMemory` | Raw process-memory reader: `Read<T>(addr, …offsets)`, `ReadString`/`ReadStringU`, `ReadStructsArray<T>`, `ReadMem`/`ReadBytes`. | [memory.md](memory.md) |
| `Cache` | `Cache` | Shared memory-object cache (settable). | [caching.md](caching.md) |
| `SoundController` | `SoundController` | Play sounds/alerts. | — |
| `Settings` | `SettingsContainer` | Load/save plugin settings (`Settings.LoadSettings(this)`, `SaveSettings(this)`). | — |
| `MultiThreadManager` | `MultiThreadManager` | Worker thread pool for parallel work. | — |
| `DeltaTime` | `double` | Most recent frame delta time. | — |
| `ElapsedMs` | `long` | Milliseconds since the controller was created. | — |
| `InGame` | `bool` | True when in-game (not menu/loading). | — |
| `IsLoading` | `bool` | True while on a loading screen. | — |
| `IsForeGroundCache` | `bool` | Cached foreground state of the game/overlay window (settable). | — |
| `LeftPanel` | `PluginPanel` | Drawing anchor at the map's left corner (see below). | this doc (see below) |
| `UnderPanel` | `PluginPanel` | Drawing anchor under the map. | this doc (see below) |
| `PluginBridge` | `PluginBridge` | Share named delegates/objects between plugins. | this doc (see below) |
| `Debug` | `Dictionary<string, object>` | Ad-hoc debug values shared between components. | — |
| `Initialized` | `bool` | True when the controller finished initializing successfully. | — |
| `GetLeftCornerMap()` | `Vector2` | Screen position of the map's left corner. | this doc (see below) |
| `eIsForegroundChanged` | `static event Action<bool>` | Raised when the game/overlay foreground state changes. | this doc (see below) |

Notes:

- In this source the geometry types are `SharpDX`: `GetLeftCornerMap()`, `LeftPanel.StartDrawPoint`/`Margin`, `Entity.Pos`/`GridPos` and `Camera.WorldToScreen` all use `SharpDX.Vector2`/`Vector3` (the panel/controller/entity files all `using SharpDX`). `Color` is likewise `SharpDX`.
- `InGame`, `IsLoading` and `IsForeGroundCache` are refreshed each frame inside `Tick()`; treat them as read-only frame state (`IsForeGroundCache` is technically settable but the host owns it).

## Reaching the common targets

### Player and entities

`Player` and `Entities` are thin shortcuts onto `EntityListWrapper`:

```csharp
// Core/GameController.cs
public Entity Player => EntityListWrapper.Player;
public ICollection<Entity> Entities => EntityListWrapper.Entities;
```

`EntityListWrapper` also exposes filtered views plugins rely on:

| Member | Type | Use |
| --- | --- | --- |
| `Player` | `Entity` | Local player. |
| `Entities` | `ICollection<Entity>` | All cached entities. |
| `OnlyValidEntities` | `List<Entity>` | Entities that passed validity checks last refresh. |
| `ValidEntitiesByType` | `Dictionary<EntityType, List<Entity>>` | Valid entities grouped by `EntityType` (e.g. `Monster`, `Chest`). |
| `EntityAdded` / `EntityRemoved` / `EntityAddedAny` / `EntityIgnored` | `event Action<Entity>` | React to entities appearing/disappearing. |
| `PlayerUpdate` | `event EventHandler<Entity>` | The local player entity changed (e.g. on area change). |
| `GetEntityById(uint id)` | `static Entity` | Look up a cached entity by id. |

PickItV2 reads both the player and a typed entity list straight off `GameController` (adapted to this source's `Entity.Pos`, a `SharpDX.Vector3`):

```csharp
// adapted from exApiTools/PickItV2 (PickIt.cs)
var playerPos = GameController.Player.Pos;   // SharpDX.Vector3

var monsters = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster];
bool enemyClose = monsters.Any(x =>
    Vector3.Distance(playerPos, x.Pos) < Settings.PickupRange);
```

### IngameState and Files

`IngameState` and `Files` are surfaced from `Game`:

```csharp
// Core/GameController.cs
public IngameState IngameState => Game.IngameState;   // shortcut for Game.IngameState
public FilesContainer Files { get; }                  // == Game.Files
```

Radar uses `GameController.IngameState.Data` (world data) and `GameController.IngameState.Camera` (screen projection):

```csharp
// adapted from instantsc/Radar (Radar.cs)
var localPlayer = GameController.IngameState.Data.LocalPlayer;            // IngameData.LocalPlayer
var screen = GameController.IngameState.Camera.WorldToScreen(worldPos);  // SharpDX.Vector2
```

### Area

`Area` is an `AreaController` that tracks the current zone and fires when it changes:

| Member | Type | Use |
| --- | --- | --- |
| `CurrentArea` | `AreaInstance` | The zone the player is in: `Name`, `Hash`, `RealLevel`, `DisplayName`, and `Area` (an `AreaTemplate` with `RawName`/`Name`). |
| `OnAreaChange` | `event Action<AreaInstance>` | Subscribe to react to zone transitions. |
| `ForceRefreshArea(bool)` / `RefreshState()` | `void` / `bool` | Rebuild the current area (the host drives these from `Tick()`). |

```csharp
// adapted from instantsc/Radar (Radar.cs)
var areaName = GameController.Area.CurrentArea.Area.RawName;   // AreaTemplate.RawName
var areaLevel = GameController.Area.CurrentArea.RealLevel;
```

### Window and Memory

```csharp
// adapted from instantsc/Radar (Radar.cs)
var rect = GameController.Window.GetWindowRectangle();   // GameWindow -> SharpDX.RectangleF
var hp = GameController.Memory.Read<int>(someAddress);   // IMemory.Read<T>
var name = GameController.Memory.ReadStringU(strAddress); // IMemory string read
```

### PluginBridge — sharing between plugins

`PluginBridge` is a small named registry so one plugin can publish a delegate/object and another can consume it:

```csharp
// Core/GameController.cs (nested PluginBridge)
public T GetMethod<T>(string name) where T : class;   // returns null if missing
public void SaveMethod(string name, object method);
```

Radar publishes its pathfinding routine for other plugins:

```csharp
// adapted from instantsc/Radar (Radar.cs) — producer
GameController.PluginBridge.SaveMethod("Radar.LookForRoute", (Func<...>)LookForRoute);

// consumer side
var lookForRoute = GameController.PluginBridge.GetMethod<Func<...>>("Radar.LookForRoute");
```

### Drawing panels — LeftPanel / UnderPanel

`LeftPanel` and `UnderPanel` are `PluginPanel` anchors positioned each frame at the map's left corner and under the map. A plugin registers a "do I want to draw?" predicate via `WantUse`, then draws starting at `StartDrawPoint`:

| `PluginPanel` member | Type | Use |
| --- | --- | --- |
| `StartDrawPoint` | `Vector2` | Where panel drawing begins (host updates it each frame). |
| `Margin` | `Vector2` | Margin around panel content. |
| `Used` | `bool` | True when any registered predicate currently returns true. |
| `WantUse(Func<bool>)` | `void` | Register a predicate that signals the plugin wants the panel. |

`GetLeftCornerMap()` returns the raw `Vector2` left-corner position (the value `LeftPanel.StartDrawPoint` is fed from); plugins that draw freehand near the minimap can call it directly.

### eIsForegroundChanged event

`eIsForegroundChanged` is a `static event Action<bool>` raised when the game/overlay foreground state flips (the argument is the new foreground state). Internally the host uses it to pause/resume coroutines; plugins can subscribe to suspend work while the game is not focused.

## Quick usage example

```csharp
public override void Render()
{
    // Bail when not in a playable state.
    if (!GameController.InGame || GameController.IsLoading)
        return;

    // Player position (SharpDX.Vector3 in this source).
    var playerPos = GameController.Player.Pos;

    // Get a typed entity list and draw distances to nearby monsters.
    foreach (var monster in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
    {
        var dist = Vector3.Distance(playerPos, monster.Pos);
        // ... draw via Graphics ...
    }
}
```

## Source

- `Core/GameController.cs` (`GameController`, nested `PluginBridge`)
- `Core/AreaController.cs`
- `Core/PluginPanel.cs`
- `Core/EntityListWrapper.cs`
- `Core/BaseSettingsPlugin.cs` (`SetApi`, `GameController` property)
