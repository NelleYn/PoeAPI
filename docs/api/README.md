# ExileCore Plugin API Reference

This is the index of the **plugin-author** API reference for ExileCore (ExileApi). It is
written for people building HUD/automation plugins, not for people hacking on the engine
itself — for engine internals see [architecture.md](../architecture.md),
[offsets.md](../offsets.md) and [plugin-compiler.md](../plugin-compiler.md). Every type,
member and signature documented here is grounded in the `Core/` source of this repo; where
a popular reference plugin uses a symbol that does **not** live in this repo, that is called
out as belonging to an external library.

## What ExileCore is

ExileCore is a Windows-only .NET 10 HUD/plugin framework for Path of Exile. It runs as a
separate process, reads the live game's memory read-only, interprets it as a tree of typed
objects (entities, components, UI elements, static data files), and draws an overlay on top
of the game window with DirectX 11. Plugins consume that parsed data and render or act on it.

A plugin is a class deriving from
[`BaseSettingsPlugin<TSettings>`](../../Core/BaseSettingsPlugin.cs) where `TSettings`
implements [`ISettings`](../../Core/Shared/Interfaces/ISettings.cs). At load time the host
calls `SetApi(...)`, which injects a [`GameController`](../../Core/GameController.cs) (your
window into game state) and a [`Graphics`](../../Core/Graphics.cs) facade (your way to draw)
as the `GameController` and `Graphics` properties. Each frame the host calls your
`Tick()` (logic — may return a `Job` to run off the main thread) and `Render()` (drawing).
Settings are loaded from / saved to JSON automatically and rendered into the plugin menu.
Other overridable hooks include `Initialise()`, `OnLoad()`, `AreaChange(AreaInstance)`,
`EntityAdded(Entity)` and `EntityRemoved(Entity)` (see [plugins.md](plugins.md)).

## Your first plugin

A plugin is two classes: a settings class and the plugin class.

```csharp
using ExileCore;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;                     // Color, Vector2, Vector3 used by the engine
using Vector2N = System.Numerics.Vector2;

// 1. Settings: must implement ISettings, which requires the Enable toggle.
public class MySettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);
}

// 2. Plugin: subclass BaseSettingsPlugin<TSettings>.
public class MyPlugin : BaseSettingsPlugin<MySettings>
{
    public override void Render()
    {
        var player = GameController.Player;       // the local player Entity
        if (player == null)
            return;

        // World position of the player (SharpDX Vector3) -> screen position.
        Vector2 screenPos = GameController.IngameState.Camera.WorldToScreen(player.Pos);

        // DrawText returns the rendered text size (System.Numerics.Vector2).
        Graphics.DrawText("Hello from MyPlugin", screenPos, Color.Yellow);
    }
}
```

Notes, all verified against source:

- `ISettings` only requires `ToggleNode Enable { get; set; }`
  ([ISettings.cs](../../Core/Shared/Interfaces/ISettings.cs)). `ToggleNode` has a
  `ToggleNode(bool)` constructor and an implicit `operator bool`, so `new(true)` is valid and
  `if (Settings.Enable)` works directly ([ToggleNode.cs](../../Core/Shared/Nodes/ToggleNode.cs)).
- `GameController` and `Graphics` are public properties on `BaseSettingsPlugin<TSettings>`,
  populated by `SetApi(GameController, Graphics, PluginManager)`
  ([BaseSettingsPlugin.cs](../../Core/BaseSettingsPlugin.cs)).
- `GameController.Player` is the local player `Entity`; `Entity.Pos` is a SharpDX `Vector3`
  ([Entity.cs](../../Core/PoEMemory/MemoryObjects/Entity.cs)).
- `GameController.IngameState.Camera.WorldToScreen(Vector3)` returns a SharpDX `Vector2`
  ([Camera.cs](../../Core/PoEMemory/MemoryObjects/Camera.cs)).
