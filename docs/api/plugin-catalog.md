# ExileApi plugin catalog

A categorized survey of community ExileApi/ExileCore plugins, gathered to find reusable
building blocks and worked examples for plugin authors of this fork. For the API itself see
the [API reference index](README.md); the [plugin usage index](plugin-usage-index.md) maps
API symbols to the plugins that exercise them.

This is a **list**, not a tutorial. Three sources are merged here (see [Source](#source)):

1. **exApiTools** — the org that hosts the actively-maintained .NET 10 ports most of this
   community runs (66 repos).
2. **DetectiveSquirrel** — prolific author of the `*WithLinq` family and many small
   awareness/QoL plugins; also maintainer of the shared `ItemFilterLibrary`.
3. **PluginBrowser** ([instantsc.github.io/PluginBrowser](https://instantsc.github.io/PluginBrowser))
   — the curated *endorsed* list (38 entries) driving the in-app plugin installer.

Conventions used in the tables:

- **Fork?/Archived?** — `fork` = a fork (usually a .NET 10 port of an older plugin),
  `archived` = repo marked read-only. Blank = original, active.
- **Endorsed** — the author the PluginBrowser endorses for that plugin (the recommended fork
  to install), when the plugin is on the endorsed list. Blank = not on the endorsed list.
- **✦** in "Reusable" marks a library or technique worth lifting into our docs/cookbook,
  not just an end-user plugin. These are pulled together under
  [Shared libraries / building blocks](#shared-libraries--building-blocks).

> Note on accuracy: exApiTools repo metadata (fork/archived) was confirmed for the subset
> returned by the authenticated repo search; the remainder of the 66 are listed from the
> known org inventory and marked *(unconfirmed)* where status could not be checked.
> DetectiveSquirrel's repos were enumerated from his public profile (he has since archived
> most PoE1 plugins and produced `-PoE2` variants). `ItemFilterLibrary` is referenced by
> every `*WithLinq` plugin but is not currently exposed as a standalone repo under his
> account — see the note in the library section.

## Rendering / overlays & icons

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| Radar | exApiTools/Radar | | instantsc | Full map outline + terrain/target lines | |
| Radar | instantsc/Radar | | instantsc | Upstream of the above (endorsed source) | |
| MinimapIcons | exApiTools/MinimapIcons | | | Renders entity icons on the minimap/world | ✦ |
| IconsBuilder | exApiTools/IconsBuilder | fork (TehCheat), archived | | Shared icon-building lib (Chest/Monster/Player icons) | ✦ |
| HeistIcons | exApiTools/HeistIcons | | | Heist contract/chest icons | |
| ExpeditionIcons | exApiTools/ExpeditionIcons | | instantsc | Expedition world-UI improvements + icons | |
| ExpeditionIcons | instantsc/ExpeditionIcons | | instantsc | Upstream (endorsed source) | |
| ProximityAlert | vadash/ProximityAlert | fork | vadash | Mod/metadata path warnings drawn on screen | |
| ProximityAlert | IlliumIv/ProximityAlert | | vadash | Upstream proximity warnings | |
| ShowGroundEffects | arturino009/ShowGroundEffects | | arturino009 | Circles around ground-effect zones | |
| ShowGroundEffects | vadash/ShowGroundEffects | fork | arturino009 | Port of ShowGroundEffects | |
| HealthBars | exApiTools/HealthBars | | | Custom monster/player health bars | |
| EliteBar | exApiTools/EliteBar | | | Rare/unique monster status bar | |
| XpBar | exApiTools/XpBar | | | XP-per-hour bar overlay | |
| ProximityAlert | exApiTools/ProximityAlert | fork (Arecurius0) | vadash | Org port of proximity warnings | |
| ProjectileTracker | exApiTools/ProjectileTracker | | | Visualizes enemy projectiles | |
| WhereTheWispsAt | exApiTools/WhereTheWispsAt | | exApiTools | Affliction wisp highlighting | |
| WhereTheWispsAt | DetectiveSquirrel/WhereTheWispsAt | archived | exApiTools | Upstream wisp highlighter | |
| WhereTheCirclesAt | DetectiveSquirrel/WhereTheCirclesAt | archived | DetectiveSquirrel | Distance circles around the player | |
| WhereAreYouGoing | DetectiveSquirrel/ExileAPI-WhereAreYouGoing | archived | DetectiveSquirrel | Draws monster pathing | |
| Wheres-My-Cursor | DetectiveSquirrel/Wheres-My-Cursor | archived | DetectiveSquirrel | Line from player to cursor | |
| AreaStatVisual | DetectiveSquirrel/AreaStatVisual | | DetectiveSquirrel | Popups for tracked area/map-stat mods | |
| WhereIsMyShrine | DetectiveSquirrel/WhereIsMyShrine | archived | | Shrine awareness/highlighting | |
| WhereTheRaysAt | DetectiveSquirrel/WhereTheRaysAt | archived | | Beam/ray hazard awareness | |
| WhereTheGridAt | DetectiveSquirrel/WhereTheGridAt | archived | | Grid overlay debug | |
| ItemVisualizer | DetectiveSquirrel/ItemVisualizer | | | Draws frames around items by rarity | |
| ProximityAlert / TC_MiscInformation | exApiTools/TC_MiscInformation | fork (TehCheat) | | Misc HUD info readout | |

## Item filtering & loot

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| ItemFilter | exApiTools/ItemFilter | | | Item-filter highlighting on the ground | |
| ItemFilterExamples | exApiTools/ItemFilterExamples | | | Example IFL rule sets (docs/data) | |
| HighlightedItems | exApiTools/HighlightedItems | | | Highlights/quick-stashes matching items | |
| HighlightedItems | DetectiveSquirrel/HighlightedItems | fork (exApiTools) | | Port of HighlightedItems | |
| GroundItemsWithLinq | DetectiveSquirrel/Ground-Items-With-Linq | | DetectiveSquirrel | Ground items shown via ItemFilterLibrary rules | |
| Ground-Items-With-Linq | exApiTools/Ground-Items-With-Linq | fork (DetectiveSquirrel) | | Org port of the above | |
| NPCInventoryWithLinq | DetectiveSquirrel/NPCInvWithLinq | archived | DetectiveSquirrel | NPC vendor items via IFL rules | |
| NPCInvWithLinq | exApiTools/NPCInvWithLinq | fork (DetectiveSquirrel) | | Org port | |
| InventoryItemsWithLinq | mikkelpetersen/InvWithLinq | | mikkelpetersen | Inventory items via IFL rules | |
| IFL Inspector | DetectiveSquirrel/ItemFilterLibInspector | archived | DetectiveSquirrel | Inspect items as ItemFilterLibrary objects | ✦ |
| MapModHighlight | exApiTools/MapModHighlight | | | Highlights/labels dangerous map mods | |
| AdvancedTooltip | exApiTools/AdvancedTooltip | | | Adds DPS/iLvl/quality detail to item tooltips | |
| HarvestPicker | exApiTools/HarvestPicker | | exApiTools | Calculates/labels harvest plot values | |
| HarvestForge | exApiTools/HarvestForge | | | Harvest crafting helper | |

## Inventory / stash / vendor

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| Stashie | DetectiveSquirrel/Stashie | fork, archived | DetectiveSquirrel | Rule-based stash sorting/dumping | |
| Stashie | exApiTools/Stashie | fork (Arecurius0) | DetectiveSquirrel | Org port of Stashie | |
| StashieV2 | exApiTools/StashieV2 | | | Rewritten Stashie | |
| FullRareSetManager | exApiTools/FullRareSetManager | | exApiTools | Chaos/regal recipe set manager | |
| FullRareSetManager | bruno105/FullRareSetManager | fork | exApiTools | Port of FRSM | |
| EZVendor | vadash/EZVendor | | vadash | Auto-vendors junk items | |
| MercScanner | exApiTools/MercScanner | | | Scans/evaluates mercenary inventories | |
| MarketWizard | exApiTools/MarketWizard | | | Currency-exchange / market helper | |
| WhereMyFavsAt | deMathias/WhereMyFavsAt | | | Favourite items for Currency Exchange | |
| WhereAreMyDeployedObjects | DetectiveSquirrel/WhereAreMyDeployedObjects | archived | | Tracks placed/deployed objects | |

## Automation (pickit / flask / movement)

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| PickItV2 | exApiTools/PickItV2 | | exApiTools | Configurable item pickup | |
| PickItV2 | DetectiveSquirrel/PickItV2 | fork | exApiTools | Port of PickItV2 | |
| TC_Pickit | exApiTools/TC_Pickit | fork (TehCheat) | | Older TehCheat pickit | |
| BasicFlaskRoutine | exApiTools/BasicFlaskRoutine | fork | | Auto-flask on HP/ES/mana thresholds | |
| BuffUtil | exApiTools/BuffUtil | fork | | Auto-casts/maintains buffs & auras | |
| ReAgent | exApiTools/ReAgent | | exApiTools | Universal condition→action automation | ✦ |
| AutoOpen | exApiTools/AutoOpen | | | Auto-opens doors/containers | |
| AutoQuit | exApiTools/AutoQuit | | | Logs out on low HP / danger | |
| MineDetonator | exApiTools/MineDetonator | | | Auto-detonates mines | |
| AutoSextant | exApiTools/AutoSextant | | | Automates sextant rolling | |
| WheresMyCraftAt | DetectiveSquirrel/WheresMyCraftAt | | DetectiveSquirrel | Crafting automation (Craft-of-Exile style, IFL) | |
| WheresMyCraftAt | exApiTools/WheresMyCraftAt | | DetectiveSquirrel | Org mirror/port | |
| InputHumanizer | exApiTools/InputHumanizer | fork (sychotixdev) | | Humanized input timing service for automation | ✦ |
| Wasdeg | exApiTools/Wasdeg | | | WASD-style movement helper | |
| AutoMyAim | DetectiveSquirrel/AutoMyAim | archived | | Aim/cursor assistance | |

## Pricing / trade / economy

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| NinjaPricer (Get-Chaos-Value) | instantsc/Get-Chaos-Value | | instantsc | Auto-prices uniques/currency/etc. | |
| Get-Chaos-Value | DetectiveSquirrel/Get-Chaos-Value | fork (exApiTools) | instantsc | Upstream pricing | |
| Get-Chaos-Value | exApiTools/Get-Chaos-Value | | instantsc | Org pricing fork | |
| Get-Chaos-Value | TheOptimisticFactory/Get-Chaos-Value | fork | instantsc | Alt pricing fork | |
| tft-data-prices | exApiTools/tft-data-prices | | | TFT bulk-price data feed (data repo) | |
| MapsExchange | exApiTools/MapsExchange | | | Map-trading / exchange helper | |
| GemGuide | exApiTools/GemGuide | | | Gem leveling/quality pricing guidance | |
| TraderForPoe | DetectiveSquirrel/TraderForPoe | fork | | Standalone trade whisper helper | |
| KalandraOptimizer | exApiTools/KalandraOptimizer | archived | | Kalandra mirror-tier optimizer | |

## Combat / DPS / character info

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| DPSMeter | exApiTools/DPSMeter | | | Live DPS dealt/taken meter | |
| SkillDPS | exApiTools/SkillDPS | | | Computes skill DPS from data | |
| Skill-DPS | DetectiveSquirrel/Skill-DPS | archived | | Shows DPS from tooltip | |
| SkillGems | DetectiveSquirrel/SkillGems | archived | DetectiveSquirrel | Side panel for fast leveling gems | |
| GuardianStats | DetectiveSquirrel/Guardians-R-Us | archived | DetectiveSquirrel | Guardian (minion) stat readout | |
| Character Data | DetectiveSquirrel/Character-Data | archived | DetectiveSquirrel | Per-instance progress + resists/stats | |
| CharacterInformation | DetectiveSquirrel/CharacterInformation | archived | | Character info modules | |
| HowsMyPing | DetectiveSquirrel/HowsMyPing | archived | | Latency readout | |
| GameStatExporter | exApiTools/GameStatExporter | fork (TehCheat) | | Exports GameStats for other tools | ✦ |
| PobCheck | exApiTools/PobCheck | | | Path-of-Building import/check (Lua) | |
| SentinelHelper | exApiTools/SentinelHelper | archived | | Sentinel league helper | |
| RecipeTracker | exApiTools/RecipeTracker | archived | | Tracks vendor recipe progress | |
| KillCounter | exApiTools/KillCounter | | | Counts kills / kills-per-hour | |

## League mechanics

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| AltarHelper | bruno105/AltarHelper | | bruno105 | Eldritch altar choice highlighting | |
| AltarHelper | exApiTools/AltarHelper | fork (bruno105) | bruno105 | Org port | |
| BlightHelper | bruno105/BlightHelper | | bruno105 | Valuable anointment highlighting | |
| Blight Paths | DetectiveSquirrel/Blight | | DetectiveSquirrel | Blight lane + tower-radius awareness | |
| Abyss Paths | DetectiveSquirrel/Abyss | | DetectiveSquirrel | Abyss crack path awareness | |
| BetterSanctum | exApiTools/BetterSanctum | archived | ChandlerFerry | Sanctum room/affliction helper | |
| BetterSanctum | ChandlerFerry/PathfindSanctum | | ChandlerFerry | Endorsed Sanctum pathing fork | |
| VillageHelper | exApiTools/VillageHelper | | instantsc | Settlers/Kingsmarch (3.25) management | |
| VillageHelper | instantsc/VillageHelper | | instantsc | Upstream (endorsed source) | |
| UltimatumCheck | exApiTools/UltimatumCheck | | | Ultimatum reward/round helper | |
| AncestorQol | exApiTools/AncestorQol | | | Tujen/Ancestor QoL | |
| AdvancedUberLabLayout | exApiTools/AdvancedUberLabLayout | fork | | Uber lab layout solver | |
| WhatAreYouDoing | DetectiveSquirrel/WhatAreYouDoing | archived | DetectiveSquirrel | Labyrinth trap movement display | |
| EssenceCorruptionHelper | deMathias/EssenceCorruptionHelper | | deMathias | Tags essences matching criteria | |
| BroodyHen | IlliumIv/BroodyHen | | IlliumIv | Frame above non-incubated items | |
| Beasts | bruno105/Beasts | | bruno105 | Tracks/prices/auto-processes beasts | |
| Where The Circles At | DetectiveSquirrel/WhereTheCirclesAt | archived | DetectiveSquirrel | Ritual/league distance circles | |

## Pathfinding / movement

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| Radar | exApiTools/Radar | | instantsc | Terrain grid + target pathing (canonical pathfinder) | ✦ |
| DelveWalls | exApiTools/DelveWalls | | | Highlights Delve walls to mine | |
| LevelingHelper | TehCheat/LevelingHelper | | TehCheat | Suggests pacing while leveling | |
| BetterLeveling | DetectiveSquirrel/BetterLeveling | | | Leveling guidance | |
| WhereAreYouGoing | DetectiveSquirrel/ExileAPI-WhereAreYouGoing | archived | DetectiveSquirrel | Monster pathing prediction | |

## Dev tooling & libraries

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| ExileApi-Compiled | exApiTools/ExileApi-Compiled | | | The compiled ExileCore host distribution | ✦ |
| DevTree | exApiTools/DevTree | | exApiTools | Browse the live memory object tree | ✦ |
| TC_DevTree | exApiTools/TC_DevTree | fork (TehCheat) | | Older DevTree variant | |
| PluginTemplate | exApiTools/PluginTemplate | | | Starter skeleton for a new plugin | ✦ |
| PluginUpdater | exApiTools/PluginUpdater | fork (exCore2) | | In-app plugin install/update from GitHub | ✦ |
| exApiWiki | exApiTools/exApiWiki | | | Org wiki / how-to docs (data) | ✦ |
| IconsBuilder | exApiTools/IconsBuilder | fork (TehCheat), archived | | Shared icon construction library | ✦ |
| MinimapIcons | exApiTools/MinimapIcons | | | Minimap/large-map icon rendering helper | ✦ |
| InputHumanizer | exApiTools/InputHumanizer | fork (sychotixdev) | | Humanized input service (shared) | ✦ |
| ItemFilterLibrary | DetectiveSquirrel/ItemFilterLibrary *(not a standalone repo at survey time)* | | | Item-filter query lib used by every `*WithLinq` plugin | ✦ |
| ItemFilterLibInspector | DetectiveSquirrel/ItemFilterLibInspector | archived | DetectiveSquirrel | Visual inspector for IFL objects | ✦ |
| PassiveSkillTreePlanter | exApiTools/PassiveSkillTreePlanter | fork | | Tree-import overlay (uses tree routine) | ✦ |
| TC_PreloadAlert | exApiTools/TC_PreloadAlert | fork (TehCheat) | | Preload-based area/boss alerting | |
| Preloads Revised | DetectiveSquirrel/PreloadsRevised-poe1 | archived | | Configurable preload display | |
| GameStatExporter | exApiTools/GameStatExporter | fork (TehCheat) | | Exposes GameStats to other tooling | ✦ |
| ProjectileTracker | exApiTools/ProjectileTracker | | | Reference for reading projectile entities | |

> **Note on `BaseTreeRoutine`:** the passive-tree/automation "tree routine" base class
> (`BaseTreeRoutine`, used by `PassiveSkillTreePlanter` and the older flask/aura bots) is a
> shared building block originating from TehCheat-era plugins; it is not a separate repo in
> the exApiTools/DetectiveSquirrel inventories surveyed but is vendored inside the plugins
> that use it. Flagged ✦ as a technique worth documenting.

## Misc / QoL

| Plugin | Repo (author/name) | Fork?/Archived? | Endorsed | Purpose | Reusable |
| --- | --- | --- | --- | --- | --- |
| copilot | exApiTools/copilot | | | General play-assist HUD | |
| OverlayURLs | DetectiveSquirrel/OverlayURLs | | | Renders web URLs/overlays | |
| Cooking-Timer | DetectiveSquirrel/Cooking-Timer | | | Non-PoE utility timer | |
| FindingDory | DetectiveSquirrel/FindingDory | archived | | Object/entity finder | |
| FiveGuysFiveWays | DetectiveSquirrel/FiveGuysFiveWays | | | Misc/experimental | |
| TraceMyRay | DetectiveSquirrel/TraceMyRay | archived | | Ray/line tracing experiment | |

## Shared libraries / building blocks

These are the cross-plugin pieces most worth lifting into our docs. Each maps to a planned
cookbook recipe (produced by sibling workers; linked here by planned path).

| Building block | Where it lives | What it gives you | Cookbook recipe |
| --- | --- | --- | --- |
| **IconsBuilder** + **MinimapIcons** | exApiTools/IconsBuilder (fork of TehCheat, archived); exApiTools/MinimapIcons | Constructs and renders entity icons on the minimap and large map; the standard way HUD plugins draw map markers | [cookbook/icons-and-minimap.md](cookbook/icons-and-minimap.md) |
| **InputHumanizer** | exApiTools/InputHumanizer (fork of sychotixdev) | A shared input service that adds human-like delays/jitter to clicks and key presses; consumed by pickit/stash/vendor automation via `PluginBridge` | [cookbook/input-and-automation.md](cookbook/input-automation.md) |
| **ItemFilterLibrary** | bundled in DetectiveSquirrel's `*WithLinq` plugins (no standalone repo confirmed at survey time) | A LINQ/expression query layer over item data; the basis of GroundItems/NPCInv/InvWithLinq and WheresMyCraftAt rule syntax | [cookbook/item-filter-library.md](cookbook/item-filtering.md) |
| **BaseTreeRoutine** | vendored inside PassiveSkillTreePlanter and legacy flask/aura bots | A reusable tick-driven state/"routine" base for input automation plugins | [cookbook/input-and-automation.md](cookbook/input-automation.md) |
| **GameStatExporter** | exApiTools/GameStatExporter (fork of TehCheat) | Surfaces the player's `GameStat` values to other plugins/tools | [cookbook/reading-game-stats.md](cookbook/dev-tooling.md) |
| **PluginUpdater** | exApiTools/PluginUpdater (fork of exCore2) | Install/update plugins from GitHub URLs in-app; the distribution mechanism | [cookbook/distributing-plugins.md](cookbook/dev-tooling.md) |
| **PluginTemplate** | exApiTools/PluginTemplate | Minimal `BaseSettingsPlugin<TSettings>` skeleton to copy when starting | [cookbook/new-plugin-skeleton.md](cookbook/from-exapi-wiki.md) |
| **exApiWiki** | exApiTools/exApiWiki | The community how-to wiki; cross-reference for behaviors not in source | [cookbook/new-plugin-skeleton.md](cookbook/from-exapi-wiki.md) |
| **Radar (pathfinding)** | exApiTools/Radar (upstream instantsc/Radar) | Terrain-grid pathfinding + `Camera.WorldToScreen` line drawing — the canonical map/pathing example | [cookbook/pathfinding-and-terrain.md](cookbook/pathfinding-movement.md) |
| **ReAgent** | exApiTools/ReAgent | A general condition→action rules engine; reference for entity/buff/input glue | [cookbook/input-and-automation.md](cookbook/input-automation.md) |
| **DevTree** | exApiTools/DevTree | Live memory-tree browser — the tool you use to discover offsets/components while developing | [cookbook/debugging-with-devtree.md](cookbook/dev-tooling.md) |

## Source

This catalog was assembled from three lists, cross-checked where possible:

1. **PluginBrowser endorsed list** — parsed directly from
   [instantsc/PluginBrowserData `source.json`](https://raw.githubusercontent.com/instantsc/PluginBrowserData/master/source.json)
   (38 endorsed plugins, each with `OriginalAuthor`, `EndorsedAuthor`, repository list and
   description). This is the authoritative source for the "Endorsed" column.
2. **exApiTools org inventory** — the 66 known repo names (ReAgent, ExileApi-Compiled,
   Get-Chaos-Value, ItemFilter, MinimapIcons, WheresMyCraftAt, Radar, GemGuide, AltarHelper,
   Stashie, TC_Pickit, DevTree, PassiveSkillTreePlanter, XpBar, SkillDPS, TC_PreloadAlert,
   TC_MiscInformation, HeistIcons, FullRareSetManager, EliteBar, DPSMeter, BuffUtil,
   BasicFlaskRoutine, AutoQuit, AdvancedUberLabLayout, PluginUpdater, HighlightedItems,
   HealthBars, AutoOpen, AdvancedTooltip, StashieV2, MapModHighlight, PickItV2, HarvestForge,
   MercScanner, Wasdeg, NPCInvWithLinq, IconsBuilder, DelveWalls, BetterSanctum,
   HarvestPicker, VillageHelper, MarketWizard, ExpeditionIcons, ItemFilterExamples, exApiWiki,
   Ground-Items-With-Linq, tft-data-prices, copilot, UltimatumCheck, WhereTheWispsAt,
   InputHumanizer, PluginTemplate, MineDetonator, MapsExchange, KillCounter, PobCheck,
   AutoSextant, ProjectileTracker, AncestorQol, TC_DevTree, ProximityAlert, KalandraOptimizer,
   GameStatExporter, SentinelHelper, RecipeTracker). The fork/archived/description metadata
   for a confirmed subset (ReAgent, ItemFilter, Radar, GemGuide, MapModHighlight, PickItV2,
   HarvestForge, MercScanner, Wasdeg, BetterSanctum, HarvestPicker, VillageHelper,
   MarketWizard, ItemFilterExamples, exApiWiki, UltimatumCheck, PluginTemplate, PobCheck,
   ProjectileTracker, AncestorQol, KalandraOptimizer, SentinelHelper, RecipeTracker) came from
   the authenticated GitHub repo search; individual repo pages (IconsBuilder, InputHumanizer,
   GameStatExporter, PluginUpdater, Stashie, AltarHelper, NPCInvWithLinq, Ground-Items-With-Linq,
   ProximityAlert) were checked to confirm their fork-origin and library role. `exApiTools/SkillGems`
   and `instantsc/HarvestPicker` were checked and 404 at survey time, so no such rows are listed
   (SkillGems is endorsed under DetectiveSquirrel; HarvestPicker is endorsed under exApiTools).
3. **DetectiveSquirrel profile** — enumerated from his public repository pages
   ([github.com/DetectiveSquirrel?tab=repositories](https://github.com/DetectiveSquirrel?tab=repositories),
   pages 1–3). Most of his PoE1 plugins are archived and have `-PoE2` counterparts; the
   PoE1 plugins are the ones cataloged here. The standalone `ItemFilterLibrary` repo could
   not be resolved under his account at survey time (it 404s); it is bundled inside the
   `*WithLinq` plugins and is included as a building block on that basis.

Plugins or repos that could not be confirmed are tagged *(unconfirmed)* inline. The
unauthenticated GitHub JSON API was rate-limited during this survey, and anonymous
`git clone` was unavailable in the build environment, so confirmation relied on the
authenticated repo search, individual repo pages, and the raw `source.json` feed above.
