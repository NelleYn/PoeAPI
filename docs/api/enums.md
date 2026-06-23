# Enums reference

> Categorized reference of every enum under `Core/Shared/Enums/`. All live in namespace `ExileCore.Shared.Enums`. See the [API reference index](README.md) for the rest of the plugin-author docs.

These enums are the typed vocabulary the API speaks: entity classification, item rarity, in-game stat ids, render alignment, inventory slots, and more. Plugins reference them constantly (`using ExileCore.Shared.Enums;`), so the values below are documented strictly from source — no invented members.

## All enums

| Enum | File | Purpose |
| --- | --- | --- |
| `ActionFlags` | `ActionFlags.cs` | `[Flags]` actor action state bits (moving, dead, using ability). |
| `AnimationE` | `AnimationE.cs` | Animation ids copied from GGPK `Animation.dat`. |
| `AreaTransitionType` | `AreaTransitionType.cs` | Kind of area transition (normal, corrupted, labyrinth). |
| `BuffEnums` | `BuffEnums.cs` | Large enumeration of buff/debuff names (ailments, auras, flasks). |
| `CharacterClass` | `CharacterClass.cs` | The seven base classes plus `None`. |
| `ChestType` | `ChestType.cs` | League-flavoured chest categories (Breach, Delve, Strongbox...). |
| `CoroutinePriority` | `CoroutinePriority.cs` | Scheduling priority for coroutines. |
| `CreatureType` | `CreatureType.cs` | Creature rarity/role bitset values from game data. |
| `DamageType` | `DamageType.cs` | Physical / Fire / Cold / Lightning / Chaos. |
| `DiagnosticInfoType` | `DiagnosticInfoType.cs` | Verbosity for diagnostic overlays. |
| `Direction` | `Direction.cs` | Minimal `Down` / `Left` direction. |
| `EntityType` | `EntityType.cs` | Classification of every world `Entity`. |
| `FontAlign` | `FontAlign.cs` | Text alignment for `Graphics.DrawText`. |
| `GameStat` | `GameStat.cs` | Stat id keys (thousands of values) for `Stats` and `MapStats`. |
| `IconPriority` | `IconPriority.cs` | Draw priority for map/world icons. |
| `InventoryIndex` | `InventoryIndex.cs` | Logical equipment/inventory slot index. |
| `InventorySlotE` | `InventorySlotE.cs` | Server inventory slot ids (from GGPK `Inventories.dat`). |
| `InventoryTabFlags` | `InventoryEnums.cs` | `[Flags]` stash tab flags. |
| `InventoryTabMapSeries` | `InventoryTabMapSeries.cs` | Atlas map series of a tab. |
| `InventoryTabPermissions` | `InventoryEnums.cs` | `[Flags]` guild tab permissions. |
| `InventoryTabType` | `InventoryEnums.cs` | Stash tab type (Currency, Map, Premium...). |
| `InventoryType` | `InventoryType.cs` | High-level open inventory kind. |
| `InventoryTypeE` | `InventoryTypeE.cs` | Inventory type ids (from GGPK `InventoryType.dat`). |
| `ItemRarity` | `ItemRarity.cs` | Item rarity (Normal..Unique plus Gem/Currency/Quest/Prophecy). |
| `ItemStatEnum` | `ItemStatEnum.cs` | Computed item stat keys (armour, DPS, resistances...). |
| `ItemType` | `ItemType.cs` | `[Obsolete]` legacy item category. |
| `LeagueType` | `League.cs` | League/mechanic identifier. |
| `MapIconsIndex` | `MapIconsIndex.cs` | Built-in minimap icon sprite indices. |
| `MapType` | `MapType.cs` | Map base type (Normal, Shaped, Unique). |
| `MemoryAllocationState` | `EnumerationsMagic.cs` | Win32 memory allocation state flags. |
| `MemoryAllocationType` | `EnumerationsMagic.cs` | Win32 memory allocation type. |
| `MemoryFreeType` | `EnumerationsMagic.cs` | `[Flags]` Win32 `VirtualFree` flags. |
| `MemoryProtectionType` | `EnumerationsMagic.cs` | Win32 page protection constants. |
| `ModDomain` | `ModDomain.cs` | Mod generation domain (Item, Flask, Monster...). |
| `ModType` | `ModType.cs` | Mod generation type (Prefix, Suffix, Unique...). |
| `MonsterRarity` | `MonsterRarity.cs` | White / Magic / Rare / Unique monster rarity. |
| `MouseActionType` | `MouseActionType.cs` | Current cursor interaction state. |
| `MyMapIconsIndex` | `MyMapIconsIndex.cs` | Custom plugin icon sprite indices. |
| `NetworkStateE` | `NetworkStateE.cs` | Client network connection state. |
| `OffsetsName` | `OffsetsName.cs` | Named base offsets used by memory readers. |
| `PantheonGod` | `PantheonGod.cs` | Selected Pantheon god. |
| `PartyAllocation` | `PartyAllocation.cs` | Loot allocation mode of a party. |
| `PartyStatus` | `PartyStatus.cs` | Player's status within a party. |
| `ProcessAccessRights` | `EnumerationsMagic.cs` | `[Flags]` Win32 process access rights. |
| `StatType` | `StatType.cs` | Display format of a stat value. |
| `ThreadAccessRights` | `EnumerationsMagic.cs` | `[Flags]` Win32 thread access rights. |
| `ToolTipType` | `ToolTipType.cs` | Which tooltip the game is showing. |

