# Recipe: pricing, market data & trade automation

How a plugin fetches external economy data (poe.ninja / TFT), caches it on disk, maps an in‑game item to a price, and shows that value on hover and on ground labels — adapted to this fork.

[API reference index](../README.md) · [cookbook index](README.md)

These patterns are mined from `exApiTools/Get-Chaos-Value` ("Ninja Price"), `MarketWizard`, `MapsExchange` and the `exApiTools/tft-data-prices` data feed. They lean on members documented in [files-in-memory.md](../files-in-memory.md) (`Files.BaseItemTypes`), [components-items.md](../components-items.md) (`Mods`, `Base`, `Stack`, `Sockets`, `RenderItem`) and [caching.md](../caching.md). External price feeds are **not** part of ExileCore — you wire up `System.Net.Http` and JSON yourself.

> External dependencies. Everything that talks to the network here (`HttpClient`, poe.ninja and TFT endpoints, `Newtonsoft.Json`/`System.Text.Json`) is your own plugin code, not an engine API. Treat endpoint URLs and JSON shapes as unstable — poe.ninja changed its overview endpoints between leagues, and TFT renames channels.

---

## 1. Fetch external price data over HTTP

The simplest correct fetch: a fresh `HttpClient` per call, cookies off, returning the body as a string for JSON deserialization. This is exactly what Ninja Price does (`Utils.cs`):

```csharp
public static async Task<string> DownloadFromUrl(string url)
{
    using var handler = new HttpClientHandler { UseCookies = false };
    using var client = new HttpClient(handler);
    return await client.GetStringAsync(url).ConfigureAwait(false);
}
```

> In production prefer a single shared/static `HttpClient` (or `IHttpClientFactory`) to avoid socket exhaustion. The per‑call form above is fine for the infrequent, batched downloads price plugins do (once per reload period), but do not call it per item or per frame.

poe.ninja exposes two endpoint families that price plugins template by league:

```csharp
// Exchange-style (currency, fragments, essences, scarabs, oils, fossils, …)
const string CurrencyUrl =
    "https://poe.ninja/poe1/api/economy/exchange/current/overview?league={0}&type=Currency";
// Stash/item-style (uniques, maps, gems, divination cards, beasts, base types, …)
const string UniqueWeaponsUrl =
    "https://poe.ninja/poe1/api/economy/stash/current/item/overview?league={0}&type=UniqueWeapon";
```

You fan out one request per `type`, deserialize each into a typed root object, and assemble a single in‑memory snapshot (Ninja Price calls it `CollectiveApiData`). All network work runs off the render thread.

### Run it off-thread, guard against overlap

Never block `Render()`/`Tick()` on a download. Kick the reload onto a `Task` and use an `Interlocked` flag so a slow reload can't be started twice:

```csharp
private int _updating;

private void StartDataReload(string league, bool forceRefresh)
{
    if (Interlocked.CompareExchange(ref _updating, 1, 0) != 0)
        return; // a reload is already in progress

    Task.Run(async () =>
    {
        try
        {
            var newData = new CollectiveApiData();
            await LoadData<CurrencyOverviewData.RootObject>(
                "Currency.json", CurrencyUrl, league, tryWeb, t => newData.Currency = t);
            // … one LoadData call per type …
            CollectedData = newData; // publish atomically: swap the whole snapshot in one ref write
        }
        finally
        {
            Interlocked.Exchange(ref _updating, 0);
        }
    });
}
```

Publishing the finished snapshot with a single reference assignment (`CollectedData = newData`) means the render thread always sees either the old complete snapshot or the new complete one — never a half‑filled object.

---

## 2. Cache price JSON on disk (with web/backup fallback)

Network can fail; the game shouldn't lose pricing because poe.ninja is down. Ninja Price keeps a per‑league cache folder under its plugin directory and falls back to the last good copy.

Pick the cache root off the plugin's own directory ([`BaseSettingsPlugin.DirectoryFullName`](../../../Core/BaseSettingsPlugin.cs) — verified in this fork):

