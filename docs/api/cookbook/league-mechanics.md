# Recipe: league-mechanic helpers

How "league helper" plugins find the league's UI window or special entities, read the
choices/state on offer, score them, and highlight the best — distilled from a dozen
real plugins (AltarHelper, UltimatumCheck, SentinelHelper, AncestorQol, WhereTheWispsAt,
Beasts, MapModHighlight, VillageHelper, DelveWalls, HarvestForge, AdvancedUberLabLayout).

[API reference index](../README.md) · [cookbook index](README.md)

> Many league windows (Ultimatum, Sentinel, Ancestor, Village, Harvest crafting, Altar
> choice entities, Bestiary panel) are **upstream-only**: they exist in the larger
> ExileApi-Compiled distribution but not in this fork's `Core/`. Where a recipe step uses
> one, it is flagged and cross-linked to
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) and
> [exilecore-dll-reference.md](../exilecore-dll-reference.md). The *shapes* of the recipes
> still apply — only the concrete window/entity type differs. Everything not flagged is
> verified against this fork's `Core/`.

---

## The shape of a league helper

Almost every one of these plugins follows the same four steps:

1. **Find the league surface** — either a named window under
   [`GameController.IngameState.IngameUi`](../ingame-state.md), or the league's special
   entities found by metadata/path in [`EntityListWrapper`](../entities.md).
2. **Gate on state** — only act while the encounter is *live* and unselected (a
   state-machine flag, a `Chest.IsOpened`, a `Monolith.IsOpened`, panel `IsVisible`).
3. **Read the choices and score them** — enumerate the option elements / option entities,
   pull their id or mod text, and rank each against the user's config.
4. **Highlight the winner** — outline the best option with
   [`Graphics.DrawFrame`](../graphics.md) over its `GetClientRect`, or draw a label/box in
   the world or on the large map.

The two entry points (window vs. entities) are covered below, then scoring, then drawing.

---

## Step 1a: find a league window via `IngameUi`

The simplest helpers read a named panel straight off `IngameUi` and bail unless it is
visible. The fork exposes the windows it knows about as typed properties on
[`IngameUIElements`](../ui-elements.md); for anything else, walk by child indices with
`GetChildFromIndices`.

```csharp
public override void Render()
{
    var ui = GameController.IngameState.IngameUi;

    // Always gate on IsVisible: an off-screen panel still has a (stale) Address.
    if (ui.IncursionWindow is { IsVisible: true } window)
    {
        // Reward text + accept button reached by child indices (see ui-elements.md).
        var reward = window.Reward1;
        var accept = window.AcceptElement;        // GetChildFromIndices(3, 13, 2)
        // ... score the reward, outline `accept`, etc.
    }
}
```

`IncursionWindow` is one of the few league windows this fork ships (see
[ui-elements.md](../ui-elements.md) for the full list of named roots, and `IsVisible`
vs. `IsVisibleLocal`).

> **Upstream-only windows.** UltimatumCheck reads `ui.UltimatumPanel.ChoicesPanel`,
> SentinelHelper reads `ui.GameUI.SentinelPanel.RedSentinelSubPanel`, AncestorQol reads
> `ui.AncestorFightSelectionWindow` / `ui.AncestorMainShopWindow`, VillageHelper reads
> `ui.VillageScreen` / `ui.VillageShipmentScreen`, HarvestForge reads
> `ui.HorticraftingStationWindow`. **None of these `IngameUi` properties exist in this
> fork** — see the "Elements & InventoryElements (114 absent)" and "MemoryObjects (61
> absent)" sections of [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).
> The traversal idiom (read panel → check `IsVisible` → enumerate option children) is the
> reusable part; the property names are not portable.

### Occlusion guard

When a helper draws in the *world* (not on a panel), it first checks whether a big UI
panel is covering the screen. Upstream plugins use `IngameUi.FullscreenPanels` /
`IngameUi.LargePanels` (both **absent in this fork**). In this fork, gate on the panels
that do exist:

```csharp
var ui = GameController.IngameState.IngameUi;
bool sidePanelOpen = ui.OpenLeftPanel.IsVisible || ui.OpenRightPanel.IsVisible;
bool blockingPanel = ui.StashElement.IsVisible || ui.InventoryPanel.IsVisible;
if (sidePanelOpen || blockingPanel)
    return;   // don't draw world overlays underneath an open panel
```

This is exactly DelveWalls' guard (`OpenLeftPanel`/`OpenRightPanel`/`StashElement`/
`InventoryPanel`, all verified in [ui-elements.md](../ui-elements.md)).

---