- `Graphics.DrawText(string, Vector2, Color)` returns a `System.Numerics.Vector2` (the text
  size); it accepts both SharpDX and System.Numerics positions via overloads
  ([Graphics.cs](../../Core/Graphics.cs)).

This is the exact pattern used by the real plugins: `Radar`, `PickItV2` and `Stashie` all
declare `class X : BaseSettingsPlugin<XSettings>`, their settings implement `ISettings` with
`public ToggleNode Enable { get; set; } = new ToggleNode(...)`, and they call
`GameController.IngameState.Camera.WorldToScreen(...)` and `GameController.Player` exactly as
shown.

## Documentation map

| Document | Covers |
| --- | --- |
| [plugins.md](plugins.md) | The plugin lifecycle: `BaseSettingsPlugin<TSettings>`, `IPlugin`, `SetApi`, `Initialise`/`Tick`/`Render`/`AreaChange`/entity hooks, `Job`, hot reload. |
| [settings.md](settings.md) | The settings system: `ISettings`, the `*Node` types (`ToggleNode`, etc.), menu attributes, JSON load/save. |
| [game-controller.md](game-controller.md) | `GameController` — the root API surface: `Player`, `Entities`, `IngameState`, `Game`, `Area`, `Window`, `Files`, `Cache`, `PluginBridge`, panels. |
| [entities.md](entities.md) | `Entity` and `EntityListWrapper`: identity, `Pos`/`GridPos`/`BoundsCenterPos`, `RenderName`, `Rarity`, `IsAlive`, `DistancePlayer`, `GetComponent<T>`/`HasComponent<T>`. |
| [components-combat.md](components-combat.md) | Combat-related components: `Life`, `Actor`, `Buffs`, `ObjectMagicProperties`, `Stats`, `DiamondSockets` and friends. |
| [components-items.md](components-items.md) | Item components: `Base`, `Mods`, `Sockets`, `Quality`, `Stack`, `RenderItem`, `Map`, etc. |
| [components-world.md](components-world.md) | World/interaction components: `Positioned`, `Render`, `Chest`, `Targetable`, `Transitionable`, `Shrine`, `MinimapIcon`. |
| [ingame-state.md](ingame-state.md) | `IngameState`, `IngameData`, `IngameUi`, `Camera`, `ServerData`, `LatencyRectangle`. |
| [ui-elements.md](ui-elements.md) | The UI tree: `Element`, `GetClientRect`/`GetClientRectCache`, visibility, children traversal, common typed elements. |
| [inventories.md](inventories.md) | Inventory and stash UI: `ServerInventory`, `InventoryElement`, `NormalInventoryItem`, stash tabs. |
| [graphics.md](graphics.md) | `Graphics`: `DrawText`, `DrawLine`, `DrawFrame`, `DrawBox`, `DrawImage`/`DrawImageGui`, `MeasureText`, `InitImage`, atlas textures, fonts. |
| [coordinates.md](coordinates.md) | Coordinate spaces and conversions: world `Vector3`, grid `Vector2`, screen, `Camera.WorldToScreen`, `RectangleF`, SharpDX vs `System.Numerics`/`Vector2N`. |
| [files-in-memory.md](files-in-memory.md) | Static game data via `GameController.Files` (`FilesContainer`): `BaseItemTypes`, `Mods`, `Stats`, `WorldAreas`, `Quests`, etc. |
| [caching.md](caching.md) | The cache layer plugins rely on: `FrameCache`, `TimeCache`, `AreaCache`, `CachedValue`, when values refresh. |
| [input.md](input.md) | Reading mouse/keyboard and driving input (where exposed) for automation plugins. |
| [memory.md](memory.md) | Low-level access: `GameController.Memory` (`IMemory`), `RemoteMemoryObject`, reading raw structs/strings (advanced). |
| [utilities.md](utilities.md) | Helper APIs: `DebugWindow` logging, `Graphics`/vector extension helpers, `PluginBridge`, coroutines/`Job`. |
| [enums.md](enums.md) | Common enums plugins use: `MonsterRarity`, `FontAlign`, `IconPriority`, `GameStat`, etc. |

