# Plugin lifecycle & BaseSettingsPlugin

> Part of the plugin-author API reference. See the [API reference index](README.md).

Every ExileCore plugin implements `ExileCore.Shared.Interfaces.IPlugin`. In practice you never
implement that interface directly: you derive from `ExileCore.BaseSettingsPlugin<TSettings>`, which
implements `IPlugin`, wires settings load/save to disk, builds the settings menu, and exposes the
core APIs (`GameController`, `Graphics`). You override only the virtual hooks you need.

The engine discovers, instantiates and drives plugins through
`ExileCore.Shared.PluginManager` and `ExileCore.Shared.PluginWrapper`. Understanding the call order
below is the key to writing a correct plugin.

## `IPlugin`

`namespace ExileCore.Shared.Interfaces` — `IPlugin : IDisposable`. The complete contract:

```csharp
public interface IPlugin : IDisposable
{
    bool Initialized { get; set; }
    ISettings _Settings { get; }
    bool CanUseMultiThreading { get; }
    bool Force { get; }
    string DirectoryName { get; set; }
    string DirectoryFullName { get; set; }
    string InternalName { get; }
    string Name { get; }
    string Description { get; }
    int Order { get; }
    void DrawSettings();
    void OnLoad();
    void OnUnload();
    bool Initialise();
    Job Tick();
    void Render();
    void OnClose();
    void SetApi(GameController gameController, Graphics graphics, PluginManager pluginManager);
    void OnPluginSelectedInMenu();
    void EntityAdded(Entity entity);
    void EntityRemoved(Entity entity);
    void EntityAddedAny(Entity entity);
    void EntityIgnored(Entity entity);
    void AreaChange(AreaInstance area);
    void ReceiveEvent(string eventId, object args);
    void _LoadSettings();
    void _SaveSettings();
    void OnPluginDestroyForHotReload();
}
```

