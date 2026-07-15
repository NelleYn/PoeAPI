# Inventory & stash UI

Reading the player inventory grid and the stash window: enumerating on-screen item
slots, mapping them to item entities, and reaching the server-side inventory data model.
See the [API reference index](README.md).

These types come in two layers:

- **UI layer** (`ExileCore.PoEMemory.Elements` / `.Elements.InventoryElements`) — the
  rendered grids and item slots. They are [`Element`](ui-elements.md)s, so you get
  `GetClientRect()`, `Children`, `IsVisible` for free, and each item slot exposes the
  item [`Entity`](components-items.md). Only populated while the relevant panel is open.
- **Server-data layer** (`ExileCore.PoEMemory.MemoryObjects`) — the inventory contents the
  game fetched from the server. Available even when the panel is closed. Reached from
  [`ServerData`](ingame-state.md).

---

## InventoryElement — the player's equipped + carried inventories

`ExileCore.PoEMemory.Elements.InventoryElement` is the panel reached from
`GameController.IngameState.IngameUi.InventoryPanel`. It is an indexer over the player's
inventories keyed by `InventoryIndex`:

```csharp
public Inventory this[InventoryIndex k] => AllInventories[k];
```

`AllInventories` is read lazily as an [`InventoryList`](#inventorylist) at offset `0x340`.
There is no public bulk enumerator on `InventoryElement` itself — index in by slot.

`InventoryIndex` (`ExileCore.Shared.Enums`) names each equipment slot plus the carried grid:

```
None, Helm, Amulet, Chest, LWeapon, RWeapon, LWeaponSwap, RWeaponSwap,
LRing, RRing, Gloves, Belt, Boots, PlayerInventory, Flask
```

The carried 12-wide grid is `InventoryIndex.PlayerInventory`:

```csharp
var inv = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
foreach (var slot in inv.VisibleInventoryItems)   // IList<NormalInventoryItem>
{
    var entity = slot.Item;                        // the item Entity
    var rect   = slot.GetClientRect();             // on-screen position
}
```

> There is no separate `InventoryElements.cs` type — `InventoryElement` (singular) is the
> only inventory-panel element. The plural shows up only as the namespace
> `ExileCore.PoEMemory.Elements.InventoryElements`, which holds the item-slot wrappers below.

---

## Inventory — one grid (memory object)

`ExileCore.PoEMemory.MemoryObjects.Inventory` is itself an `Element`. It is what
`InventoryElement[...]`, `StashElement.VisibleStash` and `StashElement.AllInventories[...]`
return. It is the bridge between the UI grid and the per-cell layout.

| Member | Type | Notes |
| --- | --- | --- |
| `ItemCount` | `long` | Number of items in the grid. |
| `TotalBoxesInInventoryRow` | `long` | Grid width in cells (e.g. 12 normal, 24 quad). |
| `HoverItem` | `NormalInventoryItem` | Item currently under the cursor, or `null`. |
| `X` / `Y` | `int` | Real grid origin. |
| `XFake` / `YFake` | `int` | Fake/animated origin. |
| `CursorHoverInventory` | `bool` | True when the cursor is over this inventory. |
| `InvType` | `InventoryType` | Detected kind (see enum below). |
| `InventoryUIElement` | `Element` | The element to draw against / iterate (varies by tab type). |
| `VisibleInventoryItems` | `IList<NormalInventoryItem>` | Item slots currently rendered. **`null`** when the grid is not in view. |
| `this[int x, int y, int xLength]` | `Entity` | Item entity at grid cell `(x, y)`; `null` if empty. Works even when the grid isn't on screen, as long as the client has the data. |

`VisibleInventoryItems` is the workhorse for on-screen iteration. Internally it switches on
`InvType` and returns the matching typed slot wrapper (e.g. `CurrencyInventoryItem` for a
currency tab); the result is typed as the base `NormalInventoryItem`. Empty / not-yet-loaded
tabs can yield `null`, so null-check before iterating.

`InventoryType` (`ExileCore.Shared.Enums`) — the UI-side tab kind:

```
InvalidInventory, PlayerInventory, NormalStash, QuadStash, CurrencyStash,
EssenceStash, DivinationStash, MapStash, FragmentStash, DelveStash
```

---

## NormalInventoryItem — one on-screen item slot

`ExileCore.PoEMemory.Elements.InventoryElements.NormalInventoryItem` wraps a single item
slot. It derives from [`Element`](ui-elements.md), so `GetClientRect()`, `Parent`,
`Children`, `Address` are inherited.

| Member | Type | Notes |
| --- | --- | --- |
| `Item` | `Entity` | The item [`Entity`](components-items.md) in this slot. Cached; check `Item.Address != 0` for empty slots. |
| `InventPosX` / `InventPosY` | `int` (`virtual`) | Grid coordinates of the slot's top-left cell. |
| `ItemWidth` / `ItemHeight` | `int` (`virtual`) | Item size in grid cells. |
| `GetClientRect()` | `RectangleF` | Inherited from `Element` — the screen rectangle for the slot. |
| `toolTipType` | `ToolTipType` | Always `ToolTipType.InventoryItem`. |

> The size members are named `ItemWidth` / `ItemHeight` (not `Width` / `Height`). The
> underlying offset struct fields are `Width` / `Height`, but the public properties read
> them through `ItemWidth` / `ItemHeight`.

`GetClientRect()` returns a SharpDX `RectangleF` in overlay/screen coordinates — pass it
straight to [`Graphics`](graphics.md) to draw a highlight (see [the loop](#the-common-plugin-loop)).

### Typed tab-item variants

The special stash tabs lay items out differently, so each gets a `NormalInventoryItem`
subclass (same namespace) that overrides positioning. `VisibleInventoryItems` returns the
right one automatically based on `InvType`.

| Type | Overrides | What it changes |
| --- | --- | --- |
| `CurrencyInventoryItem` | `InventPosX`/`Y` → `0`; `GetClientRect()` → `Parent.GetClientRect()` | Currency tab items are fixed; grid coords are meaningless, so the rect is the parent cell's. |
| `EssenceInventoryItem` | same as Currency | Essence tab fixed-slot layout. |
| `FragmentInventoryItem` | same as Currency | Fragment tab fixed-slot layout. |
| `DelveInventoryItem` | same as Currency | Delve resonator/fossil tab fixed-slot layout. |
| `DivinationInventoryItem` | `InventPosX`/`Y` → `0`; `GetClientRect()` → parent rect minus the tab's scroll offset | Divination tab scrolls, so the rect is adjusted by the scrollbar value. |

For all of these, do not rely on `InventPosX`/`InventPosY` — they are forced to `0`. Use
`Item` plus `GetClientRect()`.

### MapStashTabElement — the map stash tab

`MapStashTabElement` (same namespace) is a different shape: it is an `Element` that reads
map counts rather than wrapping a single slot. When `InvType == InventoryType.MapStash`,
`Inventory.InventoryUIElement` returns its near-identical twin
`ExileCore.PoEMemory.MemoryObjects.MapStashTabElement` (same members — cast to that type,
not to the `InventoryElements` one documented here). Reach it that way:
`IngameUi.MapStashTab` currently returns `null` because it has no verified offset in
`GameOffsets/IngameUElementsOffsets.cs`.

| Member | Type | Notes |
| --- | --- | --- |
| `TotalInventories` | `int` | Number of map sub-inventories stored. |
| `MapsCount` | `Dictionary<MapSubInventoryKey, MapSubInventoryInfo>` | Stored maps read from memory, keyed by path + `MapType`. |
| `MapsCountByName` | `Dictionary<string, string>` | Maps as `"tier: name" -> count`, ordered by tier. |
| `MapsCountByTier` | `Dictionary<string, string>` | Tier → count, read from the rendered UI. |
| `CurrentCell` | `Dictionary<string, string>` | Contents of the selected cell, by item name. |

Supporting types in the same namespace: `MapSubInventoryInfo` (`Count`, `MapName`, `Tier`),
`MapSubInventoryKey` (`Path`, `Type`), and the `MapType` enum
(`Normal, Shaped, Unknown2, Unknown3, Unique`).

---

## StashElement — the stash window

`ExileCore.PoEMemory.Elements.StashElement`, reached from
`GameController.IngameState.IngameUi.StashElement`, is the open stash. It is an `Element`.

| Member | Type | Notes |
| --- | --- | --- |
| `TotalStashes` | `long` | Number of stash tabs. |
| `IndexVisibleStash` | `int` | Index of the currently shown tab. |
| `VisibleStash` | `Inventory` | The currently shown tab's [`Inventory`](#inventory--one-grid-memory-object). |
| `AllStashNames` | `IList<string>` | Names of all tabs (by index). |
| `AllInventories` | `IList<Inventory>` | One `Inventory` per tab (may contain `null` for tabs not yet loaded). |
| `GetStashInventoryByIndex(int)` | `Inventory` | Tab inventory by index; `null` if out of range / not loaded. |
| `GetStashName(int)` | `string` | Tab name by index; empty if out of range. |
| `ExitButton` | `Element` | The close button. |
| `ViewAllStashButton` / `ViewAllStashPanel` | `Element` | "View all stashes" controls. |
| `MoveStashTabLabelsLeft_Button` / `..Right_Button` | `Element` | Tab-label scroll buttons. |

`AllInventories` and `GetStashInventoryByIndex` only return a populated `Inventory` for tabs
the client has actually loaded (i.e. visited); other entries are `null`. The reliable
always-available tab is `VisibleStash`.

```csharp
var stash = GameController.IngameState.IngameUi.StashElement;
var tab   = stash.VisibleStash;                       // current tab's Inventory
if (tab?.VisibleInventoryItems != null)
    foreach (var slot in tab.VisibleInventoryItems) { /* slot.Item, slot.GetClientRect() */ }
```

---

## Server-side inventory memory objects

These live under `ExileCore.PoEMemory.MemoryObjects` and back
[`ServerData`](ingame-state.md)'s inventory lists. They expose contents independent of any
open UI panel.

### InventoryHolder

`InventoryHolder` (a `RemoteMemoryObject`) pairs an id with a `ServerInventory`. It is the
element type of `ServerData.PlayerInventories`, `ServerData.NPCInventories`, and
`ServerData.GuildInventories`.

| Member | Type | Notes |
| --- | --- | --- |
| `Id` | `int` | Holder identifier. |
| `Inventory` | `ServerInventory` | The owned server inventory. |
| `StructSize` | `const int` (`0x20`) | Native struct stride (internal; used when reading the array). |

### ServerInventory

`ServerInventory` is the server-side contents of one inventory.

| Member | Type | Notes |
| --- | --- | --- |
| `InventType` | `InventoryTypeE` | Inventory kind (GGPK `InventoryType.dat`). |
| `InventSlot` | `InventorySlotE` | Equipment/stash slot (GGPK `Inventories.dat`). |
| `Columns` / `Rows` | `int` | Grid dimensions. |
| `IsRequested` | `bool` | Whether the client has requested this data. |
| `CountItems` | `long` | Item count (also `TotalItemsCounts` as `int`). |
| `ServerRequestCounter` | `int` | Server fetch counter. |
| `Items` | `IList<Entity>` | All item entities (hash-map walk). |
| `InventorySlotItems` | `IList<InventSlotItem>` | All occupied slots with position + item. |
| `Hash` | `long` | Hash of the slot map. |
| `this[int x, int y]` | `InventSlotItem` | Slot at grid cell `(x, y)`; `null` if empty. |

`ServerInventory.InventSlotItem` (nested) describes one occupied cell:

| Member | Type | Notes |
| --- | --- | --- |
| `Item` | `Entity` | The item [`Entity`](components-items.md). |
| `InventoryPosition` | `Vector2` | Top-left grid cell (SharpDX `Vector2`). |
| `PosX` / `PosY` | `int` | Top-left cell coordinates. |
| `SizeX` / `SizeY` | `int` | Item size in cells. |
| `GetClientRect()` | `RectangleF` | Screen rect, computed against the player inventory element's cell size. |

`InventType` / `InventSlot` use enums distinct from the UI-side `InventoryType`:

- `InventoryTypeE` — `MainInventory, BodyArmour, Weapon, Offhand, Helm, Amulet, Ring,
  Gloves, Boots, Belt, Flask, Cursor, Map, PassiveJewels, AnimatedArmour, Crafting,
  Leaguestone, Unused, Currency, Offer, Divination, Essence, Fragment, MapStashInv,
  UniqueStashInv, CraftingSpreeCurrency, CraftingSpreeItem, NormalOrQuad`.
- `InventorySlotE` — `MainInventory1, BodyArmour1, Weapon1, Offhand1, Helm1, Amulet1,
  Ring1, Ring2, Gloves1, Boots1, Belt1, Flask1, Cursor1, Map1, Weapon2, Offhand2, …`
  (continues through master-crafting and league slots; see the source enum).

> There is no `InventorySlotE` value/type missing here, but note the naming: the UI-side
> kind is `InventoryType`, while the server-data kind is `InventoryTypeE` and the slot is
> `InventorySlotE`. Don't mix them.

### InventoryList

`InventoryList` is a `RemoteMemoryObject` indexer over the player's inventories used inside
`InventoryElement`.

| Member | Type | Notes |
| --- | --- | --- |
| `InventoryCount` | `static int` (`15`) | Number of indexable inventories. |
| `this[InventoryIndex inv]` | `Inventory` | Inventory by index; `null` if out of range. |
| `DebugInventories` | `List<Inventory>` | All inventories (debug helper). |

### Reaching server data

`ServerData` exposes the holders plus lookup helpers:

```csharp
var server = GameController.Game.IngameState.Data.ServerData;
IList<InventoryHolder> player = server.PlayerInventories;   // also NPCInventories, GuildInventories
ServerInventory carried = server.GetPlayerInventoryBySlot(InventorySlotE.MainInventory1);
// also GetPlayerInventoryByType(InventoryTypeE) and GetPlayerInventoryBySlotAndType(...)
```

`InvWithLinq` reads the carried bag this way (adapted):

```csharp
var inventory = GameController?.Game?.IngameState?.Data?.ServerData?.PlayerInventories[0]?.Inventory;
foreach (var slotItem in inventory?.InventorySlotItems ?? [])
{
    if (slotItem.Item == null || slotItem.Address == 0) continue;
    var entity = slotItem.Item;               // read components here
    var rect   = slotItem.GetClientRect();    // draw against this
}
```

---

## The common plugin loop

Enumerate slots, read each item's [components](components-items.md), then draw highlights
with [`Graphics`](graphics.md). Use the **UI layer** when you only care about what's on
screen (cheaper, already laid out); use the **server-data layer** when you need contents of
panels that aren't open.

```csharp
public override void Render()
{
    var ui = GameController.IngameState.IngameUi;

    // 1. Player carried inventory (only when the panel is open).
    if (ui.InventoryPanel.IsVisible)
    {
        var grid  = ui.InventoryPanel[InventoryIndex.PlayerInventory];
        var slots = grid?.VisibleInventoryItems;
        if (slots != null)
            foreach (var slot in slots)
                HighlightIfRare(slot);
    }

    // 2. Currently visible stash tab.
    var tab = ui.StashElement?.VisibleStash;
    if (tab?.VisibleInventoryItems != null)
        foreach (var slot in tab.VisibleInventoryItems)
            HighlightIfRare(slot);
}

private void HighlightIfRare(NormalInventoryItem slot)
{
    var item = slot.Item;                                   // Entity
    if (item == null || item.Address == 0) return;

    var rarity = item.GetComponent<Mods>()?.ItemRarity;     // see components-items.md
    if (rarity == ItemRarity.Rare)
        Graphics.DrawFrame(slot.GetClientRect(), Color.Yellow, 2);  // see graphics.md
}
```

The `slot.Item.GetComponent<...>()` + `Graphics.DrawFrame(slot.GetClientRect(), ...)`
pattern is exactly what `FullRareSetManager` does to outline matched stash items
(`Graphics.DrawFrame(foundItem.GetClientRect(), Color.Yellow, 2)`), and what `InvWithLinq`
does to box filtered inventory items.

Notes:

- `VisibleInventoryItems` can be `null` (panel not loaded / empty special tab) — always
  null-check.
- An item entity may be empty: guard with `slot.Item == null || slot.Item.Address == 0`.
- Grid coords (`InventPosX`/`InventPosY`) are valid for player / normal / quad grids; the
  special-tab subclasses force them to `0`, so rely on `Item` + `GetClientRect()` there.
- `RectangleF` and `Color` are SharpDX types (see [graphics.md](graphics.md)).
- Helper extension methods such as `CustomItemData` or `ItemFilterLibrary` matchers used by
  the reference plugins are **external libraries**, not part of this API. Component access
  (`Entity.GetComponent<T>()`) and `Graphics`/`Element` members shown above are the
  in-repo API.

> Reference plugins built on newer ExileCore forks use members not present in this repo —
> e.g. `InventoryIndex.PlayerExpandedInventory` and `Inventory.NestedVisibleInventoryIndex`
> / `NestedTabSwitchBar`. Those do not exist here; document/target only the members listed
> above for this codebase.

---

## Source

- `Core/PoEMemory/Elements/InventoryElement.cs`
- `Core/PoEMemory/Elements/StashElement.cs`
- `Core/PoEMemory/Elements/InventoryElements/NormalInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/CurrencyInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/EssenceInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/FragmentInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/DelveInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/DivinationInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/MapStashTabElement.cs`
- `Core/PoEMemory/MemoryObjects/Inventory.cs`
- `Core/PoEMemory/MemoryObjects/InventoryList.cs`
- `Core/PoEMemory/MemoryObjects/InventoryHolder.cs`
- `Core/PoEMemory/MemoryObjects/ServerInventory.cs`
- `Core/PoEMemory/MemoryObjects/ServerData.cs` (PlayerInventories / NPCInventories / GuildInventories, lookup helpers)
- `Core/PoEMemory/MemoryObjects/IngameUIElements.cs` (InventoryPanel, StashElement)
- `Core/Shared/Enums/InventoryIndex.cs`, `InventoryType.cs`, `InventoryTypeE.cs`, `InventorySlotE.cs`