## Step 1b: find league entities by metadata

The other entry point is the entity list. League objects (wisps, altars, wells, walls,
monoliths) carry a stable `Entity.Metadata` / `Entity.Path`, so helpers bucket them in
`EntityAdded` and forget them in `EntityRemoved`. This is the most fork-portable pattern of
all — it uses only [entity](../entities.md) members that exist here.

```csharp
public override void EntityAdded(Entity entity)
{
    switch (entity.Metadata)
    {
        case "Metadata/MiscellaneousObjects/Azmeri/AzmeriLightBomb":
            _lightBombs.Add(entity);
            break;
        case not null when entity.Metadata.Contains("Azmeri/SacrificeAltarObjects"):
            _altars.Add(entity);
            break;
        case not null when entity.Metadata.StartsWith("Metadata/Chests/LeagueAzmeri/"):
            _chests.Add(entity);
            break;
    }
}

public override void EntityRemoved(Entity entity)
{
    _lightBombs.Remove(entity);
    _altars.Remove(entity);
    _chests.Remove(entity);
}

public override void AreaChange(AreaInstance area)
{
    _lightBombs.Clear(); _altars.Clear(); _chests.Clear();   // reset per area
}
```

Adapted from WhereTheWispsAt. A variant scans `GameController.Entities` (or
`ValidEntitiesByType[EntityType.IngameIcon]`) each frame instead of caching — DelveWalls
does `foreach (var e in GameController.Entities) if (e.Path.Contains("DelveWall")) ...`.
For the cheaper, pre-filtered lists (`OnlyValidEntities`, `ValidEntitiesByType`), see
[entities.md](../entities.md).

---

## Step 2: gate on encounter state

You only want to highlight *live, undecided* choices. Helpers test a per-object state
before scoring:

```csharp
// Already-cleared league objects: skip them.
var monolith = entity.GetComponent<Monolith>();
if (monolith?.IsOpened == true) return;        // OpenStage == 4, object will vanish

if (entity.GetComponent<Chest>()?.IsOpened != false) return;   // looted chest
```

`Monolith.IsOpened` and `Chest.IsOpened` are both in this fork — see
[components-world.md](../components-world.md).

> **Upstream-only: `StateMachine.States`.** AltarHelper, UltimatumCheck and WhereTheWispsAt
> gate on a named state-machine entry, e.g.
> `entity.TryGetComponent<StateMachine>(out var sm) && sm.States.Any(s => s.Name == "activated" && s.Value == 1)`.
> This fork **does** have a `StateMachine` component, but it only exposes `CanBeTarget` /
> `InTarget` — there is **no `States` list** (`Core/PoEMemory/Components/StateMachine.cs`).
> Port these gates to whatever in-fork signal exists (`Chest.IsOpened`, `Monolith.IsOpened`,
> `Shrine.IsAvailable`, the object disappearing from the entity list, or
> `ClientAnimationController.AnimKey`). Also note `Entity.TryGetComponent<T>(out T)` is
> upstream-only here — use `var c = e.GetComponent<T>(); if (c != null)` instead (see
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) §Entity).

For panel helpers the gate is simply `panel is { IsVisible: true }`, plus (for choose-one
windows) skipping the already-selected option — UltimatumCheck thickens the frame on the
`SelectedChoice` index rather than skipping it.

---

## Step 3: read choices and score them

Two flavours of "choice": **option elements inside a window**, and **mod text on an
entity/item**.

### Option elements inside a window

Enumerate the window's option children, read an id or label off each, and look the rank up
in the user's config. Pattern adapted from UltimatumCheck (window type upstream-only):

```csharp
// `panel` is the (upstream-only) UltimatumChoicePanel; ChoiceElements / Modifiers are its
// option elements + parsed modifiers. The reusable idea: zip option-elements with their
// data, map each to a rank, pick a colour.
foreach (var (element, modifier) in panel.ChoiceElements.Zip(panel.Modifiers))
{
    int tier = Settings.ModRanking.GetModifierTier(modifier.Id);   // user config
    Color color = tier switch
    {
        1 => Settings.Rank1Color,
        2 => Settings.Rank2Color,
        3 => Settings.Rank3Color,
        _ => Color.Gray,
    };
    Graphics.DrawFrame(element.GetClientRectCache, color, Settings.FrameThickness);
}
```

When the option elements are plain `Element`s (no typed wrapper), read the label by child
index instead — AncestorQol does `option.GetChildFromIndices(1, 0, 0)?.Text` to get a tribe
name, then `Settings.GetTribeRewardTier(tribeName)`. `Element.Text`, `GetChildFromIndices`,
`this[int]` and `GetClientRectCache` are all in this fork (see
[ui-elements.md](../ui-elements.md)).