Engine-internals docs (not plugin API, but useful background):

| Document | Covers |
| --- | --- |
| [../architecture.md](../architecture.md) | How the engine reads memory, builds objects, caches and renders. |
| [../offsets.md](../offsets.md) | The `GameOffsets` structs that map raw PoE memory. |
| [../plugin-compiler.md](../plugin-compiler.md) | How source plugins are compiled and hot-reloaded at runtime. |

## Common tasks -> API

| I want to... | Use |
| --- | --- |
| Draw text / lines / boxes on the overlay | `Graphics.DrawText` / `DrawLine` / `DrawBox` / `DrawFrame` ([graphics.md](graphics.md)) |
| Draw an image / atlas icon | `Graphics.DrawImage` (+ `InitImage`, `GetAtlasTexture`) ([graphics.md](graphics.md)) |
| Get the player | `GameController.Player` ([game-controller.md](game-controller.md)) |
| Loop over all entities | `GameController.Entities` / `GameController.EntityListWrapper` ([entities.md](entities.md)) |
| Read a monster's HP | `entity.GetComponent<Life>()` then `.CurHP` / `.MaxHP` ([components-combat.md](components-combat.md)) |
| Get an entity's world position | `entity.Pos` (Vector3); grid cell `entity.GridPos` ([entities.md](entities.md)) |
| Convert world -> screen | `GameController.IngameState.Camera.WorldToScreen(vec3)` ([coordinates.md](coordinates.md)) |
| React to area changes | override `AreaChange(AreaInstance)` ([plugins.md](plugins.md)) |
| React to entities appearing/leaving | override `EntityAdded` / `EntityRemoved` ([plugins.md](plugins.md)) |
| Add a setting (toggle/slider/hotkey) | settings `*Node` fields on your `ISettings` class ([settings.md](settings.md)) |
| Read static item / mod data | `GameController.Files` (`BaseItemTypes`, `Mods`, ...) ([files-in-memory.md](files-in-memory.md)) |
| Read the in-game UI (panels, inventories) | `GameController.IngameState.IngameUi` ([ingame-state.md](ingame-state.md), [ui-elements.md](ui-elements.md)) |
| Read inventory/stash items | `IngameState.IngameUi` inventory elements ([inventories.md](inventories.md)) |
| Do heavy work off the render thread | return a `Job` from `Tick()` ([plugins.md](plugins.md), [utilities.md](utilities.md)) |
| Log a message to the overlay | `LogMessage` / `LogError` (or `DebugWindow.LogMsg`) ([utilities.md](utilities.md)) |
| Share data with another plugin | `GameController.PluginBridge` ([game-controller.md](game-controller.md)) |

## Reference plugins

These are real, community-endorsed plugins that consume this API. Each is a good worked
example of the API areas listed. Some import helper libraries that are **not** part of this
repo — most notably `ItemFilterLibrary` (item-filter querying / the `*WithLinq` family) and
`SDxColorConverter`-style helpers; symbols from those libraries are documented as external
where they appear.

