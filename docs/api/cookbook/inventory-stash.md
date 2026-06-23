# Recipe: inventory, stash & vendor automation

Patterns for enumerating inventory/stash items, mapping grid cells to screen rectangles,
driving ctrl-click moves, switching stash tabs, and the chaos-recipe / vendor selection
logic, distilled from real plugins and adapted to this fork.

- [API reference index](../README.md)
- [cookbook index](README.md)

See also [inventories.md](../inventories.md) for the type model, [ingame-state.md](../ingame-state.md)
for `ServerData` / `IngameUi`, and [input-automation.md](input-automation.md) for the cursor /
key plumbing these recipes lean on.

> **Verify before you cite.** Some reference plugins target newer ExileApi distributions and
> use members this fork lacks (`GuildStashElement`, `SellWindowHideout`, `TradeWindow`,
> `UIHoverElement`, `WaitFunctionTimed`, `RectangleF.ClickRandomNum()`,
> `InventoryIndex.PlayerExpandedInventory`, `Inventory.NestedVisibleInventoryIndex`). Where a
> recipe below leans on one, the fork equivalent is shown. Several of these gaps
> (`SellWindowHideout`, `TradeWindow`, `UIHoverElement`, `PlayerExpandedInventory`,
> `NestedVisibleInventoryIndex`) are catalogued in
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md); the rest were
> confirmed absent by grepping `Core/` while writing this recipe.

---

## 1. Enumerate items

Two sources, picked by what you need (full detail in [inventories.md](../inventories.md)):

- **UI layer** (`Inventory.VisibleInventoryItems` → `NormalInventoryItem`) — only what is
  rendered; cheapest when a panel is open. Each slot already has a laid-out
  `GetClientRect()` / `GetClientRectCache`.
- **Server-data layer** (`ServerData.PlayerInventories[…].Inventory.InventorySlotItems` →
  `ServerInventory.InventSlotItem`) — available even with the panel closed; carries grid
  coordinates (`PosX/PosY/SizeX/SizeY`) and computes its own screen rect.

### Carried bag, server side

```csharp
// HighlightedItems / Stashie read the player's bag this way.
var inventory = GameController.IngameState.ServerData.PlayerInventories[0].Inventory;
foreach (var slot in inventory?.InventorySlotItems ?? [])
{
    if (slot.Item == null || slot.Address == 0) continue;
    var item = slot.Item;                  // Entity — read components here
    var rect = slot.GetClientRect();       // screen rect, computed vs the bag element cell size
}
```

`InventSlotItem.GetClientRect()` is the convenient bridge: it sizes itself against the
player-inventory element's cell, so you do not have to do the cell math yourself when the
panel is open.

### Visible stash tab, UI side

```csharp
var ui = GameController.IngameState.IngameUi;
if (ui.StashElement is { IsVisible: true, VisibleStash: { } tab } &&
    tab.VisibleInventoryItems is { } slots)
{
    foreach (var slot in slots)               // IList<NormalInventoryItem>
    {
        var item = slot.Item;                 // Entity
        var rect = slot.GetClientRectCache;   // cached screen rect
    }
}
```

> HighlightedItems also branches on `GuildStashElement` here. That element does **not** exist
> in this fork — use `StashElement` only (see the compatibility note above).

### Reading item facts off each entity

Component access is the in-repo API (see [components-items.md](../components-items.md)); the
filter DSLs the plugins use (`ItemFilterLibrary`, `ItemQuery`) are external libraries.

```csharp
var mods = item.GetComponent<Mods>();                 // ItemRarity, Identified, ItemLevel, ItemMods
var baseC = item.GetComponent<Base>();                // ItemCellsSizeX/Y, isElder, isShaper
var stack = item.GetComponent<Stack>()?.Size;         // null when not stackable
var bit = GameController.Files.BaseItemTypes.Translate(item.Path); // ClassName / BaseName / Width / Height
```

---

## 2. Map a grid cell to a screen rectangle