> **Upstream-only text helpers.** AncestorQol/MapModHighlight also use `Element.TextNoTags`
> and `Element.GetText(maxLength)` to strip the game's `<tag>{...}` markup. Neither exists on
> this fork's `Element` (only `Text`). Strip tags yourself with a regex, e.g.
> `Regex.Replace(text, "<[^>]*>{(?<v>[^}]*)}", "${v}")`, or read `Text` and accept the markup.

### Scoring across both options (choose-one)

For a two-option altar you don't just rank each side in isolation — you compare them so only
the *better* side is flagged "pick". AltarHelper aggregates each side's mods into a
`(bisRank, brick, pickRank, nuisanceRank)` tuple, then marks an option `IsPick` only when
its rank beats the other side:

```csharp
// pseudo-distillation of AltarHelper's cross-side comparison
bool topIsPick    = top.PickRank    != null && (bottom.PickRank    == null || bottom.PickRank    >= top.PickRank);
bool bottomIsPick = bottom.PickRank != null && (top.PickRank       == null || top.PickRank       >= bottom.PickRank);
```

The general recipe: compute a comparable score per option, then a "best" flag = "this
option's score is defined AND no other option scores strictly better".

### Mod text on a hovered item

MapModHighlight scores the tooltip of the hovered item. The hover element itself —
`GameController.IngameState.UIHover` — *is* in this fork (see
[ingame-state.md](../ingame-state.md); note upstream's extra `UIHoverElement` alias is
absent). Cast it to `HoverItemIcon`, walk to the mods, and match each line:

```csharp
var hover = GameController.IngameState.UIHover;
if (hover is not { Address: not 0, IsValid: true, IsVisible: true }) return;

var frame = hover.AsObject<HoverItemIcon>().ItemFrame;     // AsObject<T> verified in fork
if (frame is not { IsVisible: true }) return;
// ... walk frame.Children / GetChildAtIndex to the mod element, split its Text into lines,
//     and DrawFrame the lines your filter matches.
```

`UIHover`, `AsObject<T>`, `HoverItemIcon`, `Children`/`GetChildAtIndex` and
`GetClientRectCache` are all in this fork ([ui-elements.md](../ui-elements.md)).

### Scoring monsters in range (predicate config)

SentinelHelper counts nearby monsters by [rarity](../enums.md) and fires when a
user-written predicate (e.g. `"Rare>2 || Total>20"`) is satisfied. The counting loop is
fully fork-compatible:

```csharp
foreach (var e in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
{
    if (!e.IsHostile || !e.IsAlive) continue;
    if (e.DistancePlayer > Settings.Range) continue;
    var omp = e.GetComponent<ObjectMagicProperties>();
    if (omp == null || omp.Rarity == MonsterRarity.Error) continue;
    switch (omp.Rarity) { /* White/Magic/Rare/Unique tallies */ }
}
```

`IsHostile`, `IsAlive`, `DistancePlayer`, `ObjectMagicProperties.Rarity` and `MonsterRarity`
are all in this fork (see [entities.md](../entities.md),
[components-combat.md](../components-combat.md), [enums.md](../enums.md)). Buff filtering via
`entity.Buffs` (a `List<Buff>` off the entity, *not* a `Buffs` component here) also works —
SentinelHelper skips already-tagged monsters with
`entity.Buffs.Any(b => b.Name.StartsWith("sentinel_tag_visual_"))`.

---

## Step 4: highlight the best choice

### Outline a UI option

The universal "this is the pick" cue is a coloured frame over the option's screen
rectangle. `GetClientRect()` / `GetClientRectCache` return overlay-space `RectangleF`s that
line up with [`Graphics`](../graphics.md) directly:

```csharp
Graphics.DrawFrame(optionElement.GetClientRectCache, pickColor, Settings.FrameThickness);
```

To show a *second* state on the same option (e.g. BiS **and** has a downside), draw a
nested inner frame — AltarHelper inflates the rect inward and draws again:

```csharp
var rect = optionElement.GetClientRectCache;
Graphics.DrawFrame(rect, bisColor, thickness);
rect.Inflate(-thickness, -thickness);
Graphics.DrawFrame(rect, brickColor, thickness);
```

### Label an option with a note / score