| Member | Meaning |
| --- | --- |
| `bool Initialized { get; set; }` | Set to the result of `Initialise()` by the engine. The plugin is only ticked/rendered while enabled; this flag tracks whether one-time init has run. Don't set it yourself. |
| `ISettings _Settings { get; }` | The plugin's settings object (the disk-backed config). `_Settings.Enable` (a `ToggleNode`) is the on/off switch the engine checks before ticking. See [settings.md](settings.md). |
| `bool CanUseMultiThreading { get; }` | When true, the plugin's per-frame work can run on a worker thread and `EntityAdded` may be dispatched in parallel batches. Plugins are ordered with multi-threadable ones first. |
| `bool Force { get; }` | When true the plugin keeps ticking/rendering even when not in-game (`GameController.InGame == false`). |
| `string DirectoryName { get; set; }` | The plugin folder's name. Assigned by `PluginManager` at load time. |
| `string DirectoryFullName { get; set; }` | Absolute path to the plugin folder. Used to locate the `textures/` atlas (see [GetAtlasTexture](#getatlastexture)) and other plugin assets. Assigned by the engine. |
| `string InternalName { get; }` | Stable identifier — `BaseSettingsPlugin` sets it to `GetType().Namespace`. Used for settings file naming. |
| `string Name { get; }` | Display name shown in the menu. Defaults to `InternalName`. |
| `string Description { get; }` | Optional plugin description. |
| `int Order { get; }` | Load/run priority. Plugins are sorted by `Order`, then multi-threadable first, then `Name`. Lower runs earlier. |
| `void DrawSettings()` | Draws the plugin's settings UI. The base class draws all registered `Drawers`. |
| `void OnLoad()` | Called once right after construction + settings load, before `Initialise()`. One-time setup. |
| `void OnUnload()` | Called on shutdown/reload teardown. |
| `bool Initialise()` | Main init hook. **Return `false` to abort** — the plugin is disabled. Return `true` on success. |
| `Job Tick()` | Per-frame logic hook. Return a [`Job`](#tick-returning-a-job) to run work off the main thread, or `null` for none. |
| `void Render()` | Per-frame draw hook. Use `Graphics` here. See [game-controller.md](game-controller.md). |
| `void OnClose()` | Called when the plugin closes; the base class saves settings here. |
| `void SetApi(GameController, Graphics, PluginManager)` | Injects the core APIs. Called by the engine before settings load; the base class stores them. |
| `void OnPluginSelectedInMenu()` | Called when the plugin is selected in the menu (marked TODO in core; safe to override). |
| `void EntityAdded(Entity entity)` | A gameplay-relevant entity entered the world. |
| `void EntityRemoved(Entity entity)` | An entity left the world / was removed. |
| `void EntityAddedAny(Entity entity)` | Any entity was added, including non-gameplay ones. |
| `void EntityIgnored(Entity entity)` | An entity was ignored (filtered out of the active list). |
| `void AreaChange(AreaInstance area)` | The player changed area / zoned. See [entities.md](entities.md). |
| `void ReceiveEvent(string eventId, object args)` | Receives an inter-plugin event published by another plugin (see [PublishEvent/ReceiveEvent](#inter-plugin-events)). |
| `void _LoadSettings()` | Loads settings from disk and rebuilds the menu drawers. Implemented by the base class. |
| `void _SaveSettings()` | Persists settings to disk. Implemented by the base class. |
| `void OnPluginDestroyForHotReload()` | Called on the old instance just before it is replaced during a [hot reload](#hot-reload). |

Entity types are `ExileCore.PoEMemory.MemoryObjects.Entity` and `AreaInstance`; `Job` lives in the
root `ExileCore` namespace.

## `ISettings`

`namespace ExileCore.Shared.Interfaces`. Every `TSettings` must implement this:

```csharp
public interface ISettings
{
    ToggleNode Enable { get; set; }
}
```

`Enable` is the master on/off switch (`ExileCore.Shared.Nodes.ToggleNode`). The engine only ticks
and renders a plugin while `_Settings.Enable` is true, and toggling it on the first time triggers
`Initialise()`. Settings node types and the menu are covered in [settings.md](settings.md).

## `BaseSettingsPlugin<TSettings>`

`namespace ExileCore` — `abstract class BaseSettingsPlugin<TSettings> : IPlugin where TSettings : ISettings, new()`.

The `new()` constraint lets the base class construct a default settings object when no file exists.
What the base class provides:

| Member | Type | Notes |
| --- | --- | --- |
| `GameController GameController { get; }` | `ExileCore.GameController` | Game state, entities, area, window, memory, and `PluginBridge`. Set by `SetApi`. See [game-controller.md](game-controller.md). |
| `Graphics Graphics { get; }` | `ExileCore.Graphics` | The overlay draw API; use in `Render()`. |
| `TSettings Settings { get; }` | your settings type | Strongly-typed accessor over `_Settings`. |
| `ISettings _Settings { get; }` | `ISettings` | The raw settings object (also exposed via `IPlugin`). |
| `List<ISettingsHolder> Drawers { get; }` | drawer list | Built from `Settings` during `_LoadSettings`; rendered by the default `DrawSettings()`. |
| `void _LoadSettings()` | — | Loads/creates settings, deserializes JSON, then `SettingsParser.Parse(_Settings, Drawers)`. |
| `void _SaveSettings()` | — | Saves via `GameController.Settings.SaveSettings(this)`; throws if `_Settings` is null. |

It also sets `InternalName = GetType().Namespace` and `Name = InternalName` (unless you set `Name`)
in its constructor, and provides default no-op virtual implementations of all the lifecycle and
event hooks plus a default `DrawSettings()` that draws every registered drawer:

```csharp
public virtual void DrawSettings()
{
    foreach (var drawer in Drawers)
        drawer.Draw();
}
```

`Dispose()` (from `IDisposable`) calls `OnClose()`, and the default `OnClose()` calls
`_SaveSettings()`, so settings are persisted automatically on shutdown.

## Lifecycle & call order

`PluginManager` (in its constructor) and `PluginWrapper` drive the lifecycle. Verified order:

1. **Discovery** — `PluginManager` scans `Plugins/Compiled` and `Plugins/Source` (see
   [discovery](#discovery-compiled-vs-source-plugins)) and loads/compiles each assembly.
2. **Construction** — for each non-abstract type assignable to `IPlugin`,
   `Activator.CreateInstance(type)` runs the parameterless constructor. The engine then assigns
   `DirectoryName` and `DirectoryFullName`.
3. **`SetApi(gameController, graphics, pluginManager)`** — core APIs injected.
4. **`_LoadSettings()`** — settings loaded from disk (or defaults created) and `Drawers` built.
5. **`OnLoad()`** — one-time setup.
6. **`Initialise()`** — after all plugins are loaded and sorted by `Order`. Runs when the plugin is
   enabled (immediately if `_Settings.Enable` is already true, otherwise the first time the user
   toggles it on). **Returning `false` aborts**: `Initialized` stays false and the engine sets
   `Enable.Value = false`. On success, `AreaChange(CurrentArea)` is fired so the plugin sees the
   current zone.
7. **Per frame** (only while enabled and either in-game or `Force`): the engine calls
   **`Tick()`** for every enabled plugin first, runs any returned [`Job`s](#tick-returning-a-job)
   (on worker threads when threads are available), waits for them, then calls **`Render()`** for
   each plugin. So all ticks happen before any render in a frame.
8. **Event hooks** fire from game state, independent of the tick/render loop:
   `AreaChange` on zoning, and `EntityAdded` / `EntityRemoved` / `EntityAddedAny` /
   `EntityIgnored` as the entity list changes. These are only delivered to **enabled** plugins.
   `ReceiveEvent` is delivered when another plugin publishes (see below).
9. **Shutdown** — `PluginWrapper.Close()` calls `_SaveSettings()`, `OnClose()`, `OnUnload()`, then
   `Dispose()` (default `Dispose` → `OnClose` → `_SaveSettings`).

<a id="hot-reload"></a>
### Hot reload

When a plugin DLL changes on disk, `PluginManager.HotReloadDll` reloads the assembly and calls
`PluginWrapper.ReloadPlugin`, which: closes the old instance, calls `SetApi` / `_LoadSettings` on
the new instance, calls **`OnPluginDestroyForHotReload()`** on the *old* instance, swaps it in, then
runs `OnLoad()` + `Initialise()` and replays `EntityAdded` for current entities. Override
`OnPluginDestroyForHotReload()` to release resources that would otherwise leak across reloads.

## Tick returning a `Job`

`Tick()` returns a `Job` (`namespace ExileCore`) — a unit of work the engine can schedule on a
worker thread:

```csharp
public class Job
{
    public volatile bool IsCompleted;
    public volatile bool IsFailed;
    public volatile bool IsStarted;
    public Job(string name, Action work);
    public Action Work { get; set; }
    public string Name { get; set; }
    public double ElapsedMs { get; set; }
    // ...
}
```

Return `null` (the base-class default) to do nothing off-thread. If you return a `Job` and worker
threads are configured, the engine adds it to the `MultiThreadManager`, runs all plugins' jobs, then
spin-waits for completion before the render pass; with no worker threads it just runs `job.Work()`
inline. A job that runs too long (over the critical budget) gets its thread repaired and the plugin
is skipped for rendering that frame. Keep `Tick()` itself fast — it runs on the main thread before
the job is dispatched. For building jobs and other multi-threading helpers see [utilities.md](utilities.md).

Most plugins do their per-frame logic directly in `Tick()` and `return null`, as PickItV2 does:

```csharp
public override Job Tick()
{
    var playerInvCount = GameController?.Game?.IngameState?.Data?.ServerData?.PlayerInventories?.Count;
    if (playerInvCount is null or 0)
        return null;

    // ... read inventory, check hotkeys ...
    return null;
}
```

## Logging helpers

`BaseSettingsPlugin` wraps `DebugWindow` logging so messages appear on the debug overlay:

| Method | Effect |
| --- | --- |
| `void LogMsg(string msg)` | Logs an informational message. |
| `void LogError(string msg, float time = 1f)` | Logs an error for `time` seconds. |
| `void LogMessage(string msg, float time, Color clr)` | Logs with an explicit color. |
| `void LogMessage(string msg, float time = 1f)` | Logs with the default highlight color (`Color.GreenYellow`). |

`Color` is `SharpDX.Color`.

## Inter-plugin events

Two independent mechanisms let plugins talk to each other.

### PublishEvent / ReceiveEvent (broadcast)

`BaseSettingsPlugin.PublishEvent(string eventId, object args)` calls
`PluginManager.ReceivePluginEvent`, which invokes `ReceiveEvent(eventId, args)` on **every other
enabled plugin** (never the sender). Override `ReceiveEvent` to handle them:

```csharp
// Publisher
PublishEvent("MyPlugin.SomethingHappened", payloadObject);

// Subscriber
public override void ReceiveEvent(string eventId, object args)
{
    if (eventId == "MyPlugin.SomethingHappened" && args is MyPayload p)
        Handle(p);
}
```

### PluginBridge (named methods)

For typed, request-style sharing, use `GameController.PluginBridge`
(`ExileCore.PluginBridge`). A provider registers a delegate/object by name; a consumer fetches it:

```csharp
T GetMethod<T>(string name) where T : class;   // returns null if not registered
void SaveMethod(string name, object method);
```

Real usage from Radar (provider side, registered in `Initialise()`):

```csharp
GameController.PluginBridge.SaveMethod("Radar.ClusterTarget",
    (string targetName, int expectedCount) => ClusterTarget(targetName, expectedCount));
```

And a consumer fetches and invokes it (adapted from PickItV2 calling another plugin):

```csharp
var castSkill = GameController.PluginBridge.GetMethod<Action<Entity, uint>>("MagicInput.CastSkillWithTarget");
castSkill?.Invoke(item, 0x400);
```

`GetMethod<T>` returns `null` if no plugin has registered that name, so always null-check (load
order between plugins is not guaranteed). PickItV2 likewise registers
`SaveMethod("PickIt.ListItems", ...)` so other plugins can query its pickup list.

## GetAtlasTexture

`AtlasTexture GetAtlasTexture(string textureName)` loads a texture atlas from the plugin's
`textures/` folder (relative to `DirectoryFullName`) on first use and returns the named sub-texture.

How it works, exactly as written:

- Looks for a single `*.json` atlas config (the format exported by *Free texture packer*) in
  `<DirectoryFullName>/textures`. If none is found it logs an error and returns `null`.
- Derives the PNG path from the config name (`<name>.png` in the same folder). If the PNG is
  missing it logs an error and returns `null`.
- Builds an `AtlasTexturesProcessor`, calls `Graphics.InitImage(atlasTexturePath, false)`, caches
  it, and returns `_atlasTextures.GetTextureByName(textureName)`.

The returned `AtlasTexture` (`ExileCore.Shared.AtlasHelper.AtlasTexture`) exposes `TextureName`,
`AtlasFilePath`, `AtlasFileName`, and `TextureUV` (a `SharpDX.RectangleF`). Draw it with
`Graphics.DrawImage(AtlasTexture, RectangleF)`.

```csharp
var icon = GetAtlasTexture("my_icon");   // textures/atlas.json + textures/atlas.png
if (icon != null)
    Graphics.DrawImage(icon, new RectangleF(x, y, 32, 32));
```

## Discovery: compiled vs source plugins

`PluginManager` looks under the framework root in `Plugins/`:

- `Plugins/Compiled/<PluginName>/` — each folder must contain a `<PluginName>*.dll` (the DLL name
  must match the folder). Optionally a matching `.pdb` is loaded for debug symbols.
- `Plugins/Source/<PluginName>/` — source folders compiled at startup with the Roslyn compiler.
  A folder is **skipped** if it contains an `Errors.txt` (left by a previous failed compile) or is
  hidden. Compiled plugins take priority: a source folder with the same name as a compiled one is
  excluded.

For each loaded assembly the engine requires a type implementing `ISettings` and instantiates every
non-abstract `IPlugin` type it finds. Full compilation details are in
[../plugin-compiler.md](../plugin-compiler.md).

## Minimal plugin skeleton

A complete minimal plugin (one settings class, one plugin class):

```csharp
using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;

namespace MyPlugin;

public class MyPluginSettings : ISettings
{
    // Required by ISettings: the master on/off toggle.
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    // Add your own nodes here; they appear in the settings menu automatically.
    public ToggleNode ShowOverlay { get; set; } = new ToggleNode(true);
}

public class MyPlugin : BaseSettingsPlugin<MyPluginSettings>
{
    public override bool Initialise()
    {
        // One-time setup. Return false to abort loading.
        GameController.PluginBridge.SaveMethod("MyPlugin.Ping", () => "pong");
        return true;
    }

    public override Job Tick()
    {
        // Per-frame logic on the main thread. Return a Job to offload work, or null.
        return null;
    }

    public override void Render()
    {
        if (!Settings.ShowOverlay)
            return;

        Graphics.DrawText("MyPlugin running", new Vector2(100, 100), Color.White);
    }

    public override void AreaChange(AreaInstance area)
    {
        LogMessage($"Entered area: {area.Name}");
    }

    public override void EntityAdded(Entity entity)
    {
        // React to new entities here.
    }
}
```

The folder name and namespace should match the DLL name. Drop the compiled output in
`Plugins/Compiled/MyPlugin/MyPlugin.dll` (or the source in `Plugins/Source/MyPlugin/`).

## Source

- `Core/Shared/Interfaces/IPlugin.cs` — the `IPlugin` contract.
- `Core/Shared/Interfaces/ISettings.cs` — the `ISettings` contract.
- `Core/BaseSettingsPlugin.cs` — `BaseSettingsPlugin<TSettings>`, logging, `GetAtlasTexture`, `PublishEvent`.
- `Core/Shared/PluginManager.cs` — discovery, compile/load, event dispatch, hot reload.
- `Core/Shared/PluginWrapper.cs` — per-plugin lifecycle wiring (init, tick, render, close, reload).
- `Core/Core.cs` — the per-frame tick → job → render loop.
- `Core/MultiThreadManager.cs` — the `Job` type and worker-thread scheduling.
- `Core/GameController.cs` — `PluginBridge` (`GetMethod<T>` / `SaveMethod`).
- `Core/Shared/AtlasHelper/AtlasTexture.cs` — the `AtlasTexture` returned by `GetAtlasTexture`.