| Plugin | Repo(s) | Best demonstrates |
| --- | --- | --- |
| Radar | instantsc/Radar | Full-map outline; `Camera.WorldToScreen`, terrain/pathfinding, `Graphics` line drawing ([coordinates.md](coordinates.md), [graphics.md](graphics.md)) |
| ReAgent | exApiTools/ReAgent | Condition→action mapping; entities, components, buffs, input ([entities.md](entities.md), [components-combat.md](components-combat.md), [input.md](input.md)) |
| AltarHelper | bruno105/AltarHelper | Eldritch altar highlight; UI elements + mod text ([ui-elements.md](ui-elements.md), [files-in-memory.md](files-in-memory.md)) |
| BlightHelper | bruno105/BlightHelper | Anoint highlight; UI elements ([ui-elements.md](ui-elements.md)) |
| WhereAreYouGoing | DetectiveSquirrel/ExileAPI-WhereAreYouGoing | Monster pathing; `Positioned`/`Actor`, world→screen ([components-world.md](components-world.md), [coordinates.md](coordinates.md)) |
| ExpeditionIcons | instantsc/ExpeditionIcons | Expedition UI; entities + minimap drawing ([entities.md](entities.md), [graphics.md](graphics.md)) |
| NinjaPricer (Get-Chaos-Value) | instantsc/Get-Chaos-Value | Pricing; item components + static files ([components-items.md](components-items.md), [files-in-memory.md](files-in-memory.md)) |
| ProximityAlert | vadash/ProximityAlert | Mod/path warnings; entities, `ObjectMagicProperties`, drawing ([components-combat.md](components-combat.md), [graphics.md](graphics.md)) |
| ShowGroundEffects | arturino009/ShowGroundEffects | Ground-effect circles; entities + world→screen drawing ([coordinates.md](coordinates.md), [graphics.md](graphics.md)) |
| DevTree | exApiTools/DevTree | Debugging/exploration of the whole memory object tree ([game-controller.md](game-controller.md), [memory.md](memory.md)) |
| BetterSanctum (PathfindSanctum) | ChandlerFerry/PathfindSanctum | Sanctum; UI elements + pathfinding ([ui-elements.md](ui-elements.md)) |
| HarvestPicker | exApiTools/HarvestPicker | Harvest values; UI elements + item data ([ui-elements.md](ui-elements.md), [files-in-memory.md](files-in-memory.md)) |
| FullRareSetManager | exApiTools/FullRareSetManager | Chaos recipe; inventories + item components ([inventories.md](inventories.md), [components-items.md](components-items.md)) |
| EssenceCorruptionHelper | deMathias/EssenceCorruptionHelper | Essence UI; UI elements ([ui-elements.md](ui-elements.md)) |
| SkillGems | DetectiveSquirrel/SkillGems | Gem leveling; item components + UI ([components-items.md](components-items.md), [ui-elements.md](ui-elements.md)) |
| BroodyHen | IlliumIv/BroodyHen | Incubator frame; item components + drawing ([components-items.md](components-items.md), [graphics.md](graphics.md)) |
| EZVendor | vadash/EZVendor | Vendoring; inventories + input automation ([inventories.md](inventories.md), [input.md](input.md)) |
| Stashie | DetectiveSquirrel/Stashie | Stash management; inventories, UI elements, input ([inventories.md](inventories.md), [ui-elements.md](ui-elements.md), [input.md](input.md)) |
| PickItV2 | exApiTools/PickItV2 | Item pickup; ground items, components, input ([components-items.md](components-items.md), [input.md](input.md)) |
| NPCInventoryWithLinq | DetectiveSquirrel/NPCInvWithLinq | NPC inventory filtering (uses external `ItemFilterLibrary`) ([inventories.md](inventories.md)) |
| GroundItemsWithLinq | DetectiveSquirrel/Ground-Items-With-Linq | Ground-item filtering (uses external `ItemFilterLibrary`) ([components-items.md](components-items.md)) |
| InventoryItemsWithLinq | mikkelpetersen/InvWithLinq | Inventory filtering (uses external `ItemFilterLibrary`) ([inventories.md](inventories.md)) |
| WhereTheWispsAt | exApiTools/WhereTheWispsAt | Wisp highlighting; entities + drawing ([entities.md](entities.md), [graphics.md](graphics.md)) |
| WhatAreYouDoing | DetectiveSquirrel/WhatAreYouDoing | Lab traps; entities + world→screen ([entities.md](entities.md), [coordinates.md](coordinates.md)) |
| GuardianStats | DetectiveSquirrel/Guardians-R-Us | Guardian stats; `Life`/`Stats` components ([components-combat.md](components-combat.md)) |
| LevelingHelper | TehCheat/LevelingHelper | Leveling guidance; quests/static files + UI ([files-in-memory.md](files-in-memory.md)) |
| VillageHelper | instantsc/VillageHelper | Village/Settlers UI ([ui-elements.md](ui-elements.md)) |
| IFL Inspector | DetectiveSquirrel/ItemFilterLibInspector | Inspecting item-filter data (uses external `ItemFilterLibrary`) ([components-items.md](components-items.md)) |
| WheresMyCraftAt | DetectiveSquirrel/WheresMyCraftAt | Crafting automation; UI elements + input ([ui-elements.md](ui-elements.md), [input.md](input.md)) |
| AreaStatVisual | DetectiveSquirrel/AreaStatVisual | Area mods; map stats via `IngameState`/`Files` ([ingame-state.md](ingame-state.md), [files-in-memory.md](files-in-memory.md)) |
| Blight Paths | DetectiveSquirrel/Blight | Blight pathing; entities + line drawing ([entities.md](entities.md), [graphics.md](graphics.md)) |
| Abyss Paths | DetectiveSquirrel/Abyss | Abyss pathing; entities + line drawing ([entities.md](entities.md), [graphics.md](graphics.md)) |
| Character Data | DetectiveSquirrel/Character-Data | Character stats; player components + `ServerData` ([components-combat.md](components-combat.md), [ingame-state.md](ingame-state.md)) |
| WhereTheCirclesAt | DetectiveSquirrel/WhereTheCirclesAt | Ritual circles; entities + drawing ([entities.md](entities.md), [graphics.md](graphics.md)) |
| WheresMyCursor | DetectiveSquirrel/Wheres-My-Cursor | Cursor highlight; input/cursor position + drawing ([input.md](input.md), [graphics.md](graphics.md)) |
| Preloads Revised | DetectiveSquirrel/PreloadsRevised-poe1 | Preload detection; file/preload scanning ([files-in-memory.md](files-in-memory.md)) |
| WhereMyFavsAt | deMathias/WhereMyFavsAt | Stash favorites highlight; inventories + UI ([inventories.md](inventories.md), [ui-elements.md](ui-elements.md)) |
| Beasts | bruno105/Beasts | Beast capture highlight; entities + static files ([entities.md](entities.md), [files-in-memory.md](files-in-memory.md)) |