Draw a black box behind centered text next to the option (AncestorQol's tier-favour
summary, VillageHelper's wage badges). All fork-verified:

```csharp
var rect = optionElement.GetClientRectCache;
var text = $"T{tier}: {score}";
var size = Graphics.MeasureText(text);
var pos  = rect.TopRight.ToVector2Num();
Graphics.DrawBox(pos, pos + size, Color.Black);
Graphics.DrawText(text, pos, tierColor);
```

> **Upstream-only convenience:** `Graphics.DrawTextWithBackground(...)` (VillageHelper) is
> not in this fork — compose `DrawBox` + `DrawText` as above. Same for the left-panel cursor
> `GameController.LeftPanel.StartDrawPointNum` (use the fork's `LeftPanel.StartDrawPoint`,
> a SharpDX `Vector2`; `LeftPanel.WantUse(...)` is present).

### Mark an entity in the world or on the large map

For world objects, project the entity's position with the camera and draw a label/box.
WhereTheWispsAt draws on the large map; the fork-portable version uses
`Camera.WorldToScreen` + `DrawBox`/`DrawText`:

```csharp
foreach (var entity in _altars)
{
    var screen = GameController.IngameState.Camera.WorldToScreen(entity.Pos);  // SharpDX Vector2
    if (screen == Vector2.Zero) continue;                                      // off-screen sentinel
    var size = Graphics.MeasureText("Altar");
    Graphics.DrawBox(new RectangleF(screen.X - size.X/2 - 3, screen.Y - size.Y/2, size.X + 6, size.Y), Color.Black);
    Graphics.DrawText("Altar", new Vector2(screen.X, screen.Y), color, FontAlign.Center);
}
```

See [coordinates.md](../coordinates.md) for world→screen and [graphics.md](../graphics.md)
for the draw calls.

> **Upstream-only world helpers.** Beasts draws with `Graphics.DrawFilledCircleInWorld` /
> `DrawCircleInWorld` / `DrawBoundingBoxInWorld`, and maps grid→large-map with
> `IngameState.Data.GetGridMapScreenPosition` / `ToWorldWithTerrainHeight` /
> `RawTerrainHeightData`, casting `Map.LargeMap.AsObject<SubMap>()` for `MapCenter`/
> `MapScale`. **None of those exist in this fork** (`Graphics` has no `*InWorld` methods;
> `IngameData` has no terrain/grid-map helpers; there is no `SubMap` — `Map.LargeMap` is a
> plain `Element` with `LargeMapShiftX/Y` + `LargeMapZoom` floats). For an in-world circle,
> project the center with `Camera.WorldToScreen` and draw with ImGui's draw list or stick to
> screen-space `DrawFrame`/`DrawBox`. See
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) §Graphics and
> §UI Elements (`SubMap`).

### A self-contained, overlay-only helper

Not every league helper reads the game UI at all. AdvancedUberLabLayout downloads the day's
lab layout image, crops it, loads it with `Graphics.InitImage(path, false)` and draws it
with `Graphics.DrawImage` plus a reset-timer label — pure overlay, no memory reads beyond
the `AreaChange(AreaInstance area)` hook's `area.RealLevel` to auto-pick the lab tier.
DelveWalls similarly overlays a directional
arrow image (`Graphics.InitImage` + `DrawImage` with a UV sub-rect). Both use only
fork-verified `Graphics` image APIs (see [graphics.md](../graphics.md)).

---

## Reading special Files tables (mod/choice translation)

Some helpers don't read entity components for the choice text — they read the league's
static `.dat` table through [`GameController.Files`](../files-in-memory.md) and translate the
stat ids to human text. AltarHelper builds its mod dictionary from
`Files.AtlasPrimordialAltarChoices` and translates with
`Files.PrimordialAltarStatDescriptions.TranslateMod(...)`; VillageHelper reads
`Files.VillageJobTypes` / `Files.VillageShippingPorts` / `Files.VillageJobSkillLevels`.

> **Upstream-only tables.** This fork's `FilesContainer` does **not** include the altar,
> village, or the stat-description-translation tables (`AtlasPrimordial*`,
> `*StatDescriptions`, `Village*`) — see the "FilesInMemory — static data tables (90 absent)"
> and "Stat-description translation pipeline" sections of
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). The fork's
> available tables (`BaseItemTypes`, `Mods`, `Stats`, `WorldAreas`, `MonsterVarieties`,
> Bestiary, Betrayal, Metamorph, …) are documented in
> [files-in-memory.md](../files-in-memory.md); for the rest, read the mod text off the UI
> element instead, or treat the table as unavailable.