```csharp
public override bool Initialise()
{
    NinjaDirectory = Path.Join(DirectoryFullName, "NinjaData");
    Directory.CreateDirectory(NinjaDirectory);
    return true;
}
```

The fetch-or-fallback flow per file: try the web first only when the local copy is stale, otherwise serve the backup, and only hit the web as a last resort.

```csharp
private async Task LoadData<T>(string fileName, string url, string league,
                               bool tryWebFirst, Action<T> dataAction)
{
    var backupFile = Path.Join(NinjaDirectory, league, fileName);

    if (tryWebFirst && await LoadDataFromWeb(fileName, url, league, dataAction, backupFile))
        return;
    if (await LoadDataFromBackup(fileName, dataAction, backupFile))
        return;
    if (!tryWebFirst)
        await LoadDataFromWeb(fileName, url, league, dataAction, backupFile);
}

private async Task<bool> LoadDataFromWeb<T>(string fileName, string url, string league,
                                            Action<T> dataAction, string backupFile)
{
    var data = JsonConvert.DeserializeObject<T>(
        await Utils.DownloadFromUrl(string.Format(url, league)));
    new FileInfo(backupFile).Directory.Create();                 // ensure league subfolder exists
    await File.WriteAllTextAsync(backupFile,
        JsonConvert.SerializeObject(data, Formatting.Indented));   // persist for next launch
    dataAction(data);
    return true;
}
```

Staleness is tracked in a tiny sidecar `meta.json` holding the last load time, compared against a user‑set reload period:

```csharp
private async Task<bool> IsLocalCacheStale(string metadataPath)
{
    if (!File.Exists(metadataPath)) return true;
    var meta = JsonConvert.DeserializeObject<LeagueMetadata>(
        await File.ReadAllTextAsync(metadataPath));
    return DateTime.UtcNow - meta.LastLoadTime
        > TimeSpan.FromMinutes(Settings.DataSourceSettings.ReloadPeriod);
}
```

This is on‑disk persistence across sessions — distinct from the in‑memory, per‑frame [`CachedValue`](../caching.md) types ExileCore provides. Use both: disk cache for the *price data*, a `TimeCache`/`FrameCache` for the *per‑frame item scan* that consumes it (see §5).

### The TFT data feed