## Source

This index is based on the following files in this repository:

- [README.md](../../README.md)
- [docs/architecture.md](../architecture.md)
- [Core/BaseSettingsPlugin.cs](../../Core/BaseSettingsPlugin.cs)
- [Core/Shared/Interfaces/IPlugin.cs](../../Core/Shared/Interfaces/IPlugin.cs)
- [Core/Shared/Interfaces/ISettings.cs](../../Core/Shared/Interfaces/ISettings.cs)
- [Core/GameController.cs](../../Core/GameController.cs)
- [Core/Graphics.cs](../../Core/Graphics.cs)
- [Core/Shared/Nodes/ToggleNode.cs](../../Core/Shared/Nodes/ToggleNode.cs)
- [Core/PoEMemory/MemoryObjects/Entity.cs](../../Core/PoEMemory/MemoryObjects/Entity.cs)
- [Core/PoEMemory/MemoryObjects/Camera.cs](../../Core/PoEMemory/MemoryObjects/Camera.cs)
- [Core/PoEMemory/MemoryObjects/IngameState.cs](../../Core/PoEMemory/MemoryObjects/IngameState.cs)
- [Core/PoEMemory/Components/Life.cs](../../Core/PoEMemory/Components/Life.cs)
- [Core/PoEMemory/FilesContainer.cs](../../Core/PoEMemory/FilesContainer.cs)

Cross-checked against the real plugins `instantsc/Radar`, `exApiTools/PickItV2` and
`DetectiveSquirrel/Stashie`.