A fork-available cousin: Beasts resolves a captured-monster item's name via the
[`CapturedMonster` component] → `MonsterVariety.MonsterName`, and beast records via the
Bestiary tables — but note the `CapturedMonster` *component* and the Bestiary *UI panel*
(`ChallengesPanel.TabContainer.BestiaryTab.CapturedBeastsTab`) are upstream-only; the
Bestiary *Files tables* (`BestiaryCapturableMonsters`, etc.) **are** in this fork
([files-in-memory.md](../files-in-memory.md)).

---

## Caching: don't traverse the UI tree every frame

League helpers re-read the same panel/entity list many times per frame, so they wrap the
expensive read in a cache and refresh it on a timer. The fork ships the cache types these
plugins use (`FrameCache<T>`, `FramesCache<T>`, `TimeCache<T>`, all `CachedValue<T>` — see
[caching.md](../caching.md)):

```csharp
private CachedValue<List<LabelOnGround>> _labelCache;

public override bool Initialise()
{
    // refresh at most every Settings.CacheTime ms
    _labelCache = new TimeCache<List<LabelOnGround>>(GetItemsOnGround, Settings.CacheTime.Value);
    return true;
}
```

This is BlightHelper's structure (note its `ItemsOnGroundLabelsVisible` is upstream-only —
use `IngameUi.ItemsOnGroundLabels` and filter on `IsVisible` yourself, per
[ui-elements.md](../ui-elements.md)). Build heavy snapshots in `Tick()` and let `Render()`
read the snapshot, as Beasts does, to keep the draw thread cheap.

---

## Cross-references

- [ui-elements.md](../ui-elements.md) — `IngameUi` named roots, `Element` traversal,
  `GetClientRect`/`GetClientRectCache`, `IsVisible`.
- [files-in-memory.md](../files-in-memory.md) — `GameController.Files` tables available here.
- [graphics.md](../graphics.md) — `DrawFrame`/`DrawBox`/`DrawText`/`DrawImage`/`MeasureText`.
- [components-world.md](../components-world.md) — `Monolith`, `Chest`, `Shrine`,
  `StateMachine`, `MinimapIcon` and other league/world components.
- [entities.md](../entities.md) — entity lists, metadata/path, `GetComponent<T>`, distance.
- [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) /
  [exilecore-dll-reference.md](../exilecore-dll-reference.md) — the upstream-only windows,
  components, Files tables and Graphics helpers flagged throughout this recipe.

## Source repos

- [exApiTools/AltarHelper](https://github.com/exApiTools/AltarHelper) — Eldritch altar
  choice entities; cross-side mod scoring; nested-frame highlight; Files-table mod
  translation.
- [exApiTools/UltimatumCheck](https://github.com/exApiTools/UltimatumCheck) — Ultimatum
  panel / ground-label entry; zip option-elements with modifiers; tier-coloured frames.
- [exApiTools/SentinelHelper](https://github.com/exApiTools/SentinelHelper) — Sentinel
  sub-panels; nearby-monster scoring with a compiled predicate; input automation.
- [exApiTools/AncestorQol](https://github.com/exApiTools/AncestorQol) — Trial-of-the-Ancestors
  selection/shop windows; per-option child-index reads; tier labels + favour summaries.
- [exApiTools/WhereTheWispsAt](https://github.com/exApiTools/WhereTheWispsAt) — Azmeri
  entities bucketed in `EntityAdded`; state-gated removal; large-map drawing.
- [exApiTools/MapModHighlight](https://github.com/exApiTools/MapModHighlight) — `UIHover`
  tooltip → mod element walk; per-line filter highlight.
- [exApiTools/VillageHelper](https://github.com/exApiTools/VillageHelper) — Settlers village
  screens; worker scoring; resource/upgrade overlays via ImGui.
- [exApiTools/DelveWalls](https://github.com/exApiTools/DelveWalls) — Delve-wall entities by
  path; grid-delta direction; directional image overlay.
- [exApiTools/HarvestForge](https://github.com/exApiTools/HarvestForge) — Horticrafting
  window; item-filter match loop; craft-button automation.
- [exApiTools/AdvancedUberLabLayout](https://github.com/exApiTools/AdvancedUberLabLayout) —
  external lab-image download/crop; `InitImage`/`DrawImage` overlay + reset timer.
- [bruno105/BlightHelper](https://github.com/bruno105/BlightHelper) — anoint/ground-item
  mod filter; `WorldItem.ItemEntity` + `Mods.ItemMods`; `TimeCache` of ground labels.
- [bruno105/Beasts](https://github.com/bruno105/Beasts) — captured-beast panel + poe.ninja
  pricing; entity tracking; in-world + large-map labels; `Tick()` snapshot pattern.