Two files declare multiple enums: `InventoryEnums.cs` (`InventoryTabPermissions`, `InventoryTabType`, `InventoryTabFlags`) and `EnumerationsMagic.cs` (the five Win32 memory/process enums). All others are one enum per file.

---

## Entities and the world

### `EntityType`
Classifies every `Entity`. Exposed as `Entity.Type` and used to index entity lookups. Notable values: `None`, `ServerObject`, `Effect`, `Daemon`, then a numbered block starting `Monster = 100`, `Chest`, `SmallChest`, `Npc`, `AreaTransition`, `Portal`, `Player`, `Pet`, `WorldItem`, `Item`, `Terrain`, ... `Error` is the sentinel for an entity that failed to parse. See [entities.md](entities.md).

```csharp
// Radar-style entity filtering (from a reference plugin)
GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster];
if (entity.Type != EntityType.Monster && entity.Type != EntityType.Player) return;
```

### `MonsterRarity`
`White`, `Magic`, `Rare`, `Unique`, and `Error = 10000`. Read from the `ObjectMagicProperties` component and used by `Entity` / `BaseIcon` to colour monsters.

### `CreatureType`
Game-data creature classification with explicit values: `Minion = 1`, `Normal = 2`, `Magic = 4`, `Rare = 7`, `Unique = 10`, `Player = 13`. (Distinct from `MonsterRarity`; these are the raw `.dat` values.)

### `ChestType`
League-flavoured chest categories: `Breach`, `Abyss`, `Incursion`, `Delve`, `Strongbox`, `SmallChest`, `Fossil`, `Perandus`, `Labyrinth`, `Synthesis`, `Legion`.

### `CharacterClass`
The seven base classes (`Scion`, `Marauder`, `Ranger`, `Witch`, `Duelist`, `Templar`, `Shadow`) plus `None`. Read from `ServerData`.

### `ActionFlags` `[Flags]`
Actor action-state bits surfaced through the actor/action components: `None = 0`, `UsingAbility = 2`, `AbilityCooldownActive = 16`, `Dead = 64`, `Moving = 128`, `WashedUpState = 256`, `HasMines = 2048`. Combine with bitwise tests (e.g. `(flags & ActionFlags.Moving) != 0`).

### `AnimationE`
Animation ids copied from GGPK `Animation.dat`, used by the `Actor` component to identify the current animation (`Idle = 0`, `Melee`, `Run`, `Death`, `Cyclone`, `LeapSlam`, ... ~110 values). Summarized — see the source file for the full list.

