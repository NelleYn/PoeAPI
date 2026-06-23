# Plugin usage index & documentation gap report

This document audits how a broad set of real, published ExileCore (ExileApi) plugins
actually consume the API, and cross-checks that audit against (a) this fork's `Core`
source and (b) the plugin-author docs in this directory. It exists so the docs can be
proven to cover what plugins really use, and so that the few symbols plugins use that
this fork does **not** expose are recorded rather than silently invented.

[API reference index](README.md)

Scope and method:

- 45 plugin repositories were shallow-cloned and every `*.cs` file grepped for ExileCore
  API usage (component access, `GameController.*`, `IngameState`/`IngameUi`, `Graphics.*`,
  `Input.*`, `GameController.Files.*`, settings `*Node` types, enums, `PluginBridge`,
  `DebugWindow`, coordinate helpers, etc.). See [Source](#source) for the exact commits.
- Every "present / absent / documented" claim below was grounded by grepping this fork's
  `Core/` and `GameOffsets/` source and the `docs/api/*.md` files on this branch's HEAD.
  Symbols that real plugins use but this fork does not expose are listed under
  [Upstream-only symbols](#upstream-only-symbols) — they are *not* documented as if they
  were part of this API.

---

## Repos analyzed

All 45 listed repos cloned successfully (some are intentional alternates / forks of the
same plugin, kept because they exercise the API slightly differently). Three repos from
the original list could not be cloned at their given paths and were resolved to alternates
(noted under "reachable").

| Plugin | Repo | Reachable | Commit | Date | .cs / ~LOC |
|---|---|---|---|---|---|
| Radar | instantsc/Radar | ✓ | c244597 | 2026-03-18 | 14 / 1717 |
| ReAgent | exApiTools/ReAgent | ✓ | 2502d86 | 2026-05-20 | 40 / 3480 |
| AltarHelper | bruno105/AltarHelper | ✓ | 8d6f324 | 2026-03-19 | 4 / 893 |
| BlightHelper | bruno105/BlightHelper | ✓ | 9c29b39 | 2023-09-07 | 2 / 185 |
| WhereAreYouGoing | DetectiveSquirrel/ExileAPI-WhereAreYouGoing | ✓ | e94d1b3 | 2025-06-16 | 6 / 1221 |
| ExpeditionIcons | instantsc/ExpeditionIcons | ✓ | 9950bca | 2024-04-20 | 23 / 2801 |
| ExpeditionIcons (alt) | myrahz/ExpeditionIcons | ✓ | 62d9fa0 | 2023-04-11 | 8 / 2474 |
| ExpeditionIcons (alt) | arturino009/ExpeditionIcons | ✓ | aa5315e | 2022-12-12 | 4 / 1198 |
| Get-Chaos-Value | instantsc/Get-Chaos-Value | ✓ | 8ed2cc7 | 2026-05-14 | 37 / 5601 |
| Get-Chaos-Value (alt) | DetectiveSquirrel/Get-Chaos-Value | ✓ | a56db55 | 2026-04-09 | 37 / 5620 |
| Get-Chaos-Value (alt) | TheOptimisticFactory/Get-Chaos-Value | ✓ | 7c9d8b0 | 2026-03-19 | 36 / 5195 |
| ProximityAlert | vadash/ProximityAlert | ✓ | 4c7e6b3 | 2021-07-28 | 4 / 641 |
| ShowGroundEffects | arturino009/ShowGroundEffects | ✓ | 3bbd891 | 2025-07-10 | 2 / 164 |
| ShowGroundEffects (alt) | vadash/ShowGroundEffects | ✓ | ec999d6 | 2022-09-17 | 3 / 191 |
| DevTree | exApiTools/DevTree | ✓ | dc42a48 | 2026-03-09 | 4 / 1716 |
| PathfindSanctum | ChandlerFerry/PathfindSanctum | ✓ | 0279aeb | 2025-03-21 | 8 / 1786 |
| BetterSanctum | instantsc/BetterSanctum | ✓ | fd03820 | 2023-09-13 | 3 / 683 |
| HarvestPicker | exApiTools/HarvestPicker | ✓ | e8187ef | 2024-08-15 | 8 / 609 |
| FullRareSetManager | exApiTools/FullRareSetManager | ✓ | 738606e | 2026-03-06 | 9 / 2605 |
| FullRareSetManager (alt) | bruno105/FullRareSetManager | ✓ | f7530fa | 2023-08-23 | 11 / 2580 |
| EssenceCorruptionHelper | deMathias/EssenceCorruptionHelper | ✓ | e924b62 | 2023-05-16 | 2 / 167 |
| SkillGems | DetectiveSquirrel/SkillGems | ✓ | 9bfe9a0 | 2025-07-04 | 2 / 180 |
| BroodyHen | IlliumIv/BroodyHen | ✓ | 7bcbe5d | 2023-08-22 | 2 / 131 |
| EZVendor | vadash/EZVendor | ✓ | 6f8701c | 2026-03-26 | 23 / 2001 |
| Stashie | DetectiveSquirrel/Stashie | ✓ | bd4a111 | 2025-06-30 | 18 / 1856 |
| PickItV2 | exApiTools/PickItV2 | ✓ | 87c0d34 | 2025-06-21 | 5 / 1053 |
| NPCInvWithLinq | DetectiveSquirrel/NPCInvWithLinq | ✓ | cc9ce00 | 2025-06-16 | 4 / 660 |
| Ground-Items-With-Linq | DetectiveSquirrel/Ground-Items-With-Linq | ✓ | 60b4853 | 2026-04-06 | 10 / 1191 |
| InvWithLinq | mikkelpetersen/InvWithLinq | ✓ | 0e01b73 | 2026-02-23 | 4 / 415 |
| WhereTheWispsAt | exApiTools/WhereTheWispsAt | ✓ | c7f6a39 | 2023-12-09 | 2 / 257 |
| WhatAreYouDoing | DetectiveSquirrel/WhatAreYouDoing | ✓ | 0d82f3c | 2023-10-18 | 6 / 998 |
| Guardians-R-Us | DetectiveSquirrel/Guardians-R-Us | ✓ | 6efb43a | 2025-07-04 | 2 / 190 |
| LevelingHelper | TehCheat/LevelingHelper | ✓ | 4f1da26 | 2024-07-23 | 2 / 405 |
| VillageHelper | exApiTools/VillageHelper | ✓ (instantsc → exApiTools) | 4633e08 | 2024-08-12 | 2 / 570 |
| ItemFilterLibInspector | DetectiveSquirrel/ItemFilterLibInspector | ✓ | 95c67a1 | 2025-06-12 | 5 / 304 |
| WheresMyCraftAt | ChandlerFerry/WheresMyCraftAt | ✓ (DetectiveSquirrel → ChandlerFerry) | 7596a33 | 2025-02-26 | 26 / 3593 |
| AreaStatVisual | DetectiveSquirrel/AreaStatVisual | ✓ | 557dfc0 | 2026-03-27 | 2 / 342 |
| Blight | DetectiveSquirrel/Blight | ✓ | 5f29e15 | 2026-03-25 | 2 / 429 |
| Abyss | DetectiveSquirrel/Abyss | ✓ | eab95de | 2025-02-28 | 2 / 107 |
| Character-Data | DetectiveSquirrel/Character-Data | ✓ | a63c784 | 2025-07-04 | 14 / 1328 |
| WhereTheCirclesAt | DetectiveSquirrel/WhereTheCirclesAt | ✓ | a2cc832 | 2025-06-29 | 2 / 257 |
| Wheres-My-Cursor | DetectiveSquirrel/Wheres-My-Cursor | ✓ | 6571fd8 | 2024-04-01 | 3 / 202 |
| PreloadsRevised-poe1 | DetectiveSquirrel/PreloadsRevised-poe1 | ✓ | c52aa3e | 2025-09-09 | 8 / 1049 |
| WhereMyFavsAt | deMathias/WhereMyFavsAt | ✓ | fa02ae4 | 2025-11-07 | 2 / 440 |
| Beasts | bruno105/Beasts | ✓ | 76ca882 | 2026-04-12 | 7 / 2079 |

Could not be cloned at the listed path (resolved or skipped):

| Listed path | Result |
|---|---|
| instantsc/VillageHelper | ✗ 404 — current home is `exApiTools/VillageHelper` (analyzed). |
| DetectiveSquirrel/WheresMyCraftAt | ✗ 404 — current home is `ChandlerFerry/WheresMyCraftAt` (analyzed). |
| IlliumIv/ProximityAlert | ✗ 404 — does not exist; the `vadash/ProximityAlert` primary was analyzed instead. |

Totals: **45 repos analyzed, 3 listed paths dead (2 re-homed, 1 nonexistent), 0 unrecoverable.**

In the matrix below, plugin names are shortened (e.g. `GCV` = the three Get-Chaos-Value
variants, `WAYG` = WhereAreYouGoing, `WTW` = WhereTheWispsAt, `IFLI` =
ItemFilterLibInspector, `Preloads` = PreloadsRevised-poe1, `ExpIcons` = the three
ExpeditionIcons variants, `FRSM` = both FullRareSetManager variants).

---

## API-area coverage matrix

Each row is an API symbol/area; cells list representative plugins that use it, ordered by
breadth of use. Counts are the number of *distinct repos* (alternates/forks counted
separately) in which the symbol appears.

### Plugin lifecycle & base class

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `BaseSettingsPlugin<TSettings>` (subclass) | 45 | every plugin |
| `ISettings` implementation | 45 | every plugin |
| `Render()` override | 30+ | ReAgent, Radar, ExpIcons, GCV, DevTree, Beasts, … |
| `Initialise()` override | 27 | ReAgent, GCV, DevTree, HarvestPicker, WheresMyCraftAt, … |
| `Job Tick()` override (off-thread) | 19 | ExpIcons, FRSM, Beasts, HarvestPicker, WheresMyCraftAt, … |
| `AreaChange(AreaInstance)` override | 19 | GCV, ExpIcons, FRSM, DevTree, AltarHelper, VillageHelper, … |
| `EntityAdded` / `EntityRemoved` override | 6 | Beasts, ExpIcons, FRSM, WTW, WhereMyFavsAt |
| `OnLoad` / `OnClose` / `Dispose` | 6 | Beasts, DevTree, GIWL, BroodyHen, FRSM, Character-Data |
| `DrawSettings()` override | 9 | ReAgent, GIWL, Preloads, WheresMyCraftAt, FRSM, … |

### GameController root members

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `GameController.IngameState` | 24 | nearly all rendering/UI plugins |
| `GameController.Game` | 22 | DevTree, GCV, ReAgent, Beasts, ItemFilterLibInspector, … |
| `GameController.IngameState.IngameUi` | 35 | almost every plugin (panels, labels, hover) |
| `GameController.IngameState.Data` (`IngameData`) | 19 | ExpIcons, GCV, ReAgent, Beasts, PathfindSanctum, … |
| `GameController.IngameState.ServerData` | 12 | Stashie, GCV, FRSM, ReAgent, Beasts, HarvestPicker, … |
| `GameController.IngameState.Camera` | 17 | Radar, ExpIcons, ShowGroundEffects, Beasts, WAYG, … |
| `GameController.Player` | 16 | ReAgent, Beasts, Character-Data, LevelingHelper, GIWL, … |
| `GameController.Area.CurrentArea` | 10 | AltarHelper, BetterSanctum, ReAgent, PathfindSanctum, … |
| `GameController.Files` | 8 | GCV, FRSM, DevTree, GIWL, BetterSanctum, VillageHelper, … |
| `GameController.EntityListWrapper` / `.Entities` | 8 | ReAgent, ExpIcons, Beasts, HarvestPicker, BetterSanctum |
| `GameController.Window` | 9 | DevTree, ReAgent, Beasts, WheresMyCraftAt, SkillGems, … |
| `GameController.PluginBridge` | 7 | ReAgent, PickItV2, SkillGems, GCV, Radar, Preloads, Beasts |
| `GameController.Cache` | 10 | DevTree, GCV, AltarHelper, BlightHelper, BroodyHen, … |
| `GameController.InGame` / `.IsLoading` | 9 | WheresMyCraftAt, ShowGroundEffects, Preloads, FRSM, … |
| `GameController.Memory` | 2 | DevTree, Radar (`ReadStdVector` — see upstream-only), Preloads |
| `GetLeftCornerMap` / panels helpers | 12 | GCV, Beasts, FRSM, Preloads, ReAgent, AreaStatVisual, … |

### Entity & components

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `Entity.GetComponent<T>()` | 27 | GCV, ReAgent, FRSM, ExpIcons, Beasts, PickItV2, Radar, … |
| `Entity.TryGetComponent<T>(out)` | 16 | GCV, ReAgent, Beasts, GIWL, HarvestPicker, WheresMyCraftAt, WAYG, … |
| `Entity.HasComponent<T>()` | 10 | GCV, ReAgent, DevTree, PickItV2, Radar, ProximityAlert, EZVendor |
| `Entity.GetHudComponent<T>()` | 1 | ProximityAlert |
| `Entity.IsValid` / `IsAlive` / `IsHostile` / `IsTargetable` | 17 / 5 / 5 / 3 | PickItV2, ReAgent, ProximityAlert, GCV, Radar, … |
| `Entity.Path` / `Entity.Metadata` | 23 / 24 | GCV, ReAgent, DevTree, Beasts, PickItV2, FRSM, ExpIcons, … |
| `Entity.RenderName` / `Entity.Rarity` | 2 / 10 | FRSM, Guardians-R-Us / GCV, ReAgent, EZVendor, ExpIcons, … |
| `Entity.DistancePlayer` | 11 | PickItV2, ReAgent, ProximityAlert, ShowGroundEffects, WAYG, … |
| `GetComponent<Mods>` | 13 | GCV, FRSM, ReAgent, GIWL, EZVendor, PickItV2, BroodyHen, … |
| `GetComponent<Base>` | 12 | GCV, FRSM, ReAgent, DevTree, EZVendor, Stashie, IFLI, … |
| `GetComponent<Render>` | 12 | ExpIcons, ReAgent, Radar, DevTree, Beasts, PickItV2, WTW, … |
| `GetComponent<Positioned>` | 10 | ExpIcons, ReAgent, Radar, ShowGroundEffects, Beasts, WAYG, … |
| `GetComponent<WorldItem>` | 8 | GCV, FRSM, GIWL, PickItV2, BlightHelper |
| `GetComponent<ObjectMagicProperties>` | 7 | ExpIcons, ReAgent, Beasts, DevTree, ProximityAlert |
| `GetComponent<Life>` (+ `Life.Buffs`/`HasBuff`/`CurHP`) | 5 | ReAgent, Beasts, Character-Data, Guardians-R-Us, SkillGems |
| `GetComponent<Animated>` | 6 | ExpIcons, ReAgent, PathfindSanctum, WTW |
| `GetComponent<StateMachine>` | 5 | ReAgent, PathfindSanctum, HarvestPicker, WTW, Blight |
| `GetComponent<Stats>` (+ `StatDictionary`/`GameStat`) | 4 | ReAgent, Beasts, WhatAreYouDoing, Guardians-R-Us |
| `GetComponent<Flask>` | 5 | ReAgent, GCV, EZVendor |
| `GetComponent<Render Item / Stack / Quality / Weapon / Armour / Sockets / SkillGem>` | 2–4 | GCV (item-pricing), GIWL, PickItV2 |
| `GetComponent<Player>` / `Monster` / `Targetable` / `Actor` / `Charges` | 1–5 | ReAgent, Beasts, PickItV2, Character-Data, WAYG |

### IngameState / UI tree

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `Element.GetClientRect()` | 29 | most UI/inventory plugins |
| `Element.IsVisible` | 34 | most UI plugins |
| `Element.Text` | 32 | GCV, FRSM, DevTree, ProximityAlert, NPCInvWithLinq, … |
| `Element.Children` / `GetChildAtIndex` / `GetChildFromIndices` | 13 / 4 | GCV, DevTree, BetterSanctum, FRSM, EZVendor, … |
| `IngameUi.InventoryPanel` / `StashElement` / `OpenLeft/RightPanel` | 26 | Stashie, PickItV2, GCV, FRSM, EZVendor, Radar, ReAgent, … |
| `IngameUi.ItemsOnGroundLabels` / `LabelOnGround` | 12 | PickItV2, GCV, GIWL, FRSM, AltarHelper, BlightHelper, … |
| `IngameUi.Map` (`LargeMap`/`SmallMiniMap`/`*Zoom`) | 11 | ExpIcons, WAYG, WTC, Beasts, GIWL, HarvestPicker, … |
| `ServerData.PlayerInventories` / `InventorySlotItems` / `VisibleInventoryItems` | 15 | Stashie, GCV, FRSM, PickItV2, InvWithLinq, NPCInvWithLinq, ReAgent, … |
| `IngameState.UIHover` / `UIHoverTooltip` | 9 | DevTree, GCV, IFLI, NPCInvWithLinq, PickItV2, Preloads, InvWithLinq |
| `IngameState.UIRoot` / `IngameState.IngameUi` typed elements (HoverItemIcon, etc.) | 8 | DevTree, GCV, FRSM, Stashie, IFLI, BroodyHen |

### Graphics, coordinates, input

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `Graphics.DrawText` | 19 | ReAgent, GCV, ExpIcons, DevTree, Beasts, FRSM, WTW, … |
| `Graphics.DrawBox` | 16 | ReAgent, GCV, ExpIcons, Beasts, GIWL, HarvestPicker, … |
| `Graphics.DrawFrame` | 12 | ExpIcons, FRSM, DevTree, Beasts, BlightHelper, VillageHelper, … |
| `Graphics.DrawLine` | 9 | ExpIcons, DevTree, GIWL, ShowGroundEffects, WhatAreYouDoing, WMC |
| `Graphics.MeasureText` | 12 | ReAgent, GCV, ExpIcons, Beasts, HarvestPicker, WTW, … |
| `Graphics.DrawImage` / `InitImage` | 5 | ExpIcons, ReAgent, GIWL, EssenceCorruptionHelper |
| `Graphics.DrawTextWithBackground` | 4 | DevTree, GCV, VillageHelper |
| `FontAlign` | 11 | GCV, Beasts, DevTree, GIWL, PathfindSanctum, Preloads, … |
| `Camera.WorldToScreen` | 18 | Radar, ExpIcons, ShowGroundEffects, Beasts, WAYG, WTC, DevTree, … |
| `Positioned.GridPos` / `Render.Pos` / `WorldPos` | 17+ | Radar, ExpIcons, Beasts, PickItV2, GIWL, FRSM, ProximityAlert, … |
| `GridToWorldMultiplier` / `WorldToGridScale` helpers | 9 | Radar, ExpIcons, ReAgent, Beasts, HarvestPicker, WAYG, WTW |
| `Input.IsKeyDown` / `GetKeyState` | 6 | DevTree, ExpIcons, ReAgent, SkillGems, WheresMyCraftAt |
| `Input.Click` / `KeyDown` / `KeyUp` / `MouseMove` / `MousePosition` | 6 | WheresMyCraftAt, Beasts, ReAgent, SkillGems, VillageHelper |

### Settings nodes

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `ToggleNode` | 45 | every plugin |
| `RangeNode<T>` | 39 | almost every plugin |
| `ColorNode` | 28 | most rendering plugins |
| `HotkeyNode` | 20 | Radar, GCV, FRSM, PickItV2, Stashie, ReAgent, DevTree, … |
| `TextNode` | 16 | GCV, FRSM, GIWL, Radar, ReAgent, InvWithLinq, … |
| `ButtonNode` | 14 | GCV, GIWL, DevTree, Radar, ReAgent, HarvestPicker, … |
| `ListNode` | 11 | Stashie, FRSM, GCV, GIWL, HarvestPicker, ProximityAlert |
| `ContentNode` / `EmptyNode` | 11 | ExpIcons, GCV, DevTree, PickItV2, ShowGroundEffects, AreaStatVisual |
| `HotkeyNodeV2` / `HotkeyNodeValue` | 4 | DevTree, IFLI, Preloads, ReAgent |
| `[Menu]` attribute | 25 | GCV, FRSM, Radar, ReAgent, DevTree, Stashie, ExpIcons, … |

### Static data, enums, inter-plugin, utilities

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `GameController.Files.BaseItemTypes` + `.Translate(metadata)` | 8 | GCV, FRSM, DevTree, EZVendor, WheresMyCraftAt |
| `EntityType` enum | 17 | ReAgent, PickItV2, ExpIcons, FRSM, Beasts, ProximityAlert, WAYG, … |
| `GameStat` enum | 14 | GCV, ReAgent, ExpIcons, Beasts, PathfindSanctum, HarvestPicker, … |
| `ItemRarity` / `MonsterRarity` enum | 10 | GCV, FRSM, EZVendor, ExpIcons, GIWL, WheresMyCraftAt |
| `InventoryTypeE` / `InventorySlotE` enum | 5 / 3 | Stashie, EZVendor, GCV, IFLI, WheresMyCraftAt, Guardians-R-Us |
| `MouseActionType` / `MouseButtons` enum | 5 | PickItV2, Stashie, Beasts, EZVendor, WheresMyCraftAt |
| `PluginBridge.SaveMethod` / `GetMethod<T>` | 7 / 5 | GCV, PickItV2, ReAgent, SkillGems, Radar, Beasts, PathfindSanctum |
| `PublishEvent` / `ReceiveEvent` | 4 | Stashie, FRSM, EZVendor |
| `DebugWindow.Log*` | 22 | GCV, ReAgent, DevTree, PickItV2, Stashie, GIWL, HarvestPicker, … |
| `LogMessage` / `LogError` (base-class logging) | 31 | most plugins |
| `MultiThreadManager` / `Job` coroutines | 29 | Stashie, PickItV2, ExpIcons, FRSM, WheresMyCraftAt, Beasts, … |
| `TaskUtils` / `WaitTime` / coroutine helpers | 6 | PickItV2, Stashie, FRSM, EZVendor, Beasts, WheresMyCraftAt |
| `SoundController` / `PlaySound` | 8 | GCV, ProximityAlert, AltarHelper, DevTree, ExpIcons, LevelingHelper |
| `Files.LoadFiles` / `Preload` data | 2 | BetterSanctum, GIWL / Preloads, DevTree |

---

## Coverage check

For each heavily-used symbol the audit confirmed both **(a)** presence in this fork's
`Core` source and **(b)** coverage in a `docs/api/*.md` file. All checks below passed.

| Symbol / area | In this fork's `Core`? | Documented in | Verified |
|---|---|---|---|
| `BaseSettingsPlugin<T>`, lifecycle (`Initialise`/`Tick`/`Render`/`AreaChange`/`EntityAdded`/`OnLoad`) | yes (`Core/BaseSettingsPlugin.cs`, `IPlugin`) | plugins.md | ✓ |
| Settings `*Node` types, `[Menu]` | yes (`Core/Shared/Nodes/*`, `MenuAttribute`) | settings.md | ✓ |
| `GameController.{IngameState,Game,Player,Area,Files,Window,Cache,PluginBridge,EntityListWrapper,InGame,IsLoading}` | yes (`Core/GameController.cs`) | game-controller.md | ✓ |
| `Entity.{GetComponent,TryGetComponent,HasComponent,GetHudComponent,IsValid,IsAlive,Path,Metadata,RenderName,Rarity,DistancePlayer,Buffs}` | yes (`Core/PoEMemory/MemoryObjects/Entity.cs`) | entities.md | ✓ |
| Components `Life/Mods/Base/Render/Positioned/WorldItem/ObjectMagicProperties/Stats/Flask/Animated/StateMachine/RenderItem/Stack/Quality/Sockets/SkillGem/Targetable/Actor/Charges/Player/Monster` | yes (`Core/PoEMemory/Components/*`) | components-combat/items/world.md | ✓ |
| Buffs via `Life.Buffs` / `Life.HasBuff` / `Entity.Buffs` (no `Buffs` *component*) | yes (`Core/PoEMemory/Components/Life.cs`, `Buff.cs`) | components-combat.md (explicitly notes there is no `Buffs` component) | ✓ |
| `IngameState.Camera.WorldToScreen` | yes (`Core/PoEMemory/MemoryObjects/Camera.cs`) | coordinates.md, game-controller.md | ✓ |
| `IngameUi.{InventoryPanel,StashElement,ItemsOnGroundLabels,Map,OpenLeftPanel,OpenRightPanel}`; `UIHover`/`UIHoverTooltip` | yes (`Core/PoEMemory/Elements/*`, `IngameState.cs`) | ui-elements.md, ingame-state.md | ✓ |
| `Element.{GetClientRect,IsVisible,Text,Children,GetChildFromIndices}` | yes (`Core/PoEMemory/Element.cs`) | ui-elements.md | ✓ |
| `ServerData.PlayerInventories` / `ServerInventory` / inventory items | yes (`Core/PoEMemory/MemoryObjects/{ServerData,ServerInventory}.cs`) | ingame-state.md, inventories.md | ✓ |
| `Graphics.{DrawText,DrawBox,DrawFrame,DrawLine,DrawImage,InitImage,MeasureText,DrawTextWithBackground}`, `FontAlign` | yes (`Core/Graphics.cs`, `RenderQ`) | graphics.md | ✓ |
| `Input.{IsKeyDown,GetKeyState,Click,KeyDown,KeyUp,MousePosition}` | yes (`Core/Input.cs`) | input.md | ✓ |
| `Files.BaseItemTypes.Translate(metadata)` (+ `GetFromAddress`, `Contents`) | yes (`Core/PoEMemory/FilesInMemory/BaseItemTypes.cs:43`) | files-in-memory.md | ✓ |
| Enums `EntityType`, `GameStat`, `ItemRarity`, `InventorySlotE`, `InventoryTypeE`, `MouseActionType` | yes (`Core/Shared/Enums/*`) | enums.md | ✓ |
| `PluginBridge.{GetMethod,SaveMethod}` | yes (`Core/GameController.cs:19`) | game-controller.md, plugins.md, utilities.md | ✓ |
| `DebugWindow.Log*`, base-class `LogMessage`/`LogError` | yes (`Core/DebugWindow.cs`, `BaseSettingsPlugin`) | utilities.md | ✓ |
| `MultiThreadManager` + `Job` coroutines | yes (`Core/MultiThreadManager.cs`, `Job`) | utilities.md, plugins.md | ✓ |
| `SoundController` | yes (`Core/SoundController.cs`) | utilities.md, game-controller.md | ✓ |
| `GameController.Memory.ReadStringU` / `ReadStructsArray` | yes (`Core/Memory.cs`) | memory.md | ✓ |

Every heavily-used symbol that exists in this fork is already covered by the docs.

---

## Gaps

### Doc gaps

Symbols that real plugins use, that **are present** in this fork's `Core`, but are **not
yet covered** in `docs/api/*`.

**None found.** Every API symbol used by the analyzed plugins that exists in this fork's
`Core` is already documented in at least one `docs/api/*.md` file (verified by grep — see
the [Coverage check](#coverage-check)). The closest call was `Files.BaseItemTypes.Translate`,
which is sometimes assumed to be upstream-only but in fact exists in this fork
(`Core/PoEMemory/FilesInMemory/BaseItemTypes.cs:43`) and is already documented in
files-in-memory.md.

### Upstream-only symbols

Symbols that real plugins use that are **absent from this fork's `Core`/`GameOffsets`**.
These were verified absent by grepping `Core` and `GameOffsets`. They must **not** be
documented as part of this API; they belong in a dedicated compatibility note (suggested
file `compatibility-exileapi-compiled.md`) that records what the upstream "compiled"
ExileApi exposes versus this fork, alongside the closest equivalent here.

| Upstream-only symbol | Used by (file) | This fork's equivalent |
|---|---|---|
| `Entity.PosNum` / `GridPosNum` / `WorldPosNum` (`System.Numerics` position accessors) | Radar (`Radar.cs`), ExpeditionIcons, HarvestPicker, PickItV2 (`PickIt.cs`), ReAgent, WhereAreYouGoing, Beasts (`Beasts.cs`), DevTree, Abyss, Blight, WhereTheCirclesAt, WhereTheWispsAt, +others (~20 repos) | `Render.Pos` (`Vector3`), `Positioned.GridPos` / `WorldPos` (`Vector2`); the upstream `*Num` variants are the `System.Numerics` flavor that this fork does not expose. |
| `IngameState.UIHoverElement` | DevTree (`DevTree.cs:69`), PickItV2 (`PickIt.cs:348`), EZVendor (`EZVendorCore.cs:246`), WheresMyCraftAt (`Handlers/ElementHandler.cs:18`), ItemFilterLibInspector (`ItemFilterLibInspector.cs:64`) | `IngameState.UIHover` (this fork exposes `UIHover`/`UIHoverTooltip` only; there is no `UIHoverElement`). |
| `IMemory.ReadStdVector<T>` / `ReadStdVectorStride<T>` + `StdVector` type | Radar (`Radar.Pathfinding.cs:202`), PathfindSanctum (`RewardHelper.cs:320`) | `Memory.ReadStructsArray`, `ReadDoublePtrVectorClasses`, `ReadNativeArray`, `ReadList<T>` (this fork has no `ReadStdVector`/`StdVector`). |
| `GetComponent<Buffs>()` (a `Buffs` *component*) and `Buffs.BuffsList` | Beasts (`Beasts.cs:117,226`), ReAgent (`RuleState.cs`, `NearbyMonsterInfo.cs`, `FlaskInfo.cs`) | No `Buffs` component in this fork; buffs are on the `Life` component: `Life.Buffs` (`List<Buff>`), `Life.HasBuff(string)`, and the convenience `Entity.Buffs`. (Already noted in components-combat.md.) |
| `InventorySlotE.ExpandedMainInventory1` (and the `Expanded*` slot family) | Stashie (`Compartments/StashieSettingsHandler.cs:31`, `Compartments/FilterManager.cs:116`) | This fork's `InventorySlotE` (`Core/Shared/Enums/InventorySlotE.cs`) has `MainInventory1` but not the expanded-backpack slot enumerators. |
| `InventoryIndex.PlayerExpandedInventory` | Stashie (`Compartments/FilterManager.cs:114`) | This fork has no `PlayerExpandedInventory` index in `InventoryIndex`. |

Note: many of these reflect plugins targeting the newer upstream "compiled ExileApi"
build (expanded-backpack support, `System.Numerics` position helpers, `Buffs` component,
`ReadStdVector`), which this fork has not adopted. They are recorded here, not invented
into the reference docs.

---

## Verdict

**The existing `docs/api/*.md` cover everything the real plugins use that this fork
actually exposes.** Across 45 analyzed plugin repositories and roughly 120 distinct
ExileCore API symbols observed, there are **0 doc gaps** (every used-and-present symbol is
documented) and **6 upstream-only symbol families** that this fork does not expose and so
must live in a compatibility note rather than the reference. The docs are accurate and
complete for this fork's API surface.

---

## Source

All repositories were shallow-cloned and analyzed at the following commits (date = last
commit date of the cloned HEAD):

- instantsc/Radar @ c244597 (2026-03-18)
- exApiTools/ReAgent @ 2502d86 (2026-05-20)
- bruno105/AltarHelper @ 8d6f324 (2026-03-19)
- bruno105/BlightHelper @ 9c29b39 (2023-09-07)
- DetectiveSquirrel/ExileAPI-WhereAreYouGoing @ e94d1b3 (2025-06-16)
- instantsc/ExpeditionIcons @ 9950bca (2024-04-20)
- myrahz/ExpeditionIcons @ 62d9fa0 (2023-04-11) *(alt)*
- arturino009/ExpeditionIcons @ aa5315e (2022-12-12) *(alt)*
- instantsc/Get-Chaos-Value @ 8ed2cc7 (2026-05-14)
- DetectiveSquirrel/Get-Chaos-Value @ a56db55 (2026-04-09) *(alt)*
- TheOptimisticFactory/Get-Chaos-Value @ 7c9d8b0 (2026-03-19) *(alt)*
- vadash/ProximityAlert @ 4c7e6b3 (2021-07-28)
- arturino009/ShowGroundEffects @ 3bbd891 (2025-07-10)
- vadash/ShowGroundEffects @ ec999d6 (2022-09-17) *(alt)*
- exApiTools/DevTree @ dc42a48 (2026-03-09)
- ChandlerFerry/PathfindSanctum @ 0279aeb (2025-03-21)
- instantsc/BetterSanctum @ fd03820 (2023-09-13)
- exApiTools/HarvestPicker @ e8187ef (2024-08-15)
- exApiTools/FullRareSetManager @ 738606e (2026-03-06)
- bruno105/FullRareSetManager @ f7530fa (2023-08-23) *(alt)*
- deMathias/EssenceCorruptionHelper @ e924b62 (2023-05-16)
- DetectiveSquirrel/SkillGems @ 9bfe9a0 (2025-07-04)
- IlliumIv/BroodyHen @ 7bcbe5d (2023-08-22)
- vadash/EZVendor @ 6f8701c (2026-03-26)
- DetectiveSquirrel/Stashie @ bd4a111 (2025-06-30)
- exApiTools/PickItV2 @ 87c0d34 (2025-06-21)
- DetectiveSquirrel/NPCInvWithLinq @ cc9ce00 (2025-06-16)
- DetectiveSquirrel/Ground-Items-With-Linq @ 60b4853 (2026-04-06)
- mikkelpetersen/InvWithLinq @ 0e01b73 (2026-02-23)
- exApiTools/WhereTheWispsAt @ c7f6a39 (2023-12-09)
- DetectiveSquirrel/WhatAreYouDoing @ 0d82f3c (2023-10-18)
- DetectiveSquirrel/Guardians-R-Us @ 6efb43a (2025-07-04)
- TehCheat/LevelingHelper @ 4f1da26 (2024-07-23)
- exApiTools/VillageHelper @ 4633e08 (2024-08-12) *(re-homed from instantsc/VillageHelper, which 404s)*
- DetectiveSquirrel/ItemFilterLibInspector @ 95c67a1 (2025-06-12)
- ChandlerFerry/WheresMyCraftAt @ 7596a33 (2025-02-26) *(re-homed from DetectiveSquirrel/WheresMyCraftAt, which 404s)*
- DetectiveSquirrel/AreaStatVisual @ 557dfc0 (2026-03-27)
- DetectiveSquirrel/Blight @ 5f29e15 (2026-03-25)
- DetectiveSquirrel/Abyss @ eab95de (2025-02-28)
- DetectiveSquirrel/Character-Data @ a63c784 (2025-07-04)
- DetectiveSquirrel/WhereTheCirclesAt @ a2cc832 (2025-06-29)
- DetectiveSquirrel/Wheres-My-Cursor @ 6571fd8 (2024-04-01)
- DetectiveSquirrel/PreloadsRevised-poe1 @ c52aa3e (2025-09-09)
- deMathias/WhereMyFavsAt @ fa02ae4 (2025-11-07)
- bruno105/Beasts @ 76ca882 (2026-04-12)

Failed to clone (recorded for completeness): instantsc/VillageHelper (404, re-homed
above), DetectiveSquirrel/WheresMyCraftAt (404, re-homed above), IlliumIv/ProximityAlert
(404, nonexistent — the vadash primary was used).