`exApiTools/tft-data-prices` (http://data.tftrove.com) is a **data repository**, not a plugin — it has no C# code. It publishes JSON snapshots of TFT bulk prices (services, compasses/sextants, heist contracts, bulk maps/beasts/etc.) under `lsc/` (Softcore) and `std/` (Standard) folders. Each file is `{"timestamp": <epoch-ms>, "data": [{ "name", "divine", "chaos", "lowConfidence", "ratio" }, …]}`. Consume it with the same fetch‑and‑cache flow above; key entries by their `name` field. File paths and display names change with league/channel updates, so don't hard‑code beyond the folder layout.

---

## 3. Map an in-game item to its price key

This is the crux: turn an item `Entity` into the name/category you look the price up by. The keys are an item's **base name** (for currency/maps/gems/etc.) and its **unique name** (for uniques) — both resolved from this fork's parsed data.

### Resolve the base item from metadata

`Entity.Path` is the item's metadata path. Run it through `Files.BaseItemTypes.Translate(...)` to get the base record (see [files-in-memory.md](../files-in-memory.md)). The returned [`BaseItemType`](../../../Core/PoEMemory/Models/BaseItemType.cs) gives you `BaseName`, `ClassName` and `Tags` — all verified in this fork:

```csharp
var bit = GameController.Files.BaseItemTypes.Translate(itemEntity.Path);
if (bit == null) return;                 // unknown metadata → Translate returns null and logs
string baseName  = bit.BaseName  ?? "";  // e.g. "Chaos Orb", "Vaal Temple Map"
string className = bit.ClassName ?? "";  // e.g. "StackableCurrency", "Map", "Active Skill Gem"
```

Use `ClassName` to *classify* the item into a price category (currency vs. scarab vs. divination card vs. unique vs. map…). Ninja Price's `ComputeType` is essentially a big switch on `ClassName`, `BaseName` and `Path`:

```csharp
if (ClassName == "StackableCurrency" && /* not an essence/oil/fossil/… */)
    ItemType = ItemTypes.Currency;
else if (ClassName == "MapFragment" && Path.StartsWith("Metadata/Items/Scarabs/"))
    ItemType = ItemTypes.Scarab;
else if (Path.Contains("Metadata/Items/DivinationCards"))
    ItemType = ItemTypes.DivinationCard;
// … etc.
```

### Read rarity, unique name and stack size from components

From the [`Mods`](../components-items.md) component (verified members `ItemRarity`, `UniqueName`, `Identified`, `ItemLevel`):

```csharp
if (itemEntity.TryGetComponent<Mods>(out var mods))
{
    ItemRarity rarity = mods.ItemRarity;           // Normal/Magic/Rare/Unique/Gem/Currency/…
    bool identified   = mods.Identified;
    string uniqueName = mods.UniqueName;           // "" unless the item is unique
}
```

`ItemRarity` lives in `ExileCore.Shared.Enums` (`Normal, Magic, Rare, Unique, Gem, Currency, Quest, …`). Stack size for currency comes from [`Stack`](../components-items.md) (`Stack.Size`, `Stack.Info.MaxStackSize` — both present in this fork):

```csharp
if (itemEntity.TryGetComponent<Stack>(out var stack))
{
    int size    = stack.Size;
    int maxSize  = stack.Info?.MaxStackSize ?? 0;
}
```

> Unidentified uniques have an empty `UniqueName`. Ninja Price recovers candidate names by mapping the item's art file — `RenderItem.ResourcePath` (verified in this fork) — against a name table it builds once. `RenderItem` exposes only `ResourcePath` here; the art→name table is the plugin's own data, not an engine API.

### Key the price lookup

With a category and a name, look up the snapshot. The exchange‑style roots build a name→price dictionary once and cache it; multiply by stack size:

```csharp
// CurrencyOverviewData.RootObject builds LinesByName lazily, converting each line's
// primary value into a chaos-equivalent using the embedded divine/chaos rate.
var hit = CollectedData.Currency.LinesByName.GetValueOrDefault(baseName);
if (hit != default)
    priceInChaos = stackSize * hit.ChaosEquivalent;
```

For uniques, match the snapshot's lines by `UniqueName` (or the recovered candidates), optionally refined by largest link count for armour/weapons; when multiple variants match, report a min/max range:

```csharp
var matches = CollectedData.UniqueArmours.Lines
    .Where(x => x.Name == uniqueName || candidates.Contains(x.Name))
    .ToList();
if (matches.Count == 1) min = max = matches[0].ChaosValue ?? 0;
else if (matches.Count > 1) { min = matches.Min(x => x.ChaosValue) ?? 0;
                              max = matches.Max(x => x.ChaosValue) ?? 0; }
```

---

## 4. Iterate items to price: ground, inventory, stash

To price dropped items, iterate the on‑ground labels. In **this fork** the member is
[`IngameUi.ItemsOnGroundLabels`](../../../Core/PoEMemory/MemoryObjects/IngameUIElements.cs)
(an `IList<LabelOnGround>`); each [`LabelOnGround`](../../../Core/PoEMemory/Elements/LabelOnGround.cs)
exposes `ItemOnGround` (the label's `Entity`), `IsVisible`, `Label` and `CanPickUp`:

```csharp
foreach (var label in GameController.IngameState.IngameUi.ItemsOnGroundLabels)
{
    if (!label.IsVisible) continue;
    var item = label.ItemOnGround?.GetComponent<WorldItem>()?.ItemEntity;
    if (item == null) continue;
    var price = PriceItem(item);                 // §3
    var rect  = label.Label.GetClientRect();
    Graphics.DrawText(price.ToString("F1"), rect.TopRight, Color.White);
}
```

> Compatibility. Upstream ExileApi‑Compiled exposes filtered helpers like
> `ItemsOnGroundLabelsVisible` / `VisibleGroundItemLabels` (used by Get‑Chaos‑Value and
> RecipeTracker). **They do not exist in this fork** — filter `ItemsOnGroundLabels` by
> `IsVisible` yourself. See [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

For stash/inventory items, walk `VisibleInventoryItems` and read each `NormalInventoryItem.Item` (see [inventories.md](../inventories.md)), then price exactly as above. MapsExchange does this to highlight maps:

```csharp
var items = stash.VisibleStash.VisibleInventoryItems;
foreach (var invItem in items)
{
    var entity = invItem?.Item;
    var bit = GameController.Files.BaseItemTypes.Translate(entity?.Path);
    if (bit?.ClassName != "Map") continue;
    Graphics.DrawFrame(invItem.GetClientRect(), color, width);
}
```

---

## 5. Show value on hover and on ground labels

### The hovered item

[`IngameState.UIHover`](../../../Core/PoEMemory/MemoryObjects/IngameState.cs) read as a
[`HoverItemIcon`](../../../Core/PoEMemory/Elements/HoverItemIcon.cs) gives you the hovered item
across inventory/ground/chat. Both `UIHover` and `HoverItemIcon` (`Item`, `Tooltip`,
`ToolTipType`) are verified in this fork:

```csharp
var hover = GameController.Game.IngameState.UIHover.AsObject<HoverItemIcon>();
var hovered = hover?.Item;                    // the hovered item Entity, or null
var tooltipRect = hover?.Tooltip?.GetClientRect();
```

A common trick (from MapsExchange) is to skip drawing your own overlay where it would collide with the game's tooltip:

```csharp
var tooltip = hover?.Tooltip;
if (tooltip != null && tooltip.GetClientRect().Intersects(drawRect))
    continue;                                 // don't draw under the tooltip
```

### Drawing the price

Once you have a price and a `RectangleF` (a label rect or `GetClientRect()`), draw a backing box and text with [`Graphics`](../graphics.md):

```csharp
var label = $"{price:F0}c";
var size = Graphics.MeasureText(label);
Graphics.DrawBox(rect.TopRight, new Vector2(rect.Right + size.X, rect.Top + size.Y), bgColor);
Graphics.DrawText(label, rect.TopRight, Color.White);
```

Wrap the per‑frame scan that builds the priced list in a [`TimeCache`/`FrameCache`](../caching.md)
so you don't re‑resolve every label every frame — Ninja Price uses
`new TimeCache<List<...>>(GetItemsOnGroundSlow, 500)` for the expensive scan and a
`FrameCache` for the cheap one.

---

## 6. Expose / consume prices via the PluginBridge

A price plugin can publish a lookup so other plugins reuse it instead of re‑downloading.
[`GameController.PluginBridge`](../../../Core/GameController.cs) (`SaveMethod` / `GetMethod<T>`,
both verified in this fork) is the channel. Ninja Price registers callbacks; MarketWizard
consumes one:

```csharp
// Producer (Ninja Price)
GameController.PluginBridge.SaveMethod("NinjaPrice.GetValue",
    (Entity e) => new CustomItem(e, null).PriceData.MinChaosValue);

// Consumer (MarketWizard) — resolve once per Tick, then call freely
_getNinjaValue =
    GameController.PluginBridge.GetMethod<Func<BaseItemType, double>>("NinjaPrice.GetBaseItemTypeValue");
double? ratio = _getNinjaValue?.Invoke(wantedItem) / _getNinjaValue?.Invoke(offeredItem);
```

`GetMethod<T>` returns `null` when the producer isn't loaded — always null‑check before invoking.

---

## 7. Currency-exchange / bulk-trade UI automation

MarketWizard overlays the in‑game **Currency Exchange** panel: it reads both sides of the
order book, draws a depth graph and an order table with ImGui, and marks the poe.ninja fair
ratio (fetched via the PluginBridge above) on the graph.

```csharp
if (GameController.IngameState.IngameUi.CurrencyExchangePanel is { IsVisible: true } panel
    && panel is { WantedItemType: { } wanted, OfferedItemType: { } offered })
{
    var offeredStock = panel.OfferedItemStock      // listings on the offered side
        .Select(x => (x.Give, x.Get, Ratio: x.Get / (float)x.Give, x.ListedCount))
        .Where(x => x.Get != 0 && x.Give != 0 && x.ListedCount > 0).ToList();
    // … aggregate into cumulative depth, draw with ImGui.GetWindowDrawList() …
}
```

> Upstream‑only. `IngameUi.CurrencyExchangePanel` and its members
> (`WantedItemType`, `OfferedItemType`, `WantedItemStock`, `OfferedItemStock`) are part of the
> larger ExileApi‑Compiled distribution and **are not present in this fork's `Core/`** (the
> fork is a slimmer snapshot without the Village currency‑exchange subsystem). To port
> MarketWizard you would need to add that element/struct. See
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). The ImGui depth‑graph
> and order‑book rendering, the cumulative‑depth aggregation, and the `FormatNumber` ratio
> formatter are all engine‑independent and reusable as‑is.

### Driving the mouse for trade actions

MapsExchange ships a raw Win32 mouse helper (`SetCursorPos` + `mouse_event` via P/Invoke) to
click stash/exchange UI. This bypasses ExileCore entirely. Prefer the engine's
[`Input`](../input.md) helpers where they cover your need; only drop to P/Invoke for click
sequences the engine doesn't expose, and gate any automation behind explicit user opt‑in.

```csharp
[DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] static extern void mouse_event(int flags, int dx, int dy, int b, int extra);
// flags: LeftDown = 0x02, LeftUp = 0x04, RightDown = 0x08, RightUp = 0x10
```

---

## Out of scope

`exApiTools/KalandraOptimizer` is a Lake‑of‑Kalandra tablet **pathfinding/route optimizer**
(A*/binary‑heap over tile state); it carries no pricing or trade logic and is not covered here.
`RecipeTracker` (Archnemesis recipe tracking) relies on league‑specific, upstream‑only members
(`ArchnemesisInventoryPanel`, `Files.ArchnemesisRecipes`, `ItemsOnGroundLabelsVisible`) — see
the compatibility doc before porting.

## See also

- [files-in-memory.md](../files-in-memory.md) — `Files.BaseItemTypes.Translate`, `BaseItemType`.
- [components-items.md](../components-items.md) — `Mods`, `Base`, `Stack`, `Sockets`, `RenderItem`.
- [caching.md](../caching.md) — `FrameCache`/`TimeCache` for the per‑frame price scan.
- [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) — upstream‑only members (`CurrencyExchangePanel`, `ItemsOnGroundLabelsVisible`, `Mods.ImplicitMods/ExplicitMods/EnchantedStats`).

## Source repos

- [exApiTools/Get-Chaos-Value](https://github.com/exApiTools/Get-Chaos-Value) — "Ninja Price": HTTP fetch + on‑disk cache, item→price mapping, hover/ground value display, PluginBridge producer.
- [exApiTools/MarketWizard](https://github.com/exApiTools/MarketWizard) — Currency Exchange order‑book depth graph; PluginBridge consumer of Ninja Price.
- [exApiTools/MapsExchange](https://github.com/exApiTools/MapsExchange) — stash/inventory map highlighting, hover‑tooltip avoidance, Win32 mouse automation.
- [exApiTools/tft-data-prices](https://github.com/exApiTools/tft-data-prices) — TFT bulk‑price JSON data feed (no code).
- [exApiTools/RecipeTracker](https://github.com/exApiTools/RecipeTracker) — Archnemesis recipe tracker (upstream‑only members; referenced for ground‑label iteration).
- [exApiTools/KalandraOptimizer](https://github.com/exApiTools/KalandraOptimizer) — Lake‑of‑Kalandra tablet pathfinder (no pricing; out of scope).
