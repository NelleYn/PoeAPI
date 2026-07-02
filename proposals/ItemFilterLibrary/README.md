# ItemFilterLibrary (ported candidate)

> **EXPERIMENTAL — not compiled in this environment.**
> This is Windows + live-game only, and it needs the `System.Linq.Dynamic.Core` NuGet package plus a
> build against this fork's `ExileCore.dll`. It cannot be built or run here (no `ExileCore.dll`, no
> NuGet restore, no game process). Every file starts with an `// EXPERIMENTAL candidate …` banner and
> lives under `proposals/`, so it is **outside every `.csproj`** in the solution and cannot break
> `Core/`, `GameOffsets/` or `Loader/`. Nothing under `Core/` was modified.

## What ItemFilterLibrary is

[ItemFilterLibrary](https://github.com/exApiTools/ItemFilter) (IFL, by DetectiveSquirrel; the
`exApiTools/ItemFilter` mirror) is the small library every `*WithLinq` reference plugin
(**InvWithLinq**, **NPCInvWithLinq**, **Ground-Items-With-Linq**, **HighlightedItems**) builds on to
let users write text item-filter rules and match them against in-memory items. A filter file is plain
text: each rule is a C# boolean expression evaluated against one item using
[Dynamic LINQ](https://dynamic-linq.net/). It is three pieces:

1. **`ItemData`** — a flat, query-friendly snapshot of one item, built from an `Entity`.
2. **`ItemFilter`** — a compiled set of rules loaded from a `.ifl` file / string / line list.
3. **`filter.Matches(itemData)`** — evaluates the rules against an item.

The full pattern, the rule language, and the exact member mapping onto this fork are documented in the
cookbook recipe [`docs/api/cookbook/item-filtering.md`](../../docs/api/cookbook/item-filtering.md);
this port implements that recipe. IFL itself is **not** part of this engine — nothing under `Core/`
references it — so this is an additive, standalone re-implementation of the pattern against this fork's
Core API.

## How this port maps onto our fork

The upstream `ItemFilterLibrary.dll` targets `exApiTools/ExileApi-Compiled` and reads several members
this fork does not expose (see [Members deliberately NOT used](#members-deliberately-not-used-absent-from-this-fork)
and the recipe's [Compatibility caveats](../../docs/api/cookbook/item-filtering.md#compatibility-caveats)).
So the upstream binary will **not** build against this fork; this port replaces the missing members:

- **No `Entity.TryGetComponent<T>(out T)`** on this fork → the port uses
  `var c = e.GetComponent<T>(); if (c != null) { … }` throughout (see
  [entities.md](../../docs/api/entities.md)).
- **`Mods` exposes only the combined `ItemMods`** (implicit + explicit) — there are no per-category
  lists here — so `ModsData` exposes only `ItemMods` (see
  [components-items.md](../../docs/api/components-items.md)).
- **`Base` exposes only `Name`/`isCorrupted`/`isShaper`/`isElder`** (plus cell sizes) — no influence /
  price / scourge members — so those flat properties are dropped from `ItemData`.

Everything else in the recipe's "members that **do** exist here" list ports cleanly (verified below).

## Files in this port

| File | Role | Notes vs upstream |
|------|------|-------------------|
| `ItemData.cs` | Flat, query-friendly snapshot of one item, built from an `Entity`. Ctors take an `Entity`, a `LabelOnGround`, or a `NormalInventoryItem` (+ `GameController`). | `TryGetComponent` → `GetComponent`+null-check. Influence/price/scourge flat props dropped (absent). Subclassable for the `CustomItemData` pattern (it is `partial`, not `sealed`). |
| `ItemData.Nested.cs` | Nested `ModsData` / `SocketData` / `StackData` projections referenced by `ItemData`. | `ModsData` has only the combined `ItemMods` (no per-category lists). Upstream `MapData`/`ChargeData`/`AttributeRequirementsData`/`SkillGemData` omitted (backing members out of this port's verified scope). |
| `ItemFilter.cs` | Compiled rule set: `LoadFromPath` / `LoadFromString` / `LoadFromList` + `Matches(ItemData)`. Parses blank-line-separated blocks, concatenates multi-line rules, strips `//` comments (respecting string literals), and honours the `^` exclusion prefix. | Faithful to the recipe's documented rule language; `Matches` guards `Entity?.IsValid` and swallows per-rule runtime exceptions. |
| `ItemQuery.cs` | One compiled rule: a Dynamic LINQ `Func<ItemData,bool>`, with `FailedToCompile` behaviour. | A bad rule logs via `DebugWindow.LogError`, stores the message on `FailedToCompile`, and compiles to constant `false` — it never matches, never throws. |
| `CustomDynamicLinqCustomTypeProvider.cs` | Dynamic LINQ type provider that registers this fork's engine enums so rules can name them by short name (with `ResolveTypesBySimpleName = true`). | Registers every public enum in the ExileCore assembly (`ItemRarity`, `GameStat`, …) on top of the Dynamic LINQ defaults. Base ctor is package-version-sensitive (targets 1.3.5). |
| `README.md` | This file. | — |

### The rule language (recap)

Plain text; blank lines separate rules; consecutive lines are concatenated into one expression; `//`
starts a comment (whole-line comments become the rule's name; inline comments are stripped). A rule
matches when its expression is `true`. `Matches` stops on the **first** rule that is `true`: a normal
rule makes the filter match, a `^`-prefixed rule **excludes** the item (filter returns `false`).

```text
Rarity == ItemRarity.Unique                              // all uniques

BaseName.Contains("Invitation")                          // any Invitation base

SocketInfo.SocketNumber == 6 && SocketInfo.LargestLinkSize >= 6   // 6s / 6L

ModsInfo.ItemMods.Any(Name == "MapAreaContainsEssences") // extra-essence map implicit

!IsIdentified && BaseName.Contains("Leather Belt")       // unid leather belt

^ Rarity == ItemRarity.Normal                            // …but never plain-normal items
```

## Exact fork members this port builds on (verified in `master`)

All line numbers are from `master` (read via `git show master:<path>`; this branch's working tree
normalizes line endings, so cite `master`).

Game model / files
- `Core/GameController.cs:125` — `GameController.Files` (`FilesContainer`).
- `Core/PoEMemory/FilesContainer.cs:65` — `FilesContainer.BaseItemTypes`.
- `Core/PoEMemory/FilesInMemory/BaseItemTypes.cs:27` — `BaseItemTypes.Translate(string)` → `BaseItemType`.
- `Core/PoEMemory/Models/BaseItemType.cs` — `ClassName:8`, `Width:9`, `Height:10`, `BaseName:12`, `Tags:13`.

Entity & components
- `Core/PoEMemory/MemoryObjects/Entity.cs` — `IsValid:97`, `Path:282`, `HasComponent<T>():580`, `GetComponent<T>():605`. (No `TryGetComponent` — grep confirms 0 matches.)
- `Core/PoEMemory/Components/Base.cs` — `Name:10`, `isCorrupted:13`, `isShaper:14`, `isElder:15`.
- `Core/PoEMemory/Components/Mods.cs` — `UniqueName:25`, `Identified:33`, `ItemRarity:34`, `ItemMods:39`, `ItemLevel:49`, `RequiredLevel:50`, `IsMirrored:52`, `CountFractured:53`, `Synthesised:54`.
- `Core/PoEMemory/Components/Sockets.cs` — `LargestLinkSize:10`, `Links:31`, `NumberOfSockets:81`, `SocketGroup:85`, `SocketedGems:127`; nested `SocketedGem` (`class:149`, `GemEntity:151`).
- `Core/PoEMemory/Components/Stack.cs` — `Size:5`, `Info:6` (`CurrencyInfo`).
- `Core/PoEMemory/Components/CurrencyInfo.cs:5` — `MaxStackSize`.
- `Core/PoEMemory/Components/Quality.cs:5` — `ItemQuality`.
- `Core/PoEMemory/MemoryObjects/ItemMod.cs` — `Name:37`, `DisplayName:48`.

Enums, inventory, logging
- `Core/Shared/Enums/ItemRarity.cs:3` — `ItemRarity` (Normal/Magic/Rare/Unique/Gem/Currency/Quest/Prophecy).
- `Core/Shared/Enums/GameStat.cs:3` — `GameStat` (registered by the type provider for short-name use).
- `Core/PoEMemory/Elements/InventoryElements/NormalInventoryItem.cs:29` — `NormalInventoryItem.Item`.
- `Core/PoEMemory/Elements/LabelOnGround.cs:26` — `LabelOnGround.ItemOnGround`.
- `Core/DebugWindow.cs:130` — `DebugWindow.LogError(string, float)`.

## Members deliberately NOT used (absent from this fork)

Per the recipe's [Compatibility caveats](../../docs/api/cookbook/item-filtering.md#compatibility-caveats)
and confirmed against `master`:

- **`Entity.TryGetComponent<T>(out T)`** — does not exist. Replaced everywhere with
  `var c = e.GetComponent<T>(); if (c != null) { … }` (see `GetComponent<T>` — `Entity.cs:605`).
- **`Mods.ExplicitMods` / `Mods.ImplicitMods` / `Mods.EnchantedMods` / `Mods.FracturedMods` /
  `Mods.ScourgeMods` / `Mods.SynthesisMods` / `Mods.CrucibleMods`** — none exist; the fork's `Mods`
  exposes only the combined `ItemMods` (implicit + explicit, `Mods.cs:39`). `ModsData` therefore
  exposes only `ItemMods`; rules that need a specific affix category cannot be ported until the
  component is extended.
- **`Base.PublicPrice` / `Base.InfluenceFlag` / `Base.ScourgedTier` / `Base.isHunter` /
  `Base.isWarlord` / `Base.isCrusader` / `Base.isRedeemer` / `Base.CurrencyItemLevel`** — absent; the
  fork's `Base` has only `Name`, `ItemCellsSizeX/Y`, `isCorrupted`, `isShaper`, `isElder`
  (`Base.cs:10-15`). The influence / price / scourge flat properties are dropped from `ItemData`.
- **Upstream nested projections `MapData` (`MapInfo.Tier`), `ChargeData`, `AttributeRequirementsData`,
  `SkillGemData`, weapon/armour stat projections** — omitted from this port. Their backing components
  are outside this unit's verified scope (the recipe scopes `ItemData` to `Base`/`Mods`/`Sockets`/
  `Stack`/`Quality` + `BaseItemTypes`), so registering them would risk fabricating offsets/members.
  Add them the same way once the corresponding component members are verified on this fork.

## How to integrate

This port targets the file-scoped namespace **`ExileCore.ItemFilterLibrary`** (safest, and consistent
with `proposals/IconsBuilder` → `ExileCore.IconsBuilder`).

1. **Add the rule-engine dependency.** In the consuming project (or a dedicated `ItemFilterLibrary`
   class-library project), add:
   ```xml
   <PackageReference Include="System.Linq.Dynamic.Core" Version="1.3.5" />
   ```
   and reference this fork's `ExileCore.dll` (`<Reference Include="ExileCore">`).
2. **Drop in the files.** Copy the five `.cs` files (not this README) into your project (e.g. a
   `ItemFilterLibrary/` folder) and delete the leading `// EXPERIMENTAL candidate …` banner once
   promoted.
3. **Namespace choice.** Existing upstream plugins do `using ItemFilterLibrary;`. To keep them
   drop-in, either (a) change the consuming plugin to `using ExileCore.ItemFilterLibrary;`, or
   (b) rename the namespace in these five files from `ExileCore.ItemFilterLibrary` to
   `ItemFilterLibrary`. Both compile; (a) is the lower-risk change.
4. **Build** (Windows + game required to actually run). These files compile against this fork's public
   members only, plus `System.Linq.Dynamic.Core`.

Usage sketch (mirrors the recipe):

```csharp
using ExileCore.ItemFilterLibrary;

var filters = new DirectoryInfo(ConfigDirectory)
    .GetFiles("*.ifl", SearchOption.AllDirectories)
    .Select(f => ItemFilter.LoadFromPath(f.FullName))
    .ToList();

foreach (var slot in inventory.InventorySlotItems)
{
    if (slot.Item == null || slot.Address == 0) continue;
    var data = new ItemData(slot.Item, GameController);
    if (filters.Any(f => f.Matches(data))) Highlight(slot);
}
```

### Dependencies

- **`ExileCore.dll`** (this fork): the `ExileCore.PoEMemory.*` entity/component model
  (`Entity`, `Base`, `Mods`, `Sockets`, `Stack`, `Quality`, `CurrencyInfo`, `ItemMod`,
  `NormalInventoryItem`, `LabelOnGround`), `GameController`/`FilesContainer`/`BaseItemTypes`,
  `ExileCore.Shared.Enums.*`, and `DebugWindow`.
- **`System.Linq.Dynamic.Core`** (1.3.5): `ParsingConfig`, `DynamicExpressionParser.ParseLambda`, and
  `DefaultDynamicLinqCustomTypeProvider`. The base class / constructor of the default provider is
  package-version-sensitive — if you pin a different version, `CustomDynamicLinqCustomTypeProvider`'s
  `: base(config, cacheCustomTypes)` call may need adjusting.

## Provenance

- **Primary source:** the cookbook recipe
  [`docs/api/cookbook/item-filtering.md`](../../docs/api/cookbook/item-filtering.md), which documents
  the full IFL pattern, the exact `ItemData` constructor body, the rule language, the loaders, and the
  compatibility caveats mapped onto this fork. This port implements that recipe.
- **Fork grounding:** every fork member cited above was read directly from `master` in this repo
  (`git show master:Core/…`) and is listed with `file:line`.
- **Upstream repos (could not be fetched in this environment):**
  [`exApiTools/ItemFilter`](https://github.com/exApiTools/ItemFilter) (the IFL library:
  `ItemData.cs`, `ItemData.Nested.cs`, `ItemFilter.cs`, `CustomDynamicLinqCustomTypeProvider.cs`) and
  [`mikkelpetersen/InvWithLinq`](https://github.com/mikkelpetersen/InvWithLinq) (the `CustomItemData`
  + load/reload/match wiring). A `git clone` of both returned **HTTP 403** from this environment's
  egress proxy, and the GitHub MCP is scoped to `nelleyn/poeapi` only — so the upstream C# could not
  be read here. The port was reconstructed from the cookbook recipe (which itself was written from
  those repos) plus the verified fork members. The recipe additionally notes that a direct
  `DetectiveSquirrel/ItemFilterLibrary` clone **404'd**, and its source was read from the
  `exApiTools/ItemFilter` mirror.