When you have an item slot, prefer its own rect (`slot.GetClientRect()` /
`InventSlotItem.GetClientRect()`). When you only have grid coordinates (e.g. you computed a
free cell), derive the rect from the grid element. Both helpers below are lifted from Stashie.

```csharp
// Cell -> click point, from the inventory grid element.
static Vector2 CellCenter(Inventory grid, int cellX, int cellY)
{
    var rect = grid.InventoryUIElement.GetClientRect();   // the grid's element
    var cell = rect.Width / 12f;                          // 12-wide carried bag
    return rect.TopLeft + new Vector2(cell * (cellX + 0.5f), cell * (cellY + 0.5f));
}

// Stashie's variant straight from a server slot's grid position.
Vector2 ClickPos(ServerInventory.InventSlotItem item)
{
    var panel = GameController.IngameState.IngameUi
        .InventoryPanel[InventoryIndex.PlayerInventory].GetClientRect();
    var cw = panel.Width / 12;
    var ch = panel.Height / 5;
    var p = item.InventoryPosition;          // SharpDX Vector2, top-left grid cell
    return new Vector2(panel.X + cw / 2 + p.X * cw, panel.Y + ch / 2 + p.Y * ch);
}
```

Use `Inventory.TotalBoxesInInventoryRow` instead of the literal `12` for stash tabs, since a
quad tab is 24 wide. All these rects are window-client coordinates; add the window offset
before moving the cursor (next section). `RectangleF` / `Vector2` here are SharpDX.

### Tracking free / occupied cells

HighlightedItems and Stashie both rasterize items into a `bool[,]` / `int[,]` grid to find
empty slots or to detect a full bag:

```csharp
// "Is the carried bag full?" — mark every cell an item covers, then look for a gap.
var occupied = new bool[12, 5];
foreach (var it in inventory.InventorySlotItems)
    for (var x = it.PosX; x < it.PosX + it.SizeX; x++)
    for (var y = it.PosY; y < it.PosY + it.SizeY; y++)
        if (x is >= 0 and < 12 && y is >= 0 and < 5)
            occupied[x, y] = true;

bool full = !occupied.Cast<bool>().Any(c => !c);
```

The same grid drives a user-editable "ignored cells" mask (`Settings.IgnoredCells[y, x]`),
which both plugins draw as a 12×5 ImGui checkbox grid and consult before moving an item.
Stashie even seeds the mask from the current bag layout (`SaveIgnoredSlotsFromInventoryTemplate`)
by walking `InventorySlotItems` and stamping each item's `Base.ItemCellsSizeX/Y`.

---

## 3. Ctrl-click move flow

The canonical "ctrl-click each item to the other window" loop: hold control once, then
left-click each slot in a stable order, releasing control at the end. The cursor move is
client-rect-center plus the window offset (see [input-automation.md](input-automation.md)).

```csharp
private SharpDX.Vector2 WindowOffset =>
    GameController.Window.GetWindowRectangleTimeCache.TopLeft;

// HighlightedItems: stash tab -> bag, ordered top-left to bottom-right.
private async SyncTask<bool> MoveItemsToInventory(List<NormalInventoryItem> items)
{
    Input.KeyDown(Keys.LControlKey);
    await Wait(KeyDelay);
    foreach (var item in items.OrderBy(i => i.GetClientRectCache.X)
                              .ThenBy(i => i.GetClientRectCache.Y))
    {
        // Abort if a window closed mid-loop, or the bag filled up.
        if (!GameController.IngameState.IngameUi.StashElement.IsVisible) break;
        if (!GameController.IngameState.IngameUi.InventoryPanel.IsVisible) break;

        var screen = item.GetClientRect().Center + WindowOffset;
        Input.SetCursorPos(screen);
        await Wait(MouseMoveDelay);
        Input.LeftDown();  await Wait(MouseDownDelay);
        Input.LeftUp();    await Wait(MouseUpDelay);
    }
    Input.KeyUp(Keys.LControlKey);
    return true;
}
```