### `DamageType`
`Physical`, `Fire`, `Cold`, `Lightning`, `Chaos`.

### `Direction`
Minimal two-value enum: `Down`, `Left`.

### `PantheonGod`
Selected Pantheon power: `None`, major gods (`TheBrineKing`, `Arakaali`, `Solaris`, `Lunaris`) and minor gods (`Abberath`, `Gruthkul`, `Yugul`, `Shakari`, `Tukohama`, `Ralakesh`, `Garukhan`, `Ryslatha`, plus placeholder `MinorGodN` slots).

### `BuffEnums`
A very large enumeration of buff/debuff/ailment/aura names (`ignited`, `chilled`, `frozen`, `shocked`, `poison`, `blood_rage`, `righteous_fire`, ... hundreds of entries). Members use the raw game string names. Summarized — consult `BuffEnums.cs` rather than relying on memorised values; in practice buffs are usually matched by name string through the `Buffs` component.

---

## Items and mods

### `ItemRarity`
`Normal`, `Magic`, `Rare`, `Unique`, then non-rarity item classes reusing the enum: `Gem`, `Currency`, `Quest`, `Prophecy`. Exposed via the `Mods` component (`Mods.ItemRarity`). See [components-items.md](components-items.md).

```csharp
// PickIt-style rarity check (from a reference plugin)
if (Rarity == ItemRarity.Unique) { /* ... */ }
```

### `ModType`
Mod generation type with explicit values: `Prefix = 1`, `Suffix = 2`, `Unique = 3`, `Nemesis = 4`, `Corrupted = 5`, `BloodLines = 6`, `Torment = 7`, `Tempest = 8`, `Talisman = 9`, `Enchantment = 10`, `EssenceMonster = 11`.

### `ModDomain`
Mod domain (where a mod can roll): `Item = 1`, `Flask = 2`, `Monster = 3`, `Chest = 4`, `Area = 5`, `Stance = 9`, `Master = 10`, `Jewel = 11`, `Atlas = 12`, `LeagueStone = 13` (values `6`–`8` are placeholders). Both `ModType` and `ModDomain` are consumed when parsing `ModsDat`.

### `ItemStatEnum`
Computed/aggregated item stat keys (not raw `GameStat` ids): `Armor`, `EnergyShield`, `Evasion`, `DPS`, `PhysicalDPS`, `Sockets`, `LinkedSockets`, resistances (`FireResistance`, `ColdResistance`, `LightningResistance`, `ChaosResistance`, `ElementalResistance`, `TotalResistance`), attributes, and many `Local*` variants. Used for filtering/comparing item properties.

### `ItemType` `[Obsolete]`
Legacy item category (`Helm`, `OneHandedWeapon`, `Shield`, `Amulet`, ...). Marked `[Obsolete]`; prefer reading the item's `BaseItemType` metadata instead.

### `MapType`
`Normal`, `Shaped`, two unknowns, and `Unique = 65536`.

> No `SocketColor` enum exists under `Core/Shared/Enums/`; socket colours are exposed elsewhere (via item component data), not as an enum here.

---

## Stats

### `GameStat`
The numeric stat-id vocabulary of the game. This enum is **enormous** (~53,000 lines of source, with values up to `10613`, e.g. `Level = 1`, `ItemDropSlots = 2`, ... `UnaffectedByTemporalTower = 10613`); each member carries an XML-doc description. **Do not enumerate it** — link to the source file and parse/look up the member you need.

It is the dictionary key for in-game stats:

- `Stats` component: `public Dictionary<GameStat, int> StatDictionary` (see [components-combat.md](components-combat.md)).
- `IngameData.MapStats`: `public Dictionary<GameStat, int> MapStats` (see [ingame-state.md](ingame-state.md)).

