# Recipe: item filtering with ItemFilterLibrary

How the popular `*WithLinq` reference plugins let users write text item-filter rules and match them against in-memory items — and how that pattern maps onto this fork's engine API.

[API reference index](../README.md) · [cookbook index](README.md)

> **ItemFilterLibrary (IFL) is an external dependency, not part of this engine.** It is a
> separate library by DetectiveSquirrel (source mirrored at
> [`exApiTools/ItemFilter`](https://github.com/exApiTools/ItemFilter)) shipped as
> `ItemFilterLibrary.dll`. Nothing under `Core/` of this repo references it. This recipe
> documents how a *plugin* consumes IFL and which engine members IFL itself reads, so you
> can judge whether IFL (or your own port of the pattern) will run on this fork. Several
> members IFL relies on are absent here — see [Compatibility caveats](#compatibility-caveats).

## What the pattern gives you

A user-editable filter is just a text file of C# boolean expressions evaluated against an
item. Plugins like **InvWithLinq**, **NPCInvWithLinq**, **Ground-Items-With-Linq** and
**HighlightedItems** all use the same three-piece flow:

1. **`ItemData`** — a flat, query-friendly snapshot of one item, built from an `Entity`.
2. **`ItemFilter`** — a compiled set of rules loaded from a `.ifl` file / string.
3. **`filter.Matches(itemData)`** — evaluates the rules against an item.

The rule language is [Dynamic LINQ](https://dynamic-linq.net/) (the
`System.Linq.Dynamic.Core` NuGet package): each non-blank line/section becomes a
`Func<ItemData, bool>` compiled once at load time. So filter syntax is "whatever member of
`ItemData` you can name, compared with C# operators".

## Referencing IFL from a plugin

IFL is referenced as a sibling DLL, alongside `ExileCore.dll`, plus the Dynamic LINQ
package the compiled queries need at runtime
(`NPCInvWithLinq/NPCInvWithLinq.csproj`):

```xml
<Reference Include="ExileCore">
  <HintPath>$(exapiPackage)\ExileCore.dll</HintPath>
  <Private>False</Private>
</Reference>
<Reference Include="ItemFilterLibrary">
  <HintPath>$(exapiPackage)\ItemFilterLibrary.dll</HintPath>
  <Private>False</Private>
</Reference>
<!-- ... -->
<PackageReference Include="System.Linq.Dynamic.Core" Version="1.3.5" />
```

```csharp
using ItemFilterLibrary;   // ItemData, ItemFilter, ItemQuery
```

> On this fork `$(exapiPackage)` is whatever folder holds the built `ExileCore.dll`. You
> would need a build of `ItemFilterLibrary.dll` compiled against *this* engine; the
> upstream binary targets `exApiTools/ExileApi-Compiled` and references members this fork
> does not expose (below).

## The filter rule language (`.ifl`)

A filter file is plain text. Rules are separated by blank lines; each rule may span several
lines (they are concatenated). `//` starts a comment. A rule matches when its expression is
`true`; the whole filter matches if **any** rule matches
(`ItemFilter<T>.Matches` returns on the first hit). A rule prefixed with `^` is *negative*:
a match makes the filter return `false` (an exclusion). Example, distilled from
`NPCInvWithLinq/Example Filter/Standard yoinks.ifl` and
`exApiTools/ItemFilterExamples`:

```text
// Owner: you — example.ifl

Rarity == ItemRarity.Unique                       // all uniques

BaseName.Contains("Invitation")                   // any Invitation base

ClassName == "Map" && MapInfo.Tier >= 14          // T14+ maps

//----------------------------------------------
// 6 sockets, 6-link
//----------------------------------------------
SocketInfo.SocketNumber == 6
SocketInfo.LargestLinkSize >= 6

ModsInfo.ItemMods.Any(Name == "MapAreaContainsEssences")   // extra-essence map implicit

!IsIdentified && BaseName.Contains("Leather Belt")         // unid leather belt
```

`Rarity`, `ItemRarity`, `GameStat` etc. resolve because IFL configures Dynamic LINQ with a
custom type provider and `ResolveTypesBySimpleName = true`, so engine enums are usable by
their short name inside a rule.

## Loading and reloading rules

IFL exposes static loaders (`exApiTools/ItemFilter/ItemFilter.cs`):

```csharp
ItemFilter ItemFilter.LoadFromPath(string filterFilePath);    // read a .ifl file
ItemFilter ItemFilter.LoadFromString(string text);            // parse an in-memory string
ItemFilter ItemFilter.LoadFromList(string name, IEnumerable<string> lines);
```

Reference plugins scan a config folder for `*.ifl`, compile each into an `ItemFilter`, and
keep them in a list. They reload on demand (a "Reload Rules" button) and wrap the file read
in a retry loop because the user may be saving the file in an editor
(`InvWithLinq/RulesDisplay.cs`):

```csharp
public static void LoadAndApplyRules()
{
    var dir = GetConfigFileDirectory();                       // Main.ConfigDirectory by default
    var files = new DirectoryInfo(dir)
        .GetFiles("*.ifl", SearchOption.AllDirectories);

    Main._itemFilters = files
        .Where(/* rule enabled */)
        .Select(f => (LoadItemFilterWithRetry(f.FullName), /* rule meta */))
        .ToList();
}

private static ItemFilter LoadItemFilterWithRetry(string path)
{
    for (var attempt = 0; ; attempt++)
        try { return ItemFilter.LoadFromPath(path); }
        catch (IOException) when (attempt < 10) { Thread.Sleep(100); }
}
```

`ConfigDirectory` is provided by the engine on every plugin — see
[plugins.md](../plugins.md). A compile error in a rule does **not** throw at load; IFL logs
it via `DebugWindow.LogError` and stores the message on the query
(`ItemQuery.FailedToCompile`), so a bad line just never matches.

## Building `ItemData` from an item

`ItemData` is the bridge from engine memory to the query model. Its constructors take an
`Entity` (and optionally the ground-label entity), then read components into plain
properties (`exApiTools/ItemFilter/ItemData.cs`):

```csharp
public ItemData(Entity queriedItem, GameController gc);                 // inventory / equipped item
public ItemData(Entity queriedItem, Entity groundItem, GameController gc);
public ItemData(LabelOnGround queriedItem, GameController gc);          // ground item via its label
```

The core constructor body (abridged) shows exactly which engine API IFL builds on:

```csharp
var baseItemType = gc.Files.BaseItemTypes.Translate(item.Path);   // FilesInMemory lookup
if (baseItemType != null) { ClassName = baseItemType.ClassName; BaseName = baseItemType.BaseName; /* Width/Height/Tags */ }

if (item.TryGetComponent<Base>(out var b))     { Name = b.Name; IsCorrupted = b.isCorrupted; IsElder = b.isElder; /* ... */ }
if (item.TryGetComponent<Mods>(out var m))     { Rarity = m.ItemRarity; IsIdentified = m.Identified; ItemLevel = m.ItemLevel; ModsInfo = new ModsData(m.ItemMods, /* ... */); }
if (item.TryGetComponent<Sockets>(out var s))  { SocketInfo = new SocketData(s.LargestLinkSize, s.NumberOfSockets, s.Links, s.SocketGroup, /* gems */); }
if (item.TryGetComponent<Stack>(out var st))   { StackInfo = new StackData(st.Size, st.Info.MaxStackSize); }
if (item.TryGetComponent<Quality>(out var q))  { ItemQuality = q.ItemQuality; }
```

So the query model is just a re-projection of the same components documented in
[components-items.md](../components-items.md), plus the static `BaseItemTypes` table from
[files-in-memory.md](../files-in-memory.md).

Plugins usually subclass `ItemData` to attach UI data (screen rect, label color). The
inventory variant is the simplest (`InvWithLinq/CustomItemData.cs`):

```csharp
public class CustomItemData : ItemData
{
    public CustomItemData(Entity queriedItem, GameController gc, RectangleF rect)
        : base(queriedItem, gc) => ClientRectangleCache = rect;

    public RectangleF ClientRectangleCache { get; set; }
}
```

## Where the items come from

You feed `ItemData` whatever `Entity` you can reach. Two common sources:

**Player inventory** — iterate `NormalInventoryItem`s and use `.Item` (the item `Entity`);
see [inventories.md](../inventories.md) (`InvWithLinq/InvWithLinq.cs`):

```csharp
var inventory = GameController.Game.IngameState.Data.ServerData
    .PlayerInventories[0].Inventory;
foreach (var slot in inventory.InventorySlotItems)
{
    if (slot.Item == null || slot.Address == 0) continue;
    items.Add(new CustomItemData(slot.Item, GameController, slot.GetClientRect()));
}
```

**NPC / shop / stash items** — same idea, from a visible UI inventory
(`NPCInvWithLinq`, `ItemFilterLibInspector`): take `x.Item` off each
`NormalInventoryItem` (or server-side `ServerInventory.Items`) and wrap it.

## Evaluating

With a list of compiled filters and a list of `ItemData`, matching is a nested loop. Stop on
the first filter that matches and use its associated metadata (color, etc.)
(`InvWithLinq/InvWithLinq.cs`):

```csharp
foreach (var item in inventoryItems)
    foreach (var (filter, rule) in _itemFilters)
        if (filter.Matches(item)) { Highlight(item, rule.Color); break; }
```

Ground-Items caches the boolean per item and only recomputes when the engine signals the
item changed (`Ground-Items-With-Linq/Main.cs`):

```csharp
item.IsWanted ??= ItemFilters?.Any(f => f.Matches(item)) ?? false;
```

A quick "does this hovered item match?" tester uses `LoadFromString` on a single expression
(`InvWithLinq.PerformItemFilterTest`):

```csharp
var filter  = ItemFilter.LoadFromString(Settings.FilterTest);
var matched = filter.Matches(new ItemData(hoveredItem.Entity, GameController));
```

`ItemFilter.Matches` guards `item?.Entity?.IsValid` internally and never throws on a bad
rule (it logs and returns `false`), so it is safe to call every frame.

## Compatibility caveats

IFL's `ItemData` constructor leans on engine members that this fork **does not expose**.
Building the upstream `ItemFilterLibrary.dll` against this fork will fail; a port must
replace these. See [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md)
for the full table — the IFL-relevant gaps are:

| IFL uses | This fork | Port to |
| --- | --- | --- |
| `Entity.TryGetComponent<T>(out T)` | **missing** | `var c = e.GetComponent<T>(); if (c != null) { … }` (and `HasComponent<T>()`) — see [entities.md](../entities.md). |
| `Mods.EnchantedMods`, `Mods.ExplicitMods`, `Mods.FracturedMods`, `Mods.ImplicitMods`, `Mods.ScourgeMods`, `Mods.SynthesisMods`, `Mods.CrucibleMods` | **missing** | only `Mods.ItemMods` (all mods, implicit+explicit) exists here — see [components-items.md](../components-items.md#mods). |
| `Base.PublicPrice`, `Base.InfluenceFlag`, `Base.ScourgedTier`, `Base.isHunter / isWarlord / isCrusader / isRedeemer`, `Base.CurrencyItemLevel` | **missing** | fork `Base` exposes only `Name`, `ItemCellsSizeX/Y`, `isCorrupted`, `isShaper`, `isElder` — see [components-items.md](../components-items.md#base). |

Members IFL uses that **do** exist here, verified against `Core/`:
`GameController.Files.BaseItemTypes.Translate(string)`
(`Core/PoEMemory/FilesInMemory/BaseItemTypes.cs`); `BaseItemType.ClassName/BaseName/Width/Height/Tags`;
`Mods.ItemRarity/Identified/ItemLevel/RequiredLevel/UniqueName/CountFractured/Synthesised/IsMirrored/ItemMods`;
`Sockets.NumberOfSockets/LargestLinkSize/Links/SocketGroup/SocketedGems`;
`Stack.Size` + `Stack.Info.MaxStackSize` (`CurrencyInfo.MaxStackSize`);
`Quality.ItemQuality`; `ItemMod.Name/DisplayName`; and `NormalInventoryItem.Item`.

Net effect: filter rules touching only those verified members port cleanly; rules touching
influence flags, per-category mod lists, prices or scourge data do not, until the underlying
components are extended on this fork.

## Source repos

- [exApiTools/ItemFilter](https://github.com/exApiTools/ItemFilter) — the IFL library itself (`ItemData.cs`, `ItemFilter.cs`, `ItemData.Nested.cs`, `CustomDynamicLinqCustomTypeProvider.cs`); mirrors DetectiveSquirrel/ItemFilterLibrary. *(DetectiveSquirrel/ItemFilterLibrary direct clone 404'd in this environment; source read from this mirror.)*
- [exApiTools/ItemFilterExamples](https://github.com/exApiTools/ItemFilterExamples) — example `.ifl` rule files (`Base/Rarity/Uniques.ifl`, etc.).
- [mikkelpetersen/InvWithLinq](https://github.com/mikkelpetersen/InvWithLinq) — inventory highlighter; clean `CustomItemData` + load/reload/match wiring.
- [exApiTools/NPCInvWithLinq](https://github.com/exApiTools/NPCInvWithLinq) — shop/NPC inventory variant; bundled `Example Filter/*.ifl`.
- [exApiTools/Ground-Items-With-Linq](https://github.com/exApiTools/Ground-Items-With-Linq) — ground-label items; cached per-item `Matches`.
- [exApiTools/HighlightedItems](https://github.com/exApiTools/HighlightedItems) — stash highlighter using the same IFL flow.
- [DetectiveSquirrel/ItemFilterLibInspector](https://github.com/DetectiveSquirrel/ItemFilterLibInspector) — debug tool that builds `ItemData` from every reachable inventory and inspects it.