Notes drawn from the plugins:

- **Order matters.** Sort by client-rect `X`/`Y` (UI side) or `PosX`/`PosY` (server side) so
  the cursor sweeps deterministically and the game keeps up.
- **Re-check each iteration.** Both source and target panels can close; the bag can fill.
  Bail out rather than clicking into the void.
- **Restore the cursor.** Save the pre-move position and `Input.SetCursorPos` it back at the
  end (Stashie uses `Input.ForceMousePosition` before, `SetCursorPos` after).
- **User cancel.** HighlightedItems aborts the whole task if the right mouse button is held
  (`Control.MouseButtons & MouseButtons.Right`).
- **Affinity bypass.** Holding **Shift** while clicking forces the item into the *visible*
  tab instead of an affinity-matched one — Stashie wraps the click in
  `Input.KeyDown(Keys.ShiftKey)` / `KeyUp` per item when the filter result asks for it.

> `Input` here is the fork's static class ([input.md](../input.md)); `Input.Click`,
> `LeftDown/Up`, `SetCursorPos`, `KeyDown/Up`, `KeyPress` are all present. Plugins that ship
> their own `Mouse`/`Keyboard` P/Invoke wrappers are just re-implementing these.

---

## 4. Switch stash tabs

`StashElement.IndexVisibleStash` is the current tab; `AllStashNames` / `AllInventories` /
`GetStashInventoryByIndex(int)` cover the rest. Stashie's two-path switcher (verified members
all exist in this fork):

```csharp
public IEnumerator SwitchToTab(int target)
{
    var current = GameController.IngameState.IngameUi.StashElement.IndexVisibleStash;
    var distance = Math.Abs(target - current);
    if (distance == 0) yield break;

    if (alwaysUseArrows || distance < 2 || !SliderPresent())
        yield return SwitchViaArrowKeys(target);     // Left/Right key per step
    else
        yield return SwitchViaDropdown(target);      // open "view all", click label, scroll
}

private IEnumerator SwitchViaArrowKeys(int target, int tries = 1)
{
    if (tries >= 3) yield break;
    var current = GameController.IngameState.IngameUi.StashElement.IndexVisibleStash;
    var step = target < current ? Keys.Left : Keys.Right;
    for (var i = 0; i < Math.Abs(target - current); i++)
        yield return Input.KeyPress(step);
    // Verify and retry — the game may not have caught every keypress.
    if (GameController.IngameState.IngameUi.StashElement.IndexVisibleStash != target)
        yield return SwitchViaArrowKeys(target, tries + 1);
}
```

The dropdown path clicks `StashElement.ViewAllStashButton`, waits for
`ViewAllStashPanel.IsVisible`, then clicks the tab label under
`ViewAllStashPanel` and scrolls (`WinApi.mouse_event(Input.MOUSE_EVENT_WHEEL, …, clicks*120, …)`)
when there are more than the ~31 sidebar-visible tabs.

After switching, **wait for the tab to load** before reading or clicking — a freshly visited
tab's inventory is `null` until the client fetches it. Poll with the fork's `WaitTime`
coroutine helper (see [input.md](../input.md)); a freshly-loaded tab also reports
`InvType == InventoryType.InvalidInventory` until ready:

```csharp
var stash = GameController.IngameState.IngameUi.StashElement;
for (var ms = 0; ms < 2000 &&
        (stash.AllInventories[target] == null ||
         stash.VisibleStash?.InvType == InventoryType.InvalidInventory); ms += 50)
    yield return new WaitTime(50);
```

> Stashie itself uses `WaitFunctionTimed(predicate, …)` for this; that helper is **not** in
> this fork, so the poll-and-`WaitTime` loop above is the equivalent
> (see [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md)).

> Stashie keeps a background coroutine (`StashTabNamesUpdater_Thread`) polling
> `AllStashNames` while in town/hideout to keep its tab-name → index map fresh, since the
> player can rename or reorder tabs.

---