```csharp
// Reference-plugin usage: look stats up by GameStat key
var itemStats = GetGameStats(mods.ImplicitMods);          // Dictionary<GameStat, int>
var elder = itemStats.GetValueOrDefault(GameStat.MapElderBossVariation);
var passiveStat = Enum.Parse<GameStat>(nameof(GameStat.LocalJewelExpansionPassiveNodeCount));
```

### `StatType`
Display/format hint for a stat value: `Percents = 1`, `Value2 = 2`, `IntValue = 3`, `Boolean = 4`, `Precents5 = 5` (spelling per source).

### `ItemStatEnum`
See [Items and mods](#items-and-mods) above — these are derived item properties, distinct from raw `GameStat` ids.

---

## UI and render

### `FontAlign`
`Left`, `Center`, `Right`. The alignment argument on `Graphics.DrawText(...)` and `DrawTextWithBackground(...)` (default `FontAlign.Left`). See [graphics.md](graphics.md).

```csharp
// Reference-plugin draw call
Graphics.DrawText(text, position, color, FontAlign.Center);
Graphics.DrawTextWithBackground(text, topRight, fontColor, FontAlign.Right, Color.Black);
```

### `MapIconsIndex`
Indices into the built-in minimap icon sprite atlas: `MyPlayer = 1`, `PartyMember = 2`, `OtherPlayer = 3`, `NPC = 4`, `Portal = 5`, `Waypoint = 10`, ... up to ~207 values (e.g. `BlightPortalPhysical = 207`). Used by `BaseIcon`, `SpriteHelper`, and icon extensions.

### `MyMapIconsIndex`
A second sprite set for custom/plugin icons: `Jeweller = 1`, `Armoury`, `Artisan`, `Cartographer`, `Strongbox`, plus many coloured-dot/border variants (`RedDot`, `BlueWithBorderAndGrayDot`, `RGBDot`, `CelestialDot`, ...).

### `IconPriority`
Draw priority for icons: `Low`, `Medium`, `High`, `VeryHigh`, `Critical`.

### `ToolTipType`
Which tooltip is showing: `None`, `InventoryItem`, `ItemOnGround`, `ItemInChat`.

### `MouseActionType`
Current cursor interaction: `Free`, `HoldItem`, `UseItem`, `HoldItemForSell`.

### `DiagnosticInfoType`
Diagnostic-overlay verbosity: `Off`, `Full`, `Short`.

---

## Inventories and stash

See [inventories.md](inventories.md) for how these index the inventory APIs.

### `InventorySlotE`
Server inventory slot ids copied from GGPK `Inventories.dat`: `MainInventory1 = 0`, `BodyArmour1`, `Weapon1`, `Offhand1`, `Helm1`, `Amulet1`, `Ring1`, `Ring2`, `Gloves1`, `Boots1`, `Belt1`, `Flask1`, `Cursor1`, `Map1`, `Weapon2`, `Offhand2`, the master-crafting/league slots, and `StashInventoryId`. Used to index `ServerData.PlayerInventories`.

```csharp
// Reference-plugin slot indexing
var inv = GameController.IngameState.Data.ServerData
              .PlayerInventories[(int)InventorySlotE.MainInventory1];
```

### `InventoryTypeE`
Inventory type ids from GGPK `InventoryType.dat`: `MainInventory = 0`, `BodyArmour`, `Weapon`, `Offhand`, ... `Flask`, `Cursor`, `Map`, `PassiveJewels`, `Currency`, `Divination`, `Essence`, `Fragment`, `MapStashInv`, `UniqueStashInv`, ... `NormalOrQuad`.

### `InventoryType`
Higher-level "which inventory is open" enum: `InvalidInventory` (nothing open), `PlayerInventory`, `NormalStash`, `QuadStash`, `CurrencyStash`, `EssenceStash`, `DivinationStash`, `MapStash`, `FragmentStash`, `DelveStash`.

### `InventoryIndex`
Logical equipment/inventory slot index: `None = 0`, `Helm`, `Amulet`, `Chest`, `LWeapon`, `RWeapon`, `LWeaponSwap`, `RWeaponSwap`, `LRing`, `RRing`, `Gloves`, `Belt`, `Boots`, `PlayerInventory`, `Flask`.

### `InventoryTabType` (in `InventoryEnums.cs`)
Stash tab type: `Normal = 0`, `Premium = 1`, `Currency = 3`, `Map = 5`, `Divination = 6`, `Quad = 7`, `Essence = 8`, `Fragment = 9` (plus `Todo2 = 2`, `Todo4 = 4` placeholders). Backed by `uint`.

### `InventoryTabFlags` `[Flags]` (in `InventoryEnums.cs`)
`byte`-backed stash-tab flag bits: `RemoveOnly = 1`, `Public = 0x20`, `MapSeries = 0x40`, `Hidden = 0x80`, `Premium = 4`, plus `Unknown*` slots.

### `InventoryTabPermissions` `[Flags]` (in `InventoryEnums.cs`)
`uint`-backed guild-tab permissions: `None = 0`, `View = 1`, `Add = 2`, `Remove = 4`.

### `InventoryTabMapSeries`
Map series of an Atlas tab: `None = 0`, `Original = 1`, `The_Awakening = 2`, `Atlas_of_Worlds = 3`, `War_for_the_Atlas = 4`, `Bestiary = 5`.

---

## Areas, leagues, party, network

### `AreaTransitionType`
`Normal = 0`, `Local = 1`, `NormalToCorrupted = 2`, `CorruptedToNormal = 3`, `Labyrinth = 5`.

### `LeagueType`
League/mechanic identifier (enum named `LeagueType`, file `League.cs`): `General`, `Incursion`, `Abyss`, `Breach`, `Perandus`, `Delve`, `Legion`.

### `PartyStatus`
Player's party role: `PartyLeader`, `Invited`, `PartyMember`, `None`.

### `PartyAllocation`
Loot allocation mode (`byte`): `FreeForAll`, `ShortAllocation`, `PermanentAllocation`, `None`, `NotInParty = 160`.

### `NetworkStateE`
Client connection state (`byte`): `None`, `Disconnected`, `Connecting`, `Connected`.

---

## Scheduling, offsets, and Win32 interop

These are framework-internal but documented for completeness.

### `CoroutinePriority`
`Normal`, `High`, `Critical` — priority hint for scheduled coroutines.

### `OffsetsName`
Named base offsets for the memory readers: `Base`, `FileRoot`, `AreaChangeCount`, `IsLoadingScreenOffset`, `GameStateOffset`.

### Win32 memory/process enums (in `EnumerationsMagic.cs`)
Low-level interop flags used by the memory layer; values mirror the Win32 API. Plugins rarely touch these directly.

- `ProcessAccessRights` `[Flags]` — e.g. `PROCESS_VM_READ = 0x10`, `PROCESS_QUERY_INFORMATION = 0x400`, `PROCESS_ALL_ACCESS`.
- `ThreadAccessRights` `[Flags]` — e.g. `THREAD_SUSPEND_RESUME = 0x0002`, `THREAD_ALL_ACCESS`.
- `MemoryProtectionType` — page protection constants (`PAGE_READWRITE = 0x04`, `PAGE_EXECUTE_READWRITE = 0x40`, ...).
- `MemoryAllocationState` — `MEM_COMMIT = 0x1000`, `MEM_RESERVE = 0x2000`, ...
- `MemoryAllocationType` — `MEM_PRIVATE`, `MEM_MAPPED`, `MEM_IMAGE`.
- `MemoryFreeType` `[Flags]` — `MEM_DECOMMIT = 0x4000`, `MEM_RELEASE = 0x8000`.

---

## Source

All enums are defined under `Core/Shared/Enums/` in namespace `ExileCore.Shared.Enums` (40 files; `InventoryEnums.cs` and `EnumerationsMagic.cs` each declare multiple enums). The largest are `GameStat.cs` and `BuffEnums.cs` — open those files directly rather than relying on a printed member list.
