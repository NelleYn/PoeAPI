# IconsBuilder (ported candidate)

> **EXPERIMENTAL — not compiled in this environment.**
> This is Windows + live-game only. It cannot be built or run here (no `ExileCore.dll`, no game
> process). Every file starts with an `// EXPERIMENTAL candidate ...` banner and lives under
> `proposals/` so it is **outside the build** and cannot break `Core/`, `GameOffsets/` or `Loader/`.
> Nothing under `Core/` was modified.

## What IconsBuilder is

[IconsBuilder](https://github.com/exApiTools/IconsBuilder) (archived) is a reusable ExileApi/ExileCore
icon library for Path of Exile overlays. It does **not** draw anything itself — it is an
**entity → icon factory**. For every game entity it decides which kind of map icon (if any)
represents it, what sprite/size/colour to use, and a `Show` predicate for when to display it. It
attaches the resulting `BaseIcon` to the entity as a HUD component. A separate renderer plugin
(e.g. [MinimapIcons](https://github.com/exApiTools/MinimapIcons),
[HeistIcons](https://github.com/exApiTools/HeistIcons)) then pulls those icons back off the entities
and blits them onto the minimap or into the world.

The mapping is: **entity type / metadata path / league → sprite cell + size + colour + show-rule**,
all over a couple of shared sprite sheets (`Icons.png`, `sprites.png`) addressed by UV rectangles
computed from a linear/`(col,row)` index (`SpriteHelper.GetUV`). Special cases (strongbox kinds,
delve chests, abyss nodes, monster mods, legion frozen monsters, …) are handled by dedicated icon
subclasses.

## How this port maps onto our fork

The original library ships its own `BaseIcon`. **This fork already has one** at
`Core/Shared/Abstract/BaseIcon.cs`, and this port **builds on the fork's `BaseIcon` unchanged**
(it is not re-defined here). All ported icon classes extend `ExileCore.Shared.Abstract.BaseIcon` and
set its `protected`/`public` members (`MainTexture`, `Show`, `Hidden`, `Text`, `Priority`,
`GridPosition`, `DrawRect`) and read its protected statics (`strongboxesUV`, `FossilRarity`,
`PathCheck`, `RenderName`, `_HasIngameIcon`).

The fork's `BaseIcon.MainTexture` is a `HudTexture` (file name + UV + size + colour), exactly like the
original, so the per-icon sprite mapping ports verbatim. The fork's `BaseIcon` constructor already
contains the bulk of the in-game-minimap-icon detection (`MinimapIcon` lookup, portal/abyss/delve
special-casing), so the subclasses only add their type-specific sprite/size/show logic.

Rendering is shown by `IconRenderer.cs`, which maps an icon onto this fork's `Graphics` facade:

- **World draw**: `Camera.WorldToScreen(Render.Pos)` → `Graphics.DrawImage(fileName, rect, uv, color)`.
- **Minimap draw**: host projects `BaseIcon.GridPosition()` into minimap pixels, then
  `Graphics.DrawImage(...)` + `Graphics.DrawText(...)`.
- **Atlas draw** (HeistIcons style): `BaseSettingsPlugin.GetAtlasTexture(name)` →
  `Graphics.DrawImage(AtlasTexture, rect, color)`.

## Files in this port

| File | Role | Notes vs original |
|------|------|-------------------|
| `IconsBuilder.cs` | The reusable **entity → icon factory** (`EntityAddedLogic`) + a thin host plugin (`EntityAdded`/`Tick`/`AreaChange`). | `TryGetComponent` replaced with `HasComponent`+`GetComponent`; the Legion branch dispatches on `entity.League == LeagueType.Legion` as before, and Delirium monsters now dispatch to `DeliriumIcon` via a `GameStat.AffectedByDelirium` stat check + a doodad-daemon path prefix (substituting for the missing `LeagueType.Delirium`, see below); the Heist league branch is still dropped (no such `LeagueType`, and no path-only signal distinguishes ordinary Heist-area monsters from other monsters); multithreading + alert-config/ignore-file loading dropped (plugin glue); `JM.LinqFaster` swapped for plain LINQ. |
| `IconsBuilderSettings.cs` | `ISettings` with the sizes/toggles the ported icons read. | Trimmed to used members; Expedition/Sanctum settings removed; `DeliriumText`/`HeistText` kept (now used again, see below); added required `Enable`. |
| `MonsterIcon.cs` | Rarity → sprite/size, mod-icon lookup. | Unique sprite remapped (see below); the ~90-entry Archnemesis name table dropped. Unknown/out-of-range `MonsterRarity` reads fall back to the Unique-sized/coloured branch instead of throwing (original threw). |
| `DeliriumIcon.cs` | Delirium-league monster icon: fixed sprites for the Delirium-fog "doodad daemon" spawners, otherwise the same rarity/mod-icon logic as `MonsterIcon`. | **Now ported.** Dispatch uses `GameStat.AffectedByDelirium` (read the same way `LegionIcon` already reads `GameStat.MonsterMinimapIcon`) plus the `Metadata/Monsters/LeagueAffliction/DoodadDaemons` path prefix, substituting for the absent `LeagueType.Delirium`. Otherwise faithful to the original; unknown rarity also falls back gracefully instead of throwing. |
| `LegionIcon.cs` | Legion frozen-in-time monsters & legion monster-chests. | Unique sprite remapped; otherwise faithful. Unknown/out-of-range `MonsterRarity` reads fall back gracefully instead of throwing (original threw). |
| `MiscIcon.cs` | Area transitions, abyss nodes, incursion portals, delve sulphite/azurite. | Sanctum (`SanctumMote`) branch dropped (no Sanctum league/sizes). The abyss-node `Show` predicates fix the original's `?? true == false` operator-precedence bug — re-parenthesized as `!(IsHide ?? true) || (Flag1 ?? 0) == 1`. |
| `ChestIcon.cs` | Chest/strongbox/league-container classification → sprite/size/colour/label. | Heist chests are now detected via their path (`Metadata/Chests/LeagueHeist/HeistChest`, no `LeagueType.Heist` needed) and rendered by reusing the existing `ChestType.Strongbox` sprite/label path (with the original's `HeistText` path-stripping logic) instead of silently falling back to `ChestType.SmallChest`. Expedition/Sanctum `ChestType` cases remain dropped — those need native `MapIconsIndex` sprite ids (`ExpeditionChest2`, `HeistPathChest`) that don't exist in this fork and can't be assigned a value without live-game verification (see below). Verbose per-delve-path text simplified. |
| `NpcIcon.cs`, `PlayerIcon.cs`, `SelfIcon.cs`, `ShrineIcon.cs`, `MissionMarkerIcon.cs` | Simple single-sprite icons. | Faithful; defensive null-checks added. |
| `Icons/IngameIconReplacerIcon.cs` | Rebuild an icon for an entity that owns an in-game minimap icon, so it stays drawn while out of range. | Dynamic size (`Files.MinimapIcons[...].LargeMinimapSize` × `Map.LargeMapZoom` × `Camera.Height`) replaced with a fixed `SizeDefaultIcon` — this fork has no `Files.MinimapIcons` table. The `Show` predicate (cache-while-valid, draw-while-`!IsValid`) is ported verbatim from the original; the host renderer only invokes the replacer for out-of-range entities. |
| `IconRenderer.cs` | Reusable drawing helper (world / minimap / atlas) onto this fork's `Graphics`. | New; not in original (original kept rendering in the consumer plugins). |

## Exact fork members this port builds on (verified in `origin/master`)

Abstract base & textures
- `Core/Shared/Abstract/BaseIcon.cs:12` — `BaseIcon` (ctor `:65` takes `(Entity, ISettings)`; members `MainTexture:165`, `Show:163`, `Hidden:164`, `Priority:166`, `Text:168`, `GridPosition:161`, `DrawRect:162`, `RenderName:169`, `PathCheck:171`, `strongboxesUV:14`, `FossilRarity:33`, `_HasIngameIcon:63`).
- `Core/Shared/HudTexture.cs:5` — `HudTexture` (`FileName:16`, `UV:17`, `Size:18`, `Color:19`).
- `Core/Shared/AtlasHelper/AtlasTexture.cs:6` — `AtlasTexture` (`TextureUV:19`, `AtlasFileName:18`).

Drawing / camera
- `Core/Graphics.cs:157` — `DrawImage(string, RectangleF, RectangleF, Color)`; `:172` `DrawImage(AtlasTexture, RectangleF, Color)`; `:77` `DrawText(string, Vector2, Color, FontAlign)`; `:214` `InitImage(string, bool)`.
- `Core/PoEMemory/MemoryObjects/Camera.cs:38` — `WorldToScreen(Vector3)` → SharpDX `Vector2`; `:27` `Camera.Height`.
- `Core/PoEMemory/MemoryObjects/IngameState.cs:67` — `Camera`.
- `Core/BaseSettingsPlugin.cs:204` — `GetAtlasTexture(string)`.

Plugin host
- `Core/BaseSettingsPlugin.cs:19` — `BaseSettingsPlugin<TSettings>`; `:33` `GameController`, `:34` `Graphics`, `:35` `Settings`, `:73` `AreaChange`, `:93` `EntityAdded`, `:113` `OnLoad`, `:123` `Initialise`, `:157` `Tick`.
- `Core/DebugWindow.cs:138` — `LogError(string, float)`.
- `Core/Logger.cs:16` — `Logger.Log`.
- `Core/MultiThreadManager.cs:12` — `Job` (Tick return type).

Game model
- `Core/GameController.cs:134` `IngameState`, `:170` `EntityListWrapper`, `:182` `Entities`, `:125` `Game`.
- `Core/EntityListWrapper.cs:128` `Entities`, `:134` `Player`.
- `Core/PoEMemory/MemoryObjects/IngameData.cs:39` — `LocalPlayer`.
- `Core/AreaInstance.cs` — `AreaInstance` (AreaChange arg).

Entity & components
- `Core/PoEMemory/MemoryObjects/Entity.cs` — `Type:91`, `League:92`, `IsHidden:94`, `IsValid:97`, `IsAlive:99`, `GridPos:157`, `RenderName:177`, `Rarity:194`, `IsOpened:196`, `Stats:230`, `IsTargetable:261`, `Path:282`, `Id:377`, `IsHostile:382`, `HasComponent:580`, `GetComponent:605`, `GetComponentFromMemory:634`, `GetHudComponent:755`, `SetHudComponent:761`.
- `Core/PoEMemory/Components/Render.cs:34` `Pos`, `:43` `Name`.
- `Core/PoEMemory/Components/MinimapIcon.cs:17` `IsVisible`, `:20` `IsHide`, `:28` `Name`.
- `Core/PoEMemory/Components/Transitionable.cs:9` `Flag1`.
- `Core/PoEMemory/Components/Shrine.cs:9` `IsAvailable`.
- `Core/PoEMemory/Components/Player.cs:19` `PlayerName`.
- `Core/PoEMemory/Components/Life.cs:64` `HPPercentage`.
- `Core/PoEMemory/Components/ObjectMagicProperties.cs:28` `Rarity`, `:45` `Mods`.
- `Core/PoEMemory/Components/Stats.cs:35` `StatDictionary`, `:41` `ParseStats`.
- `Core/PoEMemory/Components/Targetable.cs`, `Core/PoEMemory/Components/Chest.cs:25` `IsOpened`.

Helpers / enums / static
- `Core/Shared/Helpers/SpriteHelper.cs` — `GetUV(MapIconsIndex):26`, `GetUV(MyMapIconsIndex):16`, `GetUV(int,Size2F):37`, `GetUV(Size2,Size2F):55`.
- `Core/Shared/Helpers/Extensions.cs:67` — `IconIndexByName(string)`.
- `Core/Shared/Constants.cs:7` `MapIconsSize`, `:8` `MyMapIcons`.
- `Core/Shared/Static/HudSkin.cs:8-10` — `MagicColor`/`RareColor`/`UniqueColor`.
- `Core/Shared/Enums/` — `MapIconsIndex.cs`, `MyMapIconsIndex.cs`, `IconPriority.cs`, `EntityType.cs`, `League.cs` (`LeagueType`), `ChestType.cs`, `MonsterRarity` (`Enums/`), `GameStat.cs` (`MonsterMinimapIcon:12308`, `FrozenInTime:47018`, `MonsterHideMinimapIcon:49728`, `AffectedByDelirium:8945`), `FontAlign.cs:6`.
- `Core/Shared/Nodes/` — `ToggleNode`, `RangeNode<T>`, `ButtonNode` (`OnPressed`), `Core/Shared/Attributes/MenuAttribute`.

## Members deliberately NOT used (absent from this fork) and how they were adapted

- **`Entity.TryGetComponent`** — does not exist. Replaced with `HasComponent` + `GetComponent`.
- **`LeagueType.Delirium`** — absent (`League.cs` only has General/Incursion/Abyss/Breach/Perandus/
  Delve/Legion; re-verified, still true). `DeliriumIcon` **is now ported** (see below), but its
  factory dispatch can't check `entity.League == LeagueType.Delirium` like the original — instead it
  checks the `GameStat.AffectedByDelirium` stat (read the same way `LegionIcon` already reads
  `GameStat.MonsterMinimapIcon` — a real, already-established mechanism, not a guess) plus the
  Delirium-fog doodad-daemon path prefix the icon itself also matches on.
- **`LeagueType.Heist`** — absent, still true. Nothing about a plain Heist-area monster's `Entity.Path`
  distinguishes it from a monster anywhere else (unlike chests, which have Heist-specific path
  prefixes — see next point), so there is no reliable path-based substitute for the League check.
  Heist *monster* icon handling (the original's `MiscIcon` League filter) remains out of scope; a
  live client would be needed to confirm whether some other signal (a component/stat) exists.
- **`ChestType.Heist` / `Expedition` / `Sanctum`** — absent from `ChestType.cs` (re-verified against
  the current enum, still true for all three). Of the three:
  - **Heist chests are now handled**: they're detected via the `Metadata/Chests/LeagueHeist/HeistChest`
    path prefix (no `LeagueType.Heist` needed) and rendered by reusing the existing
    `ChestType.Strongbox` sprite/size/label branch, with the original's `HeistText`-gated path-stripping
    label logic ported verbatim into that branch. This means Heist chests render as a real strongbox
    icon instead of silently defaulting to `ChestType.SmallChest`.
  - **Expedition and Sanctum remain omitted.** Their original rendering needs
    `MapIconsIndex.ExpeditionChest2` / `HeistPathChest`, which are native-game icon ids not catalogued
    in this fork's `MapIconsIndex` enum (unlike `ChestType`, these are not free-standing labels — their
    correct numeric value is defined by the live PoE client's own icon table). Assigning them an
    arbitrary value would be indistinguishable from guessing an undiscovered memory-derived constant,
    which is out of scope here; this needs live-game verification before it can be ported.
  - Any other chest that still doesn't match a known pattern continues to fall back to
    `ChestType.SmallChest` — this is the same "no more specific classification" default the original
    library used, not a bug.
- **`MapIconsIndex.LootFilterLargeWhiteHexagon`** — absent. Unique-monster icon remapped to
  `LootFilterLargeYellowHexagon` tinted `Color.DarkOrange` (also applied in the new `DeliriumIcon`).
- **`MapIconsIndex.RewardChestGeneric` / `HeistPathChest` / `ExpeditionChest2`** — absent; only used
  by the still-dropped Expedition/Sanctum cases (`RewardChestGeneric` was only used by the original's
  `ChestType.Heist` case, which this port no longer needs since it reuses `Strongbox`'s sprite instead).
- **Unknown/out-of-range `MonsterRarity` reads** in `MonsterIcon`/`LegionIcon`/`DeliriumIcon` — the
  original throws (`ArgumentOutOfRangeException`/`ArgumentException`) on an unrecognised `Rarity`.
  All three ported icons instead fall back to the Unique-styled branch, matching `BaseIcon`'s own
  graceful default for unrecognised rarity (`Core/Shared/Abstract/BaseIcon.cs`'s constructor treats
  unknown rarity as `IconPriority.Critical`, the same priority as Unique).
- **`TheGame.Files.MinimapIcons` / `AtlasNode.LargeMinimapSize` / `Map.LargeMapZoom`-based sizing** —
  no `Files.MinimapIcons` table in this fork. `IngameIconReplacerIcon` uses a fixed `SizeDefaultIcon`.
- **`JM.LinqFaster` (`AnyF`/`FirstOrDefaultF`)** — replaced with standard `System.Linq` to avoid a
  hard dependency (the package is referenced by `Core` but the port keeps itself dependency-light).
- **Multithreading (`GameController.MultiThreadManager.AddJob`)**, **alert-config / ignored-entity
  file IO**, and the `Coroutine`/`WaitTime` "fix icons" loop — these are plugin operational glue, not
  the reusable core, and were dropped to keep the port focused.

## How to integrate

This port targets namespace `ExileCore.IconsBuilder` (and `ExileCore.IconsBuilder.Icons`).

1. **As a library plugin** (closest to the original split): ship `IconsBuilder` as its own
   `BaseSettingsPlugin<IconsBuilderSettings>`, ship `Icons.png` + `sprites.png` next to it, and have a
   renderer plugin (your minimap plugin) read `entity.GetHudComponent<BaseIcon>()` each frame and draw
   it with `IconRenderer` (or your own projection). A host can also instantiate `IconsBuilder`
   directly and call `Initialise()`/`Tick()`/`AreaChange()` itself, as MinimapIcons did.
2. **Move into `Core/`**: copy these files under `Core/` (e.g. `Core/Shared/IconsBuilder/`), keep the
   `ExileCore.IconsBuilder` namespace or fold into `ExileCore.Shared.*`, drop the experimental banners,
   and remove this `proposals/` copy. The icon classes already extend the in-tree
   `Core/Shared/Abstract/BaseIcon.cs`, so no base-class duplication is needed.

Either way the host must `Graphics.InitImage("Icons.png")` and `Graphics.InitImage("sprites.png")`
(and provide those PNGs) before drawing, and populate `IconsBuilder.ModIcons` if mod icons are wanted.

### Dependencies

- `ExileCore.dll` (this fork): `Graphics`, `GameController`, `BaseSettingsPlugin<>`, the
  `ExileCore.PoEMemory.*` entity/component model, `ExileCore.Shared.*` helpers/enums/nodes, SharpDX.
- **SharpDX** for `Vector2`/`Vector3`/`RectangleF`/`Color`/`Size2`/`Size2F`/`ColorBGRA` — this matches
  the fork: `BaseIcon`, `HudTexture`, `Camera`, `Render`, `Entity` all use SharpDX (only `Graphics`
  also exposes `System.Numerics` overloads). This port uses **SharpDX throughout** to match
  `BaseIcon`.
- Two sprite sheets: `Icons.png` (game-style map icons, `MapIconsIndex`, 14×16 grid) and `sprites.png`
  (custom icons, `MyMapIconsIndex` / mod icons, 7×8 grid).

## Provenance

- Original: <https://github.com/exApiTools/IconsBuilder> (archived, MIT-style ExileApi tooling).
- Consumers studied for the draw/consume contract:
  <https://github.com/exApiTools/MinimapIcons>, <https://github.com/exApiTools/HeistIcons>.
- All three cloned successfully (no 404).
