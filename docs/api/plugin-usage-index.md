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
- **Re-verified 2026-07-24**: all 45 repos re-cloned and re-swept from scratch. This pass
  enumerated *every* `GetComponent<T>` and `IngameUi.*` member in the corpus instead of checking
  a hand-picked symbol list, so the gap inventory below is broader than the original. Three repos
  had moved; the other 42 reproduced their recorded commit/date/LOC exactly. See
  [Corrections to the previous audit](#corrections-to-the-previous-audit).
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
| Radar | instantsc/Radar | ✓ | 621a684 | 2026-07-24 | 14 / 1886 |
| ReAgent | exApiTools/ReAgent | ✓ | a328330 | 2026-07-21 | 42 / 5323 |
| AltarHelper | bruno105/AltarHelper | ✓ | 8d6f324 | 2026-03-19 | 4 / 893 |
| BlightHelper | bruno105/BlightHelper | ✓ | 9c29b39 | 2023-09-07 | 2 / 185 |
| WhereAreYouGoing | DetectiveSquirrel/ExileAPI-WhereAreYouGoing | ✓ | e94d1b3 | 2025-06-16 | 6 / 1221 |
| ExpeditionIcons | instantsc/ExpeditionIcons | ✓ | 9950bca | 2024-04-20 | 23 / 2801 |
| ExpeditionIcons (alt) | myrahz/ExpeditionIcons | ✓ | 62d9fa0 | 2023-04-11 | 8 / 2474 |
| ExpeditionIcons (alt) | arturino009/ExpeditionIcons | ✓ | aa5315e | 2022-12-12 | 4 / 1198 |
| Get-Chaos-Value | instantsc/Get-Chaos-Value | ✓ | 7f5fa8c | 2026-06-27 | 37 / 5624 |
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
documented as part of this API; they belong in
[compatibility-exileapi-compiled.md](compatibility-exileapi-compiled.md), which records what the
upstream "compiled" ExileApi exposes versus this fork, alongside the closest equivalent here.

> **Re-verified 2026-07-24 against freshly cloned sources.** All 45 repositories were
> re-cloned and re-grepped from scratch. **45/45 still reachable**; **3 had moved** since the
> original audit (Radar `c244597`→`621a684`, ReAgent `2502d86`→`a328330`, instantsc/Get-Chaos-Value
> `8ed2cc7`→`7f5fa8c` — table above updated). For the other 42 the recorded commit, date, file count
> and LOC reproduced **exactly**, which is a good check on the original method. The re-run also
> widened the sweep from hand-picked symbols to *every* `GetComponent<T>` and `IngameUi.*` member
> used anywhere in the corpus — which surfaced several gaps the first pass missed, and invalidated
> two citations. Both are recorded below rather than quietly dropped.

### Corrections to the previous audit

| Claim | Status after re-verification |
|---|---|
| `Mods.EnchantedMods` used by `stashie/ItemData.cs:109` | **Stale citation.** DetectiveSquirrel/Stashie @ `bd4a111` has no `ItemData.cs` and no `Mods` usage at all; **0 of 45** repos reference `EnchantedMods`. It remains a real ExileApi-Compiled member (and is now implemented here), but no plugin in this corpus exercises it. |
| `IMemory.ReadStdVector<T>` used by `Radar/Radar.Pathfinding.cs:202` | **Stale citation.** Radar has since moved to `621a684` and no longer references `ReadStdVector` at all. The only live user is PathfindSanctum (below). |
| `[Submenu]` / `[ConditionalDisplay]` attributes | **Under-reported.** The first pass filed these only as "types absent from `Shared/Attributes`". They are in fact the 2nd and 3rd most-used settings attributes in the corpus (17 and 8 repos). |
| `ContentNode` / `EmptyNode` counted jointly as 11 repos | **Split.** `ContentNode` alone: 6 repos. `EmptyNode` is present in this fork; `ContentNode` is not. |

### Gap inventory (re-verified 2026-07-24)

Grouped by blocker. See [compatibility-exileapi-compiled.md](compatibility-exileapi-compiled.md)
for the per-member porting notes.

#### Closed since the original audit

| Symbol | Used by | Resolution |
|---|---|---|
| `Entity.PosNum` / `GridPosNum` | 17 / 14 repos | Real instance properties (`Core/PoEMemory/MemoryObjects/Entity.cs`), PR #42. |
| `GetComponent<Buffs>()` / `Buffs.BuffsList` | Beasts, ReAgent | `Core/PoEMemory/Components/Buffs.cs`. On this fork's build the buff vector still lives in `Life`, so the component delegates to the owner's `Life.Buffs` rather than carrying a second build-specific offset. |
| `ReadStdVectorStride<T>` + `StdVector` type | PathfindSanctum (`RewardHelper.cs:320`) | `Core/Shared/Compat/MemoryCompat.cs` + `GameOffsets/Native/StdVector.cs`. The live call site is `M.ReadStdVectorStride<long>(M.Read<StdVector>(addr + 0x70), 0x10)` — an exact match for the implemented `(IMemory, StdVector, int)` overload. |
| `CustomNode` | 10 repos | `Core/Shared/Nodes/CustomNode.cs` + `SettingsParser` wiring. |
| `Element.TryGetChildFromIndices(out Element, params int[])` | ExpeditionIcons (arturino009) | `Core/PoEMemory/Element.cs`. Quiet counterpart to the existing `GetChildFromIndices`, which logs on every miss — the call site sweeps 20 candidate paths in a loop, so a probing API must not spam the log. |

#### Still open — settings attributes & nodes

The largest remaining gap, and the one most likely to block a straight port: 17 of 45 plugins
will not compile here for want of a single attribute.

| Symbol | #repos | Used by (sample) |
|---|---:|---|
| `[Submenu]` (`SubmenuAttribute`) | 17 | Radar, ReAgent, GCV, DevTree, PickItV2, Beasts, ExpIcons, WheresMyCraftAt, Character-Data, GIWL, AltarHelper, Blight, BlightHelper, AreaStatVisual |
| `[ConditionalDisplay]` | 8 | Radar, ExpIcons, PickItV2, Beasts, WheresMyCraftAt, SkillGems, AreaStatVisual |
| `ContentNode<T>` (+`ContentNodeConverter`, `IContentNodeBase`) | 6 | GCV (×3), DevTree, PickItV2, AreaStatVisual |
| `HotkeyNodeV2` / `HotkeyNodeValue` | 5 | Radar, ReAgent, DevTree, IFLI, Preloads |

Not restorable from the client-328.8 reconstruction: it decompiles the two attributes to
non-functional bodies (`ConditionalDisplayAttribute.ConditionMethodName` becomes
`return (string)(object)this;`, and `SubmenuAttribute`'s constructor to three discarded
constants), and stubs 7 of `ContentNode`'s and 10 of `HotkeyNodeV2`'s method bodies. The
attribute *shapes* are obvious enough to rewrite, but they are inert without matching
`SettingsParser`/`MenuWindow` support, which is where the real behaviour lives — so they are
recorded as work, not faked with a no-op attribute that would compile and then silently do
nothing.

#### Still open — components requested via `GetComponent<T>`

12 of the 41 distinct components the corpus requests are absent here.

| Component | #uses | Requested by |
|---|---:|---|
| `MapKey` | 4 | GCV (×3), EZVendor |
| `LocalStats` | 4 | GCV (×3), ReAgent |
| `CapturedMonster` | 4 | GCV (×3), Beasts |
| `UltimatumTrial`, `NecropolisCorpse`, `HeistRewardDisplay`, `BrequelFruit` | 3 each | GCV (×3) |
| `Tincture` | 2 | ReAgent |
| `HarvestWorldObject` | 2 | HarvestPicker |
| `Movement` | 1 | WhatAreYouDoing |
| `AttachedAnimatedObject` | 1 | ReAgent |
| `AnimationController` | 1 | PathfindSanctum |

All are offset-bearing memory components, and the reconstruction's offsets target client 328.8
(its `ModsComponentOffsets.implicitMods` is `0xC0` where this fork's is `0x90`), so they cannot be
ported as literals — each needs an offset dumped against the client this fork targets.

#### Still open — `IngameUi` members

Beyond the league-panel families already listed in the compatibility doc (Ritual, Sanctum,
Village, Ultimatum, Expedition, Heist, Ancestor, Necropolis…), these **general-purpose** members
are used broadly and have no league-specific excuse:

| Member | #repos | Used by (sample) |
|---|---:|---|
| `FullscreenPanels` | 11 | Radar, ReAgent, PickItV2, Beasts, WAYG, Blight, Character-Data, AreaStatVisual, GIWL, WTW, WhereTheCirclesAt |
| `LargePanels` | 10 | Radar, ReAgent, PickItV2, Beasts, WAYG, Blight, Character-Data, AreaStatVisual, WTW, WhereTheCirclesAt |
| `ItemsOnGroundLabelsVisible` | 7 | GCV (×3), PickItV2, AltarHelper, BlightHelper, EssenceCorruptionHelper |
| `ChatTitlePanel` | 7 | GCV (×3), ReAgent, PickItV2, Character-Data, SkillGems |
| `SellWindowHideout` | 6 | GCV (×3), FRSM (×2), EZVendor |
| `PurchaseWindowHideout` | 5 | GCV (×3), IFLI, NPCInvWithLinq |
| `QuestRewardWindow` | 2 | IFLI, NPCInvWithLinq |

`FullscreenPanels` / `LargePanels` are the idiomatic "is a blocking panel open?" check; without
them every plugin (and this fork's own consumers) hand-rolls a list of individual panels. They are
absent from the reconstruction too, so implementing them needs a dumped child-index/offset path
rather than a port.

Also still null-stubbed here rather than absent: `IngameUi.TradeWindow` (3 repos),
`NpcDialog`, `MapStashTab`, `WorldMap` — the members exist so call sites compile, but return
`null` pending a verified offset (see `Core/PoEMemory/MemoryObjects/IngameUIElements.cs`).

#### Still open — miscellaneous

| Symbol | Used by | Note |
|---|---|---|
| `InventorySlotE.Expanded*` family | Stashie (`Compartments/StashieSettingsHandler.cs:31`, `FilterManager.cs:116`) | Citation re-confirmed. Absent from the reconstruction too; the values index live inventory memory, so a guess would silently read the **wrong** inventory rather than fail. |
| `InventoryIndex.PlayerExpandedInventory` | Stashie (`FilterManager.cs:114`) | Same; citation re-confirmed. |
| `Positioned.WorldPosNum` as a **property** | ExpeditionIcons (instantsc, myrahz) | This fork ships it as an extension *method* (`Core/Shared/Compat/NumericsCompat.cs`), so `?.WorldPosNum` call sites need `.WorldPosNum()`. Friction, not absence. |
| `FontAlign.VerticalCenter` | DevTree, ProximityAlert | Absent from the reconstruction too. Adding it as a flag would mean renumbering an enum that is persisted in settings files and read by existing call sites. |

---

## Verdict

**The reference docs remain accurate for what this fork exposes**, but the original "0 doc gaps /
5 upstream-only families" verdict was too narrow: it was drawn from a hand-picked symbol list. The
2026-07-24 re-run swept every `GetComponent<T>` and `IngameUi.*` in the corpus and found the gap
set is **larger and differently shaped** than recorded — the dominant blocker is not memory
offsets but the **settings-attribute surface** (`[Submenu]` alone would stop 17 of 45 plugins from
compiling), followed by 12 absent components and ~7 general-purpose `IngameUi` members.

Five gaps have been closed since (`Entity.PosNum`/`GridPosNum`, the `Buffs` component,
`ReadStdVectorStride`/`StdVector`, `CustomNode`, `TryGetChildFromIndices`). Two previously cited
call sites turned out to be stale and are corrected above. Everything still open is recorded here
with its blocker, and is *not* invented into the reference docs.

---

## Source

All repositories were shallow-cloned and analyzed at the following commits (date = last
commit date of the cloned HEAD):

- instantsc/Radar @ 621a684 (2026-07-24)
- exApiTools/ReAgent @ a328330 (2026-07-21)
- bruno105/AltarHelper @ 8d6f324 (2026-03-19)
- bruno105/BlightHelper @ 9c29b39 (2023-09-07)
- DetectiveSquirrel/ExileAPI-WhereAreYouGoing @ e94d1b3 (2025-06-16)
- instantsc/ExpeditionIcons @ 9950bca (2024-04-20)
- myrahz/ExpeditionIcons @ 62d9fa0 (2023-04-11) *(alt)*
- arturino009/ExpeditionIcons @ aa5315e (2022-12-12) *(alt)*
- instantsc/Get-Chaos-Value @ 7f5fa8c (2026-06-27)
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
