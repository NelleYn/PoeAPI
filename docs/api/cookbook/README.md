# ExileApi plugin cookbook — reusable techniques

Practical, copy-adaptable recipes for the tasks plugin authors actually write again and
again: drawing icons, filtering items, automating input, walking the stash, pathing, pricing
and so on. The recipes are distilled from the most-used community plugins — **exApiTools**
(`PickItV2`, `ReAgent`, `DevTree`, `FullRareSetManager`, `HarvestPicker`, `WhereTheWispsAt`),
**DetectiveSquirrel** (`Stashie`, `WhatAreYouDoing`, `WhereAreYouGoing`, the `*WithLinq`
family, `WheresMyCraftAt`, the various pathing plugins) and **instantsc**'s `PluginBrowser`
plus the plugins it ships (`Radar`, `ExpeditionIcons`, `NinjaPricer`, `VillageHelper`).

Each recipe takes a real, proven pattern from those plugins and rewrites it against **this
fork's** API as it actually exists in `Core/` — not against the upstream ExileApi‑Compiled
distribution most of those plugins were written for. Where a technique depends on a symbol
this fork does not expose (a missing component member, a league subsystem, an upstream helper
type), the recipe flags it and points at
[compatibility with ExileApi‑Compiled](../compatibility-exileapi-compiled.md) for the gap and
the workaround. Treat the code as a starting point you adapt, not a drop-in.

If you are new to the API, read [your first plugin](../README.md#your-first-plugin) first,
then come back here for task-shaped recipes.

## Recipes

| Recipe | What it covers |
| --- | --- |
| [icons-and-minimap.md](icons-and-minimap.md) | Drawing entity icons on the large map and minimap; atlas textures, `WorldToScreen`, the `IconsBuilder`/`MinimapIcons` pattern. |
| [item-filtering.md](item-filtering.md) | Querying ground/inventory/stash items with LINQ-style filters; the `ItemFilterLibrary` `*WithLinq` approach and a fork-native fallback. |
| [input-automation.md](input-automation.md) | Sending mouse/keyboard input safely: clicks, key holds, cursor moves, humanized delays, the `InputHumanizer` pattern. |
| [inventory-stash.md](inventory-stash.md) | Reading and acting on the inventory and stash: tab iteration, item rects, moving items (`Stashie`/`FullRareSetManager` techniques). |
| [automation-routines.md](automation-routines.md) | Structuring multi-step automation as state machines / behaviour trees off the render thread; `Job`, coroutines, the `BaseTreeRoutine` pattern. |
| [pathfinding-movement.md](pathfinding-movement.md) | Terrain data, line-of-sight and walking a path; `Radar`-style pathfinding and movement input. |
| [pricing-trade.md](pricing-trade.md) | Valuing items from poe.ninja-style data and static files; the `NinjaPricer` pattern and trade helpers. |
| [combat-info-overlays.md](combat-info-overlays.md) | Surfacing combat data: monster HP/rarity, dangerous mods, buffs and proximity warnings. |
| [league-mechanics.md](league-mechanics.md) | Working with league content (Expedition, Blight, Ritual, Sanctum, …) and what is missing on this fork. |
| [awareness-overlays.md](awareness-overlays.md) | "Where is X" situational-awareness overlays: monster paths, ground effects, lab traps, off-screen markers. |
| [dev-tooling.md](dev-tooling.md) | Inspecting the live memory tree while developing; the `DevTree` pattern and debug logging. |
| [from-exapi-wiki.md](from-exapi-wiki.md) | Techniques lifted from the ExileApi wiki, mapped onto this fork's symbols. |

Sibling recipe pages may land after this index — links point at planned filenames.

## Reusable building blocks / libraries

Several recipes lean on small reusable components that the community ships as drop-in helper
classes or standalone libraries. Candidate C# ports of these (adapted to this fork) are being
collected under the top-level [`../../../proposals/`](../../../proposals/) folder; the recipe
that explains each pattern is listed alongside.

| Building block | What it is | Covered by |
| --- | --- | --- |
| `IconsBuilder` | Reusable helper that turns entities into draw-once map/minimap icons with priority + atlas lookup. | [icons-and-minimap.md](icons-and-minimap.md), `proposals/` port |
| `InputHumanizer` | Adds randomized, human-like delays/jitter to input so automation is less robotic. | [input-automation.md](input-automation.md), `proposals/` port |
| `ItemFilterLibrary` | LINQ-queryable item model + filter parser used by every `*WithLinq` plugin. | [item-filtering.md](item-filtering.md), `proposals/` port |
| `BaseTreeRoutine` | Behaviour-tree base class for composing automation steps/conditions. | [automation-routines.md](automation-routines.md), `proposals/` port |
| `MinimapIcons` | Minimap-specific icon placement/scaling layer that pairs with `IconsBuilder`. | [icons-and-minimap.md](icons-and-minimap.md) |
| `GameStatExporter` | Reads `GameStat`/`Stats` values and exposes them (debug overlay / `PluginBridge`). | [dev-tooling.md](dev-tooling.md), [combat-info-overlays.md](combat-info-overlays.md) |
| `PluginTemplate` | Skeleton plugin project (settings + lifecycle) you fork to start a new plugin. | [../README.md#your-first-plugin](../README.md#your-first-plugin), `proposals/` port |
| `PluginUpdater` | Self-update / version-check helper bundled by some plugins and `PluginBrowser`. | [dev-tooling.md](dev-tooling.md) |

## See also

- [API reference index](../README.md)
- [plugin catalog](../plugin-catalog.md)
- [compatibility with ExileApi‑Compiled](../compatibility-exileapi-compiled.md)

## Source

This cookbook surveys the techniques used across the most-installed community plugins, as
catalogued in [../plugin-catalog.md](../plugin-catalog.md) and
[../plugin-usage-index.md](../plugin-usage-index.md):

- **exApiTools** — `PickItV2`, `ReAgent`, `DevTree`, `FullRareSetManager`, `HarvestPicker`,
  `WhereTheWispsAt`.
- **DetectiveSquirrel** — `Stashie`, `WhatAreYouDoing`, `WhereAreYouGoing`, `WheresMyCraftAt`,
  the `NPCInvWithLinq` / `Ground-Items-With-Linq` family, `AreaStatVisual`, the Blight / Abyss
  / Ritual pathing plugins.
- **instantsc** — `PluginBrowser` and the plugins it distributes (`Radar`, `ExpeditionIcons`,
  `Get-Chaos-Value`/`NinjaPricer`, `VillageHelper`).
- Reusable libraries: `ItemFilterLibrary`, `IconsBuilder`/`MinimapIcons`, `InputHumanizer`,
  `BaseTreeRoutine`, `GameStatExporter`, `PluginTemplate`, `PluginUpdater`.

Every recipe rewrites these patterns against this fork's `Core/` API and flags upstream-only
gaps via [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).
