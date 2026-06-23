# UI elements & the UI tree

How plugins read the game's in-game UI: the `Element` base class, the named root
panels exposed by `IngameUIElements`, and the on-screen elements you can walk,
measure and draw over. See the [API reference index](README.md).

> Everything here is read-only. Elements are typed views over PoE process memory.
> Positions are computed each frame from raw offsets, so cache results yourself if you
> read them in tight loops.

## The `Element` base class

Namespace `ExileCore.PoEMemory`. `Element : RemoteMemoryObject`. Every UI node — panels,
buttons, labels, inventory cells — is an `Element` (or a subclass). The whole in-game UI
is a tree of these, rooted at `TheGame.IngameState.UIRoot` (also reachable via any
element's `Root`).

Source: `Core/PoEMemory/Element.cs`, backed by `GameOffsets/ElementOffsets.cs`.

### Identity & validity

| Member | Type | Meaning |
| --- | --- | --- |
| `Address` | `long` | Memory address this element is anchored at (`0` ⇒ not present). From `RemoteMemoryObject`. |
| `IsValid` | `bool` | `true` when the element's self-pointer matches its `Address` — a cheap "this is a real element" check. |
| `Root` | `Element` | The in-game UI root (`TheGame.IngameState.UIRoot`). |
| `Elem` | `ElementOffsets` | The raw, frame-cached offset struct. Rarely needed directly. |

### Children & tree traversal

| Member | Type | Meaning |
| --- | --- | --- |
| `Parent` | `Element` | Parent element, or `null` at the root. |
| `Children` | `IList<Element>` | Direct children (frame-cached; rebuilt only when the child list changes). |
| `ChildCount` | `long` | Number of direct children. |
| `this[int index]` | `Element` | Indexer; alias for `GetChildAtIndex`. |
| `GetChildAtIndex(int index)` | `Element` | Child at `index`, or `null` when out of range. |
| `GetChildFromIndices(params int[] indices)` | `Element` | Walks the tree following a chain of child indices; returns `null` (and logs) if any index is missing. |
| `GetChildrenAs<T>()` | `List<T>` | Direct children freshly materialized as `T` (not cached). |
| `ChildHash` | `long` | Hash of the child pointers; used internally to detect child-list changes. |

Walking the tree by **child indices** is the dominant pattern in plugins. The game's UI
layout is stable across a session, so once you find that, say, a button lives at
`panel → child 3 → child 13 → child 2`, you reach it with:

```csharp
// equivalent to root[3][13][2]
var button = panel.GetChildFromIndices(3, 13, 2);
```

`GetChildFromIndices` is the safe form: it null-checks and logs each hop instead of
throwing on a missing index. `IncursionWindow` in this repo uses exactly this idiom
(`GetChildFromIndices(3, 13, 2)` for its accept button).

### Visibility

| Member | Type | Meaning |
| --- | --- | --- |
| `IsVisibleLocal` | `bool` | This element's own visibility flag, ignoring ancestors. |
| `IsVisible` | `bool` | `true` only when this element **and its whole parent chain** are visible. Use this before drawing over an element. |
| `isHighlighted` | `bool` | Whether the element is currently highlighted by the game. |

### Position & size

| Member | Type | Meaning |
| --- | --- | --- |
| `Position` | `Vector2` | Position **relative to the parent**, in UI-local units. |
| `X` / `Y` | `float` | Components of `Position`. |
| `Width` / `Height` | `float` | Local size, in UI-local units. |
| `Scale` | `float` | Local scale factor. |
| `GetParentPos()` | `Vector2` | Accumulated, scaled position contributed by all ancestors. |

These are *local* values. To get the on-screen rectangle (accounting for the parent
chain, UI scale and camera size) use `GetClientRect`, below — that is almost always what
you want, not raw `X`/`Y`/`Width`/`Height`.

> `Vector2` here is `SharpDX.Vector2`. There is no combined `Size` property or
> `PositionNum` on `Element`; use `Width`/`Height` and `Position` (or the rectangle from
> `GetClientRect`).

### On-screen rectangle (the one you draw with)

| Member | Type | Meaning |
| --- | --- | --- |
| `GetClientRect()` | `RectangleF` | Computes the element's screen-space rectangle this instant; returns `RectangleF.Empty` when `Address == 0`. |
| `GetClientRectCache` | `RectangleF` | Same value, cached for ~200 ms via a `TimeCache`. Prefer this when reading many elements per frame. |

`RectangleF` is `SharpDX.RectangleF` (it has `.X/.Y/.Width/.Height`, plus `.Center`,
`.TopLeft`, `.TopRight`, `.Intersects(...)`, etc.). The rectangle is in **overlay screen
space**, so it lines up with the drawing helpers in [graphics.md](graphics.md). To turn a
rect into game-window coordinates, add the window position (see the example below).

### Text & tooltip

| Member | Type | Meaning |
| --- | --- | --- |
| `Text` | `string` (virtual) | The element's display text, with embedded icons collapsed to `{{icon}}`; `null` when empty. Several subclasses override this. |
| `Tooltip` | `Element` | The tooltip element attached to this element, or `null`. |

> The base `Element` exposes no text-colour or background-colour members. Colour-bearing
> members (e.g. `TextColour`, `HighlightBackgroundColor`) are not present in this source;
> read text with `Text` and supply your own colours when drawing.

---

## `IngameUIElements` — the named UI roots

Namespace `ExileCore.PoEMemory.MemoryObjects`. `IngameUIElements : Element`. This is the
in-game HUD container, reached as `GameController.Game.IngameState.IngameUi` (see
[ingame-state.md](ingame-state.md)). Instead of hand-walking child indices, it exposes the
important panels and overlays as named, typed properties.

Source: `Core/PoEMemory/MemoryObjects/IngameUIElements.cs`.

Verified members (selected — see source for the full list):

| Property | Type | What it is |
| --- | --- | --- |
| `GameUI` | `Element` | Root of the HUD layer. |
| `InventoryPanel` | `InventoryElement` | The player inventory panel — see [inventories.md](inventories.md). |
| `StashElement` | `StashElement` | The stash window and its tabs — see [inventories.md](inventories.md). |
| `OpenLeftPanel` | `Element` | Whatever panel is open on the left (vendor, etc.). |
| `OpenRightPanel` | `Element` | Whatever panel is open on the right (inventory side). |
| `TreePanel` | `Element` | The passive skill tree panel. |
| `AtlasPanel` | `Element` | The Atlas panel. |
| `WorldMap` | `WorldMapElement` | The world map (instance selection) UI. |
| `AreaInstanceUi` | `WorldMapElement` | The area/instance UI. |
| `Map` | `Map` | The map element — both the large map and the minimap (see `Map` below). |
| `ItemsOnGroundLabelElement` | `ItemsOnGroundLabelElement` | Container for ground-item labels. |
| `ItemsOnGroundLabels` | `IList<LabelOnGround>` | Shortcut to `ItemsOnGroundLabelElement.LabelsOnGround`. |
| `ItemOnGroundTooltip` | `ItemOnGroundTooltip` | Tooltip shown for a ground item. |
| `SkillBar` / `HiddenSkillBar` | `SkillBarElement` | The visible / hidden skill bars. |
| `ChatBox` | `PoeChatElement` | The chat panel. |
| `ChatMessages` | `IList<string>` | Text of each chat child. |
| `Cursor` | `Cursor` | The in-game cursor. |
| `SellWindow` / `PurchaseWindow` | `SellWindow` / `Element` | Trade windows. |
| `DelveWindow` | `SubterraneanChart` | The Delve (Subterranean Chart) window. |
| `IncursionWindow` | `IncursionWindow` | The Incursion window. |
| `BetrayalWindow` / `SyndicateTree` | `Element` | Betrayal / syndicate UI. |
| `SynthesisWindow`, `UnveilWindow`, `CraftBench`, `MetamorphWindow`, `MapStashTab`, `ZanaMissionChoice`, `QuestTracker`, `GemLvlUpPanel`, `InvitesPanel` | various | Other named windows/panels. |
| `GetUncompletedQuests`, `GetCompletedQuests`, `GetQuestStates` | quest collections | Quest tracking. |

There is no `Atlas` or `WorldMap`-named property other than the two above
(`AtlasPanel`, `WorldMap`/`AreaInstanceUi`); use those exact names.

---

## On-screen elements

Each is in `ExileCore.PoEMemory.Elements` (file under `Core/PoEMemory/Elements/`) and
subclasses `Element` unless noted.

### `EntityLabel`

`EntityLabel.cs`. A text label attached to an entity (floating names, chat lines). The
base `Element.Text` is actually implemented on top of this type's `Text2`. Links an
on-screen label to its [entity](entities.md).

| Member | Type | Meaning |
| --- | --- | --- |
| `Text` | `string` | The label string (read inline or via pointer depending on capacity). |
| `Text2` | `string` | Alternate native-string read at a fixed offset; backs `Element.Text`. |
| `Length` | `int` | Label length, clamped to `0` when out of range. |
| `Capacity` | `int` | Buffer capacity, clamped to `0` when out of range. |

### `LabelOnGround`

`LabelOnGround.cs`. **`RemoteMemoryObject`, not an `Element`** — it pairs a ground-item
entity with its on-screen label and exposes pick-up state.

| Member | Type | Meaning |
| --- | --- | --- |
| `ItemOnGround` | `Entity` | The ground-item [entity](entities.md), or `null`. |
| `Label` | `Element` | The label element for this item, or `null`. |
| `IsVisible` | `bool` | `Label?.IsVisible ?? false`. |
| `CanPickUp` | `bool` | Whether the item can be picked up right now. |
| `TimeLeft` | `TimeSpan` | Time until the item becomes pickable. |
| `MaxTimeForPickUp` | `TimeSpan` | Max wait before pickable (currently `TimeSpan.Zero`). |

### `ItemsOnGroundLabelElement`

`ItemsOnGroundLabelElement.cs`. The container that holds all ground-item labels.

| Member | Type | Meaning |
| --- | --- | --- |
| `LabelsOnGround` | `List<LabelOnGround>` | All ground labels with a valid label; `null` if the list is unavailable/malformed. |
| `LabelOnHover` | `Element` | The label element currently under the cursor, or `null`. |
| `ItemOnHover` | `Entity` | The item entity under the cursor, or `null`. |
| `ItemOnHoverPath` | `string` | Metadata path of the hovered item, or `"Null"`. |
| `LabelOnHoverText` | `string` | Text of the hovered label, or `"Null"`. |
| `CountLabels` / `CountLabels2` | `int` | Raw label counters. |

Reached as `IngameUi.ItemsOnGroundLabelElement`; `IngameUi.ItemsOnGroundLabels` is the
shortcut to `LabelsOnGround`. (There is no `LabelsOnGroundVisible` member — filter
`LabelsOnGround` on `IsVisible` yourself.)

Real usage (adapted from `exApiTools/FullRareSetManager`,
`FullRareSetManagerCore.cs`):

```csharp
var groundItems = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
    .Where(y => y?.ItemOnGround != null)
    .GroupBy(y => y.ItemOnGround.Address)
    .ToDictionary(y => y.Key, y => y.First());
```

### `HoverItemIcon`

`HoverItemIcon.cs`. Describes the item currently hovered, resolving the right tooltip,
frame and item across inventory / ground / chat contexts.

| Member | Type | Meaning |
| --- | --- | --- |
| `ToolTipType` | `ToolTipType` | Which kind of tooltip is showing (`None`, `InventoryItem`, `ItemOnGround`, `ItemInChat`). |
| `Tooltip` | `Element` | The active tooltip element for the current `ToolTipType`. |
| `ItemFrame` | `Element` | The item frame element, or `null`. |
| `Item` | `Entity` | The hovered item [entity](entities.md), or `null`. |
| `InventoryItemTooltip` / `ItemInChatTooltip` | `Element` | Per-context tooltip elements. |
| `ToolTipOnGround` | `ItemOnGroundTooltip` | The ground-item tooltip. |
| `InventPosX` / `InventPosY` | `int` | The hovered item's cell position within its inventory. |

### `HPbarElement`

`HPbarElement.cs`. A monster health-bar overlay.

| Member | Type | Meaning |
| --- | --- | --- |
| `MonsterEntity` | `Entity` | The monster this bar belongs to. |
| `Children` | `List<HPbarElement>` | Child bars, re-typed as `HPbarElement`. |

### `SkillBarElement` & `SkillElement`

`SkillBarElement.cs` — the skill bar.

| Member | Type | Meaning |
| --- | --- | --- |
| `TotalSkills` | `long` | Slot count (`ChildCount`). |
| `this[int k]` | `SkillElement` | The skill in slot `k`. |

`SkillElement.cs` — one skill slot.

| Member | Type | Meaning |
| --- | --- | --- |
| `isValid` | `bool` | Whether the slot points at valid skill data. |
| `IsAssignedKeyOrIsActive` | `bool` | `true` while bound to a key or active (handy for auras/golems). |
| `SkillIconPath` | `string` | Path of the skill icon. |
| `totalUses` | `int` | Times used (resets on area change). |
| `isUsing` | `bool` | Whether currently being used (channelling skills). |

### `Map`

`Map.cs`. The map element — exposes both the large map and the minimap.

| Member | Type | Meaning |
| --- | --- | --- |
| `LargeMap` | `Element` | The large map element. |
| `SmallMiniMap` | `Element` | The minimap element. |
| `LargeMapShiftX/Y`, `LargeMapZoom` | `float` | Large-map pan/zoom. |
| `SmallMinMapX/Y`, `SmallMinMapZoom` | `float` | Minimap pan/zoom. |
| `OrangeWords` / `BlueWords` | `Element` | Map text overlays. |

### `SubterraneanChart` & `DelveElement`

`SubterraneanChart.cs` — the Delve window. `GridElement` returns the `DelveElement` grid
(or `null`).

`DelveElement.cs` — the Delve mine grid and its cells:

| Type | Key members |
| --- | --- |
| `DelveElement` | `Cells` (`IList<DelveBigCell>`). |
| `DelveBigCell` | `Cells` (`IList<DelveCell>`), `Text`, `TypePtr`. |
| `DelveCell` | `Mods`, `MinesText`, `Type`, `TypeHuman`, `Info`, `Text`. |
| `DelveCellInfoStrings` | `TestString`…`TestString5`, `Interesting` (`RemoteMemoryObject`). |

### `WorldMapElement`

`WorldMapElement.cs`. The world/area map UI. `Panel` returns the underlying panel
element. Used for both `IngameUi.WorldMap` and `IngameUi.AreaInstanceUi`.

### `PoeChatElement`

`PoeChatElement.cs`. The chat panel.

| Member | Type | Meaning |
| --- | --- | --- |
| `TotalMessageCount` | `long` | Message count (`ChildCount`). |
| `this[int index]` | `EntityLabel` | The chat message at `index`, or `null`. |

### `WindowState`

`WindowState.cs`. A thin element exposing a window's local visibility flag via a
**`new IsVisibleLocal`** (`bool`) read at a fixed offset, shadowing the base property.

### `IncursionWindow`

`IncursionWindow.cs`. The Incursion window.

| Member | Type | Meaning |
| --- | --- | --- |
| `AcceptElement` | `Element` | The "enter incursion" button (via `GetChildFromIndices(3, 13, 2)`), or `null`. |
| `Reward1` / `Reward2` | `string` | Reward text (`GetChildFromIndices(3, 13, 3/4).Text`). |

### Ground-item tooltip — `ItemOnGroundTooltip`

`ItemOnGroundTooltip.cs`. Wraps the tooltip shown for an item on the ground:
`ItemFrame`, `Tooltip`, `TooltipUI` (all `Element`, each resolved via child indices).

> **Inventory & stash item containers** — `InventoryElement`, `NormalInventoryItem` and
> the `StashElement` tabs are documented in [inventories.md](inventories.md), not here.

---

## Example: outline an element and read its text

Find an element, draw a box around it, and label it with its `Text`. This combines
`GetClientRect` (screen-space rectangle) with the [graphics helpers](graphics.md). The
rect from `GetClientRect` is in overlay space; add the game window position to align with
the game when you need game-window coordinates.

```csharp
public override void Render()
{
    var ui = GameController.Game.IngameState.IngameUi;

    // Walk to a specific element by child indices (stable within a session).
    var tabLabel = ui.StashElement?.GetChildFromIndices(0, 1, 2);
    if (tabLabel == null || !tabLabel.IsVisible)
        return;

    // Screen-space rectangle. Use GetClientRectCache in hot paths.
    RectangleF rect = tabLabel.GetClientRectCache;

    // Draw a frame around it (SharpDX.Color).
    Graphics.DrawFrame(rect, Color.Yellow, 2);

    // Read and draw the element's text.
    var text = tabLabel.Text;
    if (!string.IsNullOrEmpty(text))
        Graphics.DrawText(text, rect.TopLeft.ToVector2Num(), Color.White);
}
```

The `DrawFrame(element.GetClientRect…, color, thickness)` idiom is exactly how real
plugins outline UI — e.g. `exApiTools/FullRareSetManager` draws
`Graphics.DrawFrame(foundItem.GetClientRect(), Color.Yellow, 2)`, and
`DetectiveSquirrel/NPCInvWithLinq` outlines stash tab names with
`Graphics.DrawFrame(tabNameElement.GetClientRectCache, ...)`.

## Source

- `Core/PoEMemory/Element.cs` — the `Element` base class.
- `GameOffsets/ElementOffsets.cs` — raw offsets backing `Element`.
- `Core/PoEMemory/RemoteMemoryObject.cs` — `Address`, `M`, `TheGame`, materialization helpers.
- `Core/PoEMemory/MemoryObjects/IngameUIElements.cs` — named UI roots (`IngameUi`).
- `Core/PoEMemory/Elements/` — `EntityLabel`, `LabelOnGround`, `ItemsOnGroundLabelElement`,
  `HoverItemIcon`, `HPbarElement`, `SkillBarElement`, `SkillElement`, `Map`,
  `SubterraneanChart`, `DelveElement`, `WorldMapElement`, `PoeChatElement`, `WindowState`,
  `IncursionWindow`, `ItemOnGroundTooltip`.