## 5. Split a stack (shift-click + type amount)

To move *part* of a stack, Stashie shift-clicks the source (opens the amount prompt), types
the number digit-by-digit, presses Enter, then clicks the destination:

```csharp
private IEnumerator SplitStack(int amount, Vector2 from, Vector2 to)
{
    Input.KeyDown(Keys.ShiftKey);
    yield return Input.SetCursorPositionSmooth(from + _clickWindowOffset);
    Input.Click(MouseButtons.Left);            // shift-click opens the split dialog
    Input.KeyUp(Keys.ShiftKey);

    amount = Math.Min(amount, 40);
    foreach (var d in amount.ToString())       // type the count, one digit at a time
        yield return Input.KeyPress((Keys)((int)Keys.D0 + (d - '0')));
    yield return Input.KeyPress(Keys.Enter);

    yield return Input.SetCursorPositionSmooth(to + _clickWindowOffset);
    Input.Click(MouseButtons.Left);            // drop the split-off stack
}
```

(`Input.SetCursorPositionSmooth` and `Input.KeyPress` are coroutine helpers, see
[input.md](../input.md).)

---

## 6. Chaos-recipe set logic (FullRareSetManager)

Classify each rare into a recipe slot, then count how many complete sets you can form. A
chaos set needs one of each: weapon, helmet, body, gloves, boots, belt, ring×2, amulet —
and at least one piece below item level 75 ("low") for a *chaos* set vs an all-≥75 *regal*
set.

### Classify one item

```csharp
private StashItem ProcessItem(Entity item)
{
    var mods = item.GetComponent<Mods>();
    if (mods?.ItemRarity != ItemRarity.Rare) return null;   // recipe is rares only
    if (mods.Identified && !allowIdentified) return null;   // unid only, usually
    if (mods.ItemLevel < 60) return null;                   // 60-74 still qualifies

    var bit = GameController.Files.BaseItemTypes.Translate(item.Metadata);
    return new StashItem
    {
        LowLvl    = mods.ItemLevel < 75,                    // <75 = "low" tier piece
        ItemClass = bit.ClassName,
        ItemType  = ClassToSlot(bit.ClassName),             // Helmet/Body/Ring/…
        Width = bit.Width, Height = bit.Height,
    };
}
```

`ClassToSlot` maps `BaseItemType.ClassName` to a slot: `"Two Hand*"` → two-handed weapon,
`"One Hand*"` / `Wand` / `Dagger` / `Sceptre` / `Claw` / `Shield` → one-handed,
`Bow` / `Staff` → two-handed, then `Ring` / `Amulet` / `Belt` / `Helmet` / `Body Armour` /
`Boots` / `Gloves`. Optionally reject Shaper/Elder bases via `Base.isElder` / `Base.isShaper`.

### Count sets

The weapon slot is the tricky one (a set takes either one 2H *or* two 1H). FRSM's counters:

```csharp
// Low-tier weapon sets available (used for chaos sets).
int LowWeaponSets()
{
    int twoH = twoHandedLow.Count;
    int leftover = oneHandedLow.Count - oneHandedHigh.Count;
    int oneH = leftover <= 0
        ? oneHandedLow.Count
        : oneHandedHigh.Count + leftover / 2;   // pair a high with a low, then low+low
    return twoH + oneH;
}
```

Across all eight slots: chaos sets = `min(minSlotTotal, sum(lowWeaponCount + lowPieceCount))`,
i.e. limited both by the scarcest slot and by how many *low*-tier pieces you have. FRSM then
highlights the items to grab with `Graphics.DrawFrame(item.GetClientRect(), Color.Yellow, 2)`
(see [graphics.md](../graphics.md)) and uses the move flow from §3 to assemble the set.

> Upstream FRSM reads `Inventory.NestedVisibleInventoryIndex` to find the open stash; in this
> fork use `StashElement.IndexVisibleStash` / `VisibleStash` instead (see
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md)).

---

## 7. Vendor selection (EZVendor)

