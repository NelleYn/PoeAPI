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

**Update:** this fork's `Core` does have standalone `Map` / `Charges` / `AttributeRequirements` /
`SkillGem` / `Weapon` / `Armour` components
(`Core/PoEMemory/Components/{Map,Charges,AttributeRequirements,SkillGem,Weapon,Armour}.cs`,
present since long before this port — the earlier revision of this README simply hadn't grepped for
them). `ItemData` now exposes `MapInfo` / `ChargeInfo` / `AttributeRequirementsInfo` / `SkillGemInfo` /
`WeaponInfo` / `ArmourInfo` projections backed by those components (`null` when the entity doesn't
carry the component — see
[Members now wired up](#members-now-wired-up-post-verification) below). Per-category mod lists and
influence/price/scourge data remain unbacked (no new members appeared for those; see
[Members deliberately NOT used](#members-deliberately-not-used-absent-from-this-fork)).

## Files in this port

| File | Role | Notes vs upstream |
|------|------|-------------------|
| `ItemData.cs` | Flat, query-friendly snapshot of one item, built from an `Entity`. Ctors take an `Entity`, a `LabelOnGround`, or a `NormalInventoryItem` (+ `GameController`). | `TryGetComponent` → `GetComponent`+null-check. Influence/price/scourge flat props dropped (absent). Subclassable for the `CustomItemData` pattern (it is `partial`, not `sealed`). |
| `ItemData.Nested.cs` | Nested `ModsData` / `SocketData` / `StackData` / `MapData` / `ChargeData` / `AttributeRequirementsData` / `SkillGemData` / `WeaponData` / `ArmourData` projections referenced by `ItemData`. | `ModsData` has only the combined `ItemMods` (no per-category lists). `MapData`/`ChargeData`/`AttributeRequirementsData`/`SkillGemData`/`WeaponData`/`ArmourData` are backed by this fork's `Map`/`Charges`/`AttributeRequirements`/`SkillGem`/`Weapon`/`Armour` components and populated only when the entity has the matching component (`null` otherwise). |
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

Map / charge / attribute / skill-gem / weapon / armour components (wired up in this revision — see below)
- `Core/PoEMemory/Components/Map.cs` — `Tier:33`, `Area:30` (a `WorldArea`), `MapSeries:36`.
- `Core/PoEMemory/MemoryObjects/WorldArea.cs` — `Id:11`, `Name:13`, `AreaLevel:17`.
- `Core/PoEMemory/Components/Charges.cs` — `NumCharges:9`, `ChargesPerUse:12`, `ChargesMax:15`.
- `Core/PoEMemory/Components/AttributeRequirements.cs` — `strength:9`, `dexterity:12`, `intelligence:15`.
- `Core/PoEMemory/Components/SkillGem.cs` — `Level:22`, `MaxLevel:37`, `TotalExpGained:25`,
  `ExperienceMaxLevel:31`, `ExperienceToNextLevel:34`, `SocketColor:40`.
- `Core/PoEMemory/Components/Weapon.cs` — `DamageMin:9`, `DamageMax:12`, `AttackTime:15`, `CritChance:18`.
- `Core/PoEMemory/Components/Armour.cs` — `EvasionScore:9`, `ArmourScore:12`, `EnergyShieldScore:15`.
- `Core/Shared/Enums/InventoryTabMapSeries.cs` — `InventoryTabMapSeries` enum (registered by the type
  provider for short-name use, same as any other public engine enum).

## Members now wired up (post-verification)

The original revision of this port omitted `MapData` / `ChargeData` / `AttributeRequirementsData` /
`SkillGemData` / weapon / armour projections under the assumption that no backing components existed
on this fork. That assumption was never actually re-checked against `Core/PoEMemory/Components/` — a
grep shows `Map`, `Charges`, `AttributeRequirements`, `SkillGem`, `Weapon` and `Armour` components have
existed there since long before this port was written, each reading real, already-verified offsets (no
new/guessed memory offsets were added to implement this — see [Scope note](#scope-note)). This revision
wires them up:

- **`MapInfo` (`MapData`)** — `Tier`, `AreaId`/`AreaName`/`AreaLevel` (from `Map.Area`, a `WorldArea`),
  `MapSeries`. `null` unless the entity has a `Map` component (e.g. non-map items).
- **`ChargeInfo` (`ChargeData`)** — `NumCharges`, `ChargesPerUse`, `ChargesMax`. `null` unless the
  entity has a `Charges` component (e.g. flasks).
- **`AttributeRequirementsInfo` (`AttributeRequirementsData`)** — `Strength`/`Dexterity`/`Intelligence`
  (PascalCase wrappers around the fork's lowercase `strength`/`dexterity`/`intelligence` members).
  `null` unless the entity has an `AttributeRequirements` component.
- **`SkillGemInfo` (`SkillGemData`)** — `Level`, `MaxLevel`, `TotalExpGained`, `ExperienceMaxLevel`,
  `ExperienceToNextLevel`, `SocketColor`. `null` unless the entity has a `SkillGem` component.
- **`WeaponInfo` (`WeaponData`)** — `DamageMin`, `DamageMax`, `AttackTime`, `CritChance` (from the
  `Weapon` component, `Core/PoEMemory/Components/Weapon.cs`). `null` unless the entity has a `Weapon`
  component.
- **`ArmourInfo` (`ArmourData`)** — `ArmourScore`, `EvasionScore`, `EnergyShieldScore` (from the
  `Armour` component, `Core/PoEMemory/Components/Armour.cs`). `null` unless the entity has an `Armour`
  component.

All six are `null`-when-absent (unlike `ModsInfo`/`SocketInfo`/`StackInfo`, which default to
zero-valued instances) because, unlike `Mods`/`Sockets`/`Stack`/`Quality`, these components are only
present on a subset of item types — rules should guard with `MapInfo != null && …` etc. (Dynamic LINQ's
`&&` short-circuits, so this is safe.)

### Scope note

Extending `ItemData` to expose these was in scope for this pass (reading existing public component
members from the proposal, matching the pattern already used for `Base`/`Mods`/`Sockets`/`Stack`/
`Quality`). Adding brand-new *public members to `Mods`* to expose the per-category mod lists
(`ExplicitMods`/`ImplicitMods`/…) would additionally require editing `Core/PoEMemory/Components/Mods.cs`
itself — out of scope for this proposal-only pass — so those, and the influence/price/scourge flat
properties (no matching members exist anywhere in `Core`, verified by grep), remain omitted below.

## Members deliberately NOT used (absent from this fork)

Per the recipe's [Compatibility caveats](../../docs/api/cookbook/item-filtering.md#compatibility-caveats)
and confirmed against `master`. Unlike `MapData`/`ChargeData`/`AttributeRequirementsData`/
`SkillGemData`/weapon/armour (see [Members now wired up](#members-now-wired-up-post-verification)
above, previously listed here but now implemented), **no** backing member exists anywhere in `Core`
for the following — confirmed by grep, not merely by the original port-time assumption:

- **`Entity.TryGetComponent<T>(out T)`** — does not exist. Replaced everywhere with
  `var c = e.GetComponent<T>(); if (c != null) { … }` (see `GetComponent<T>` — `Entity.cs:605`).
- **`Mods.ExplicitMods` / `Mods.ImplicitMods` / `Mods.EnchantedMods` / `Mods.FracturedMods` /
  `Mods.ScourgeMods` / `Mods.SynthesisMods` / `Mods.CrucibleMods`** — none exist; the fork's `Mods`
  exposes only the combined `ItemMods` (implicit + explicit, `Mods.cs:39`). Internally `Mods` already
  reads separate implicit/explicit ranges (`ModsStruct.implicitMods` / `ModsStruct.explicitMods`, both
  `NativePtrArray`) and concatenates them before returning `ItemMods` via the private
  `Mods.GetMods(long, long)` helper — so adding public `ExplicitMods`/`ImplicitMods` properties to
  `Core/PoEMemory/Components/Mods.cs` would likely be a small, low-risk follow-up (two one-line
  wrappers around the existing offsets/helper, no new offsets needed). It's simply out of scope for
  *this* pass, which only wires up components from the `proposals/` side without touching `Core/`
  (see [Scope note](#scope-note)) — so `ModsData` still exposes only `ItemMods`. A future pass that is
  allowed to touch `Core/PoEMemory/Components/Mods.cs` could close this gap.
- **`Base.PublicPrice` / `Base.InfluenceFlag` / `Base.ScourgedTier` / `Base.isHunter` /
  `Base.isWarlord` / `Base.isCrusader` / `Base.isRedeemer` / `Base.CurrencyItemLevel`** — absent; the
  fork's `Base` has only `Name`, `ItemCellsSizeX/Y`, `isCorrupted`, `isShaper`, `isElder`
  (`Base.cs:10-15`). The influence / price / scourge flat properties are dropped from `ItemData`.

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
