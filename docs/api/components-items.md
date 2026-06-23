# Components: items & inventory

Item data in ExileCore is exposed through components read from game memory. Every component
listed here is fetched from an item or actor [`Entity`](entities.md) via
`Entity.GetComponent<T>()` (returns `null` when the entity lacks the component) — see
[entities.md](entities.md) for the access pattern, `HasComponent<T>()`, and lifetime caveats.

[API reference index](README.md)

All types below live in the `ExileCore.PoEMemory.Components` namespace (the memory-object
helper types live in `ExileCore.PoEMemory.MemoryObjects`). Each derives from `Component`
(itself a `RemoteMemoryObject`), so every instance also exposes:

| Member | Type | Note |
| --- | --- | --- |
| `Owner` | `Entity` | The entity that owns this component. |
| `OwnerAddress` | `long` | Address of the owning entity. |
| `Address` | `long` | Raw memory address of the component (`0` when absent). |
| `DumpObject()` | `string` | Reflection dump of all public properties (debugging). |

> Most components guard their reads with `Address != 0` and return a default value
> (`0`, `""`, `null`) when the component is not present, so reading a property off a
> component you obtained from `GetComponent<T>()` is safe even on partially-loaded items.

> The popular `ItemFilterLibrary` extension methods (e.g. `GetItemData()`, `IsValid`,
> path/base-name helpers) used by many community plugins are an **external library**, not
> part of this API. The members documented here are the raw ExileCore surface those helpers
> are built on.

---

### Base

File: `Core/PoEMemory/Components/Base.cs`

Base item identity: the displayed base name, inventory footprint, corruption, and Shaper/Elder
influence flags. To resolve the item's *class*/base type record, look up `Entity.Metadata`
against the `.dat` files described in [files-in-memory.md](files-in-memory.md)
(`BaseItemTypes`) rather than parsing the name string.

| Property | Type | Note |
| --- | --- | --- |
| `Name` | `string` | Base item name (cached after first read). |
| `ItemCellsSizeX` | `int` | Item width in inventory cells. |
| `ItemCellsSizeY` | `int` | Item height in inventory cells. |
| `isCorrupted` | `bool` | Whether the item is corrupted. |
| `isShaper` | `bool` | Carries Shaper influence. |
| `isElder` | `bool` | Carries Elder influence. |

Usage: `entity.GetComponent<Base>()?.Name` is the canonical way to read a dropped/stash
item's base name.

---

### Mods

File: `Core/PoEMemory/Components/Mods.cs`