Decide what to sell, prune bad combos, then ctrl-click-sell. The selection itself is plain
component reads; the value lookups (poe.ninja, div-card filters) are external.

### Detect a stuck item on the cursor

Before/after a sell loop, check whether the cursor is holding an item — a server inventory of
type `Cursor` with exactly one entry means a move got stuck:

```csharp
private bool IsCursorWithItem()
{
    var cursor = GameController.IngameState.ServerData.PlayerInventories
        .Select(h => h?.Inventory)
        .FirstOrDefault(inv => inv?.InventType == InventoryTypeE.Cursor);
    return cursor?.Items?.Count == 1
        && !string.IsNullOrEmpty(cursor.Items[0]?.Path);
}
```

EZVendor aborts the run if this stays true for >5 s, so a hung move can't stall everything.

### Build the sell list and prune bad recipes

```csharp
foreach (var invItem in GetPlayerInventory().VisibleInventoryItems)   // NormalInventoryItem
{
    var item = invItem.Item;
    if (!item.HasComponent<Base>()) continue;
    if (ShouldVendor(item))                       // your value/filter logic
        vendorList.Add(invItem);
}
```

Then drop combinations the vendor would turn into something worse, e.g.:

- **Cap "5-to-1" bases at 4.** Count by `Item.Path`; if a base appears >4 times, remove the
  excess so you don't accidentally feed the same-base recipe.
- **Avoid prismatic-ring/transmute/whetstone side recipes** by removing the catalysing
  currency from the list when the matching items are present.

### Click to sell, verifying hover

EZVendor verifies the cursor is actually over the target before clicking — robust against lag.
It uses upstream-only members (`UIHoverElement`, `RectangleF.ClickRandomNum()`); the fork
equivalents are `IngameState.UIHover` and `GetClientRect().Center`:

```csharp
private IEnumerator ClickItem(Element invItem, MouseButtons button)
{
    for (var tries = 0; tries < 10; tries++)
    {
        Input.SetCursorPos(invItem.GetClientRect().Center + WindowOffset);
        yield return new WaitTime(delay);
        if (GameController.IngameState.UIHover.Address == invItem.Address) // fork: UIHover
        {
            Input.Click(button);
            break;
        }
    }
}
```

The sell loop holds `Keys.LControlKey`, calls `ClickItem` for each, then confirms via
`SellWindow.AcceptButton` (the fork's `SellWindow` exposes `AcceptButton` / `CancelButton`).

> Upstream EZVendor also references `SellWindowHideout` and `TradeWindow`, neither of which
> exists in this fork — use `IngameUi.SellWindow` (see
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md)).

---

## Source repos

- [exApiTools/HighlightedItems](https://github.com/exApiTools/HighlightedItems) —
  `HighlightedItems.cs` (ctrl-click move loop, ignored-cells grid, inventory-full check,
  custom-filter highlighting).
- [exApiTools/Stashie](https://github.com/exApiTools/Stashie) — `Stashie.cs` (tab switching
  via arrows/dropdown, drop-to-stash loop, shift affinity bypass, stack splitting,
  cell→click math, tab-name updater).
- [exApiTools/StashieV2](https://github.com/exApiTools/StashieV2) — `Stashie.cs`,
  `Compartments/ActionsHandler.cs` (same flows, refactored into compartments).
- [exApiTools/FullRareSetManager](https://github.com/exApiTools/FullRareSetManager) —
  `FullRareSetManagerCore.cs`, `SetParts/WeaponItemsSetPart.cs` (chaos-recipe classification
  and set counting).
- [vadash/EZVendor](https://github.com/vadash/EZVendor) — `EZVendorCore.cs` (cursor-stuck
  detection, vendor selection, recipe pruning, hover-verified click).
- [exApiTools/AutoSextant](https://github.com/exApiTools/AutoSextant) — `NInventory/`,
  `NStash/`, `Cursor/Cursor.cs` (alternative inventory/stash abstractions and a cursor model).