The richest item component: rarity, identification state, item/required level, the full
modifier lists, and human-readable stat strings. Modifiers are returned as
[`ItemMod`](#itemmod-memory-object) objects.

| Property | Type | Note |
| --- | --- | --- |
| `ItemRarity` | `ItemRarity` (`ExileCore.Shared.Enums`) | `Normal`/`Magic`/`Rare`/`Unique`/`Gem`/`Currency`/`Quest`/… |
| `UniqueName` | `string` | Unique item name, or `""` when not unique. |
| `Identified` | `bool` | Whether the item is identified. |
| `IsMirrored` | `bool` | Whether the item is mirrored. |
| `IsUsable` | `bool` | Whether the item is usable. |
| `Synthesised` | `bool` | Whether the item is synthesised. |
| `HaveFractured` | `bool` | Has at least one fractured modifier. |
| `CountFractured` | `int` | Number of fractured modifiers. |
| `ItemLevel` | `int` | Item level (defaults to `1` when absent). |
| `RequiredLevel` | `int` | Level required to use the item. |
| `ItemMods` | `List<ItemMod>` | Implicit + explicit modifiers (concatenated). |
| `ItemStats` | `ItemStats` | Parsed stat object (`ExileCore.PoEMemory.Models`). |
| `HumanStats` | `List<string>` | All stat lines as displayed in-game. |
| `HumanCraftedStats` | `List<string>` | Crafted (master-mod) stat lines. |
| `HumanImpStats` | `List<string>` | Implicit stat lines. |
| `FracturedStats` | `List<string>` | Fractured stat lines. |
| `ModsStruct` | `ModsComponentOffsets` | Raw offsets struct (per-frame cached). |
| `Hash` | `long` | Hash over the mods, useful as a cache key. |

> There is no separate `ImplicitMods`/`ExplicitMods`/`EnchantedMods` list property and no
> `IncubatorName` / `CountTryApplyAffixes` member on this type — implicit and explicit mods
> are combined in `ItemMods`, and the human-readable splits are exposed via the `Human*`
> lists. Corruption lives on [`Base.isCorrupted`](#base), not here.

Usage (adapted from FullRareSetManager's `ProcessItem`):

```csharp
var mods = item.GetComponent<Mods>();
if (mods?.ItemRarity == ItemRarity.Rare && !mods.Identified && mods.ItemLevel >= 60)
{
    // candidate rare for a regal/chaos recipe set
}
```

---

### Sockets

File: `Core/PoEMemory/Components/Sockets.cs`

Sockets, links, socket colors, and the gems socketed into the item.

| Property | Type | Note |
| --- | --- | --- |
| `NumberOfSockets` | `int` | Total number of sockets (count of `SocketList`). |
| `LargestLinkSize` | `int` | Size of the biggest linked group. |
| `Links` | `List<int[]>` | One array of socket-color ints per link group. |
| `SocketList` | `List<int>` | Flat list of socket colors (`1`=R `2`=G `3`=B `4`=W `5`=A `6`=O). |
| `SocketGroup` | `List<string>` | Link groups as color strings, e.g. `"RGB"`. |
| `IsRGB` | `bool` | Has a group with at least one R, G and B linked. |
| `SocketedGems` | `List<Sockets.SocketedGem>` | Each pairs `SocketIndex` (`int`) with `GemEntity` (`Entity`). |

> There is no `Colours`/`SocketGroups`/`SocketGroup` count property beyond the list above;
> colors are integers in `SocketList`/`Links`, or the `"RGB…"` strings in `SocketGroup`.

Usage: `entity.GetComponent<Sockets>()?.LargestLinkSize >= 5` to gate on 5-links. (The
`SocketInfo.LargestLinkSize` token used in `.ifl` filter files is the ItemFilterLibrary
wrapper over this property.)

---

### Quality

File: `Core/PoEMemory/Components/Quality.cs`

| Property | Type | Note |
| --- | --- | --- |
| `ItemQuality` | `int` | Item quality percentage (`0` when absent). |

Usage: `entity.GetComponent<Quality>()?.ItemQuality`.

---

### Stack

File: `Core/PoEMemory/Components/Stack.cs`

Present on stackable items (currency, fragments, etc.).

| Property | Type | Note |
| --- | --- | --- |
| `Size` | `int` | Current number of items in the stack. |
| `Info` | [`CurrencyInfo`](#currencyinfo) | Stack metadata (max stack size), or `null`. |

> The maximum stack size is read through `Info.MaxStackSize` (see
> [`CurrencyInfo`](#currencyinfo)); there is no `MaxSize` property directly on `Stack`.

Usage (adapted from PickItV2's stack-merge check):

```csharp
var s = item.GetComponent<Stack>();
bool canStillFit = s.Size < s.Info.MaxStackSize;
```

---

### CurrencyInfo

File: `Core/PoEMemory/Components/CurrencyInfo.cs`

Currency-stack metadata. Usually reached via [`Stack.Info`](#stack) rather than directly.

| Property | Type | Note |
| --- | --- | --- |
| `MaxStackSize` | `int` | Maximum stack size for this currency item. |

> Only `MaxStackSize` exists here; the *current* count lives on [`Stack.Size`](#stack).

---

### Weapon

File: `Core/PoEMemory/Components/Weapon.cs`

| Property | Type | Note |
| --- | --- | --- |
| `DamageMin` | `int` | Minimum physical damage. |
| `DamageMax` | `int` | Maximum physical damage. |
| `AttackTime` | `int` | Attack time in milliseconds (defaults to `1`). |
| `CritChance` | `int` | Critical strike chance. |

Usage: `entity.GetComponent<Weapon>()?.DamageMax`.

---

### Armour

File: `Core/PoEMemory/Components/Armour.cs`

Defensive scores contributed by the item.

| Property | Type | Note |
| --- | --- | --- |
| `ArmourScore` | `int` | Armour rating. |
| `EvasionScore` | `int` | Evasion rating. |
| `EnergyShieldScore` | `int` | Energy shield rating. |

Usage: `entity.GetComponent<Armour>()?.EnergyShieldScore`.

---

### AttributeRequirements

File: `Core/PoEMemory/Components/AttributeRequirements.cs`

| Property | Type | Note |
| --- | --- | --- |
| `strength` | `int` | Required strength. |
| `dexterity` | `int` | Required dexterity. |
| `intelligence` | `int` | Required intelligence. |

Usage: `entity.GetComponent<AttributeRequirements>()?.strength`.

---

### Map

File: `Core/PoEMemory/Components/Map.cs`

Present on map items; exposes tier, world area, and map series.

| Property | Type | Note |
| --- | --- | --- |
| `Tier` | `byte` | Map tier. |
| `Area` | `WorldArea` | World area record (resolved via `Files.WorldAreas`). |
| `MapSeries` | `InventoryTabMapSeries` (`ExileCore.Shared.Enums`) | Map series (Atlas/Legion/…). |
| `MapInformation` | `MapComponentInner` | Inner raw map struct. |

`WorldArea` and the `.dat`-backed lookups come from [files-in-memory.md](files-in-memory.md).

Usage: `entity.GetComponent<Map>()?.Tier`.

---

### RenderItem

File: `Core/PoEMemory/Components/RenderItem.cs`

| Property | Type | Note |
| --- | --- | --- |
| `ResourcePath` | `string` | Resource/icon path of the rendered item (cached). |

Usage: `entity.GetComponent<RenderItem>()?.ResourcePath` to look up the item's art / icon.

---

### WorldItem

File: `Core/PoEMemory/Components/WorldItem.cs`

Present on the *ground container* entity of a dropped item. The actual item lives in a nested
entity. See [components-world.md](components-world.md) for other ground-object components.

| Property | Type | Note |
| --- | --- | --- |
| `ItemEntity` | `Entity` | The dropped item's entity (per-frame cached). |

Usage: on a labelled ground item, `groundEntity.GetComponent<WorldItem>().ItemEntity` gives
the item entity you then read `Base`/`Mods`/`Sockets` from.

---

### SkillGem

File: `Core/PoEMemory/Components/SkillGem.cs`

Present on skill-gem items: level, experience, and required socket color.

| Property | Type | Note |
| --- | --- | --- |
| `Level` | `int` | Current gem level. |
| `MaxLevel` | `int` | Maximum level the gem can reach. |
| `TotalExpGained` | `uint` | Total experience gained by the gem. |
| `ExperiencePrevLevel` | `uint` | Experience at the start of the current level. |
| `ExperienceMaxLevel` | `uint` | Experience required to reach max level. |
| `ExperienceToNextLevel` | `uint` | `ExperienceMaxLevel - ExperiencePrevLevel`. |
| `SocketColor` | `int` | Color of the socket the gem requires. |

> The property names are `ExperienceMaxLevel` / `TotalExpGained` (no `ExperienceMax` or
> `SkillExperience` member). The gem's *name* and active skill come from the related
> [`SkillGemWrapper`](#skillgemwrapper-memory-object) memory object, not from this component.

Usage: `entity.GetComponent<SkillGem>()?.Level`.

---

### Prophecy

File: `Core/PoEMemory/Components/Prophecy.cs`

| Property | Type | Note |
| --- | --- | --- |
| `DatProphecy` | `ProphecyDat` | Prophecy data record (resolved via `Files.Prophecies`). |

Usage: `entity.GetComponent<Prophecy>()?.DatProphecy` (legacy prophecy items).

---

### ObjectMagicProperties

File: `Core/PoEMemory/Components/ObjectMagicProperties.cs`

Rarity and modifier names of magic/rare **monsters/objects** (not equipment mods — that's
[`Mods`](#mods)). Commonly used for monster rarity; see
[components-combat.md](components-combat.md) for related monster components.

| Property | Type | Note |
| --- | --- | --- |
| `Rarity` | `MonsterRarity` (`ExileCore.Shared.Enums`) | Object/monster rarity (`Error` when absent). |
| `Mods` | `List<string>` | Modifier names on the object (cached until `ModsHash` changes). |
| `ModsHash` | `long` | Hash of the object's modifiers. |
| `ObjectMagicPropertiesOffsets` | `ObjectMagicPropertiesOffsets` | Raw offsets struct (per-frame cached). |

Usage: `monster.GetComponent<ObjectMagicProperties>()?.Rarity`.

---

### Inventories

File: `Core/PoEMemory/Components/Inventories.cs`

Present on an **actor** (e.g. the player or an NPC) and exposes the
[`InventoryVisual`](#inventoryvisual) of each equipped slot. (This is the per-actor equipment
*visuals* component; the player's grid inventories themselves are read elsewhere — see
[inventories.md](inventories.md).)

| Property | Type | Note |
| --- | --- | --- |
| `LeftHand` | `InventoryVisual` | Main-hand slot. |
| `RightHand` | `InventoryVisual` | Off-hand slot. |
| `Chest` | `InventoryVisual` | Body armour. |
| `Helm` | `InventoryVisual` | Helmet. |
| `Gloves` | `InventoryVisual` | Gloves. |
| `Boots` | `InventoryVisual` | Boots. |
| `LeftRing` | `InventoryVisual` | Left ring. |
| `RightRing` | `InventoryVisual` | Right ring. |
| `Belt` | `InventoryVisual` | Belt. |
| `Unknown` | `InventoryVisual` | Spare/unknown slot. |

Usage: `actor.GetComponent<Inventories>()?.Chest?.Name`.

---

### InventoryVisual

File: `Core/PoEMemory/Components/InventoryVisual.cs`

Visual descriptor of one equipped item; a `RemoteMemoryObject` (not a `Component`), obtained
via [`Inventories`](#inventories).

| Property | Type | Note |
| --- | --- | --- |
| `Name` | `string` | Visual name of the equipped item. |
| `Texture` | `string` | Texture path. |
| `Model` | `string` | Model path. |

---

## Related memory objects

### ItemMod (memory object)

File: `Core/PoEMemory/MemoryObjects/ItemMod.cs`. Returned by [`Mods.ItemMods`](#mods).

| Property | Type | Note |
| --- | --- | --- |
| `Name` | `string` | Mod name with trailing digits stripped. |
| `RawName` | `string` | Internal mod name as stored. |
| `DisplayName` | `string` | Human-readable display name. |
| `Group` | `string` | Mod group. |
| `Level` | `int` | Mod tier/level parsed from the name (defaults to `1`). |
| `Value1`..`Value4` | `int` | The four roll values of the mod. |

### SkillGemWrapper (memory object)

File: `Core/PoEMemory/MemoryObjects/SkillGemWrapper.cs`.

| Property | Type | Note |
| --- | --- | --- |
| `Name` | `string` | Skill gem display name. |
| `ActiveSkill` | `ActiveSkillWrapper` | Active skill associated with the gem. |

### GrantedEffectsPerLevel (memory object)

File: `Core/PoEMemory/MemoryObjects/GrantedEffectsPerLevel.cs`. Per-level skill data backing a
gem.

| Property | Type | Note |
| --- | --- | --- |
| `SkillGemWrapper` | `SkillGemWrapper` | The gem this granted-effect belongs to. |
| `Level` | `int` | Gem level for this record. |
| `RequiredLevel` | `int` | Character level required. |
| `ManaMultiplier` | `int` | Mana multiplier. |
| `ManaCost` | `int` | Mana cost. |
| `EffectivenessOfAddedDamage` | `int` | Effectiveness of added damage. |
| `Cooldown` | `int` | Cooldown. |
| `Stats` | `IEnumerable<Tuple<StatsDat.StatRecord, int>>` | Stat records with values. |
| `QualityStats` | `IEnumerable<Tuple<StatsDat.StatRecord, int>>` | Quality stat records with values. |
| `TypeStats` | `IEnumerable<StatsDat.StatRecord>` | Type stat records. |
| `Tags` | `IEnumerable<string>` | Gem tags. |

---

## Source

- `Core/PoEMemory/Components/Base.cs`
- `Core/PoEMemory/Components/Mods.cs`
- `Core/PoEMemory/Components/Sockets.cs`
- `Core/PoEMemory/Components/Quality.cs`
- `Core/PoEMemory/Components/Stack.cs`
- `Core/PoEMemory/Components/CurrencyInfo.cs`
- `Core/PoEMemory/Components/Weapon.cs`
- `Core/PoEMemory/Components/Armour.cs`
- `Core/PoEMemory/Components/AttributeRequirements.cs`
- `Core/PoEMemory/Components/Map.cs`
- `Core/PoEMemory/Components/RenderItem.cs`
- `Core/PoEMemory/Components/WorldItem.cs`
- `Core/PoEMemory/Components/SkillGem.cs`
- `Core/PoEMemory/Components/Prophecy.cs`
- `Core/PoEMemory/Components/ObjectMagicProperties.cs`
- `Core/PoEMemory/Components/Inventories.cs`
- `Core/PoEMemory/Components/InventoryVisual.cs`
- `Core/PoEMemory/Component.cs`
- `Core/PoEMemory/MemoryObjects/ItemMod.cs`
- `Core/PoEMemory/MemoryObjects/SkillGemWrapper.cs`
- `Core/PoEMemory/MemoryObjects/GrantedEffectsPerLevel.cs`
- `Core/Shared/Enums/ItemRarity.cs`
- `GameOffsets/MapComponent.cs`
- Plugin cross-check: exApiTools/FullRareSetManager (`FullRareSetManagerCore.cs` `ProcessItem`), exApiTools/PickItV2 (`Misc.cs` stack merge), DetectiveSquirrel/NPCInvWithLinq.
