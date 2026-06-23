# IngameState, IngameData & ServerData

> The live snapshot of the running game: UI roots, area data, and server-authoritative
> player/inventory/stash state. Reached as `gameController.IngameState.*`.
> See the [API reference index](README.md).

Namespace: `ExileCore.PoEMemory.MemoryObjects`. All types here are
`RemoteMemoryObject`s — every property reads from the live PoE process and is only valid
while the game is running. `GameController.IngameState` is a shortcut for
`GameController.Game.IngameState` (see [game-controller.md](game-controller.md)), so the
following are equivalent in plugin code:

```csharp
GameController.IngameState.Data.CurrentAreaLevel
GameController.Game.IngameState.Data.CurrentAreaLevel
```

---

## IngameState

`IngameState` is the root accessor for everything that exists once a character is in the
world. It is created once and cached; most members are themselves cached (per-frame or
per-area) so repeated reads in a frame are cheap.

| Member | Type | Notes |
| --- | --- | --- |
| `Data` | `IngameData` | Area / entity / map data. See [IngameData](#ingamedata) below. |
| `ServerData` | `ServerData` | Server-authoritative player state. See [ServerData](#serverdata). |
| `IngameUi` | `IngameUIElements` | Root of the in-game UI tree. See [ui-elements.md](ui-elements.md). |
| `UIRoot` | `Element` | Root UI element (parent of all on-screen elements). |
| `Camera` | `Camera` | World↔screen projection. See [coordinates.md](coordinates.md) — do not reimplement the math. |
| `InGame` | `bool` | `ServerData.IsInGame` (network state is `Connected`). |
| `UIHover` | `Element` | Element currently under the mouse. |
| `UIHoverTooltip` | `Element` | Tooltip element for the hovered item/element. |
| `UIHoverX` / `UIHoverY` | `float` | Mouse-hover coordinates reported by the game. |
| `CurentUElementPosX` / `CurentUElementPosY` | `float` | "Current UI element" position (spelling matches source). |
| `EntityLabelMap` | `long` | Pointer to the entity-label map. |
| `DiagnosticInfoType` | `DiagnosticInfoType` | Which diagnostic overlay PoE is showing. |
| `LatencyRectangle` / `FrameTimeRectangle` / `FPSRectangle` | `DiagnosticElement` | Diagnostic overlay elements. |
| `CurLatency` / `CurFrameTime` / `CurFps` | `float` | Convenience accessors of the rectangles above (`.CurrValue`). |
| `TimeInGame` | `TimeSpan` | Time since entering the game (`TimeSpan.FromSeconds(...)`). |
| `TimeInGameF` | `float` | Same value as a raw float (seconds). |
| `UpdateData()` | `void` | Forces `Data` to re-read from memory. |

> Note: the framework exposes the current mouse cursor position via the
> [GameController](game-controller.md) / `Input` helpers, not directly on `IngameState`.
> `UIHoverX/Y` are the game's own hover coordinates, distinct from raw cursor position.

---

## IngameData

`IngameState.Data`. Per-area data: the current area, the local player entity, map mods, and
pointers into the terrain/entity systems.

| Member | Type | Notes |
| --- | --- | --- |
| `CurrentArea` | `AreaTemplate` | The area template (see [AreaTemplate](#areatemplate--worldarea)). |
| `CurrentWorldArea` | `WorldArea` | Looked up from `Files.WorldAreas` by the current area address. |
| `CurrentAreaLevel` | `int` | Monster/area level of the current zone. |
| `CurrentAreaHash` | `uint` | Instance hash of the current area. |
| `LocalPlayer` | `Entity` | The player's own entity (Player/Life/etc. components). |
| `Terrain` | `TerrainData` | Walkable-grid layout struct. See [../offsets.md](../offsets.md). |
| `EntityList` | `EntityList` | Entity-list memory object. Prefer `GameController.Entities` for enumeration. |
| `EntitiesCount` | `long` | Number of entities in the list. |
| `EntiteisTest` | `long` | Raw `DataStruct.EntityList` pointer (spelling matches source). |
| `LabyrinthData` | `LabyrinthData` | Labyrinth layout, or `null` when not in a Lab. |
| `MapStats` | `Dictionary<GameStat, int>` | Map mods of the current area, keyed by [`GameStat`](enums.md). |
| `TownPortals` | `IList<IngameData.PortalObject>` | Open town portals (player owner + destination area). |
| `DataStruct` | `IngameDataOffsets` | The raw backing struct (see [../offsets.md](../offsets.md)). |

`MapStats` reads the stat key/value pairs out of memory and caches them until the backing
pointer changes; it returns `null` if the implied count looks corrupt (>200 stats).

> `ServerData` is reached via `IngameState.ServerData`, **not** `IngameState.Data.ServerData`
> — `IngameData` does not expose a `ServerData` member in this build. (Some older upstream
> ExileApi code wrote `IngameState.Data.ServerData`; that path does not compile here.)

### AreaTemplate & WorldArea

`CurrentArea` is an `AreaTemplate`; `CurrentWorldArea` is the matching `WorldArea` record.
Notable members (both are `RemoteMemoryObject`s):

| `AreaTemplate` | `WorldArea` |
| --- | --- |
| `RawName`, `Name` | `Id`, `Name`, `Index` |
| `Act`, `IsTown`, `HasWaypoint` | `Act`, `IsTown`, `HasWaypoint`, `AreaLevel` |
| `MonsterLevel`, `NominalLevel` | `IsAtlasMap`, `IsMapWorlds`, `IsLabyrinthArea`, `IsUnique` |
| `WorldAreaId` | `WorldAreaId`, `Connections` |

`AreaController.CurrentArea` (an `AreaInstance`, accessed as `GameController.Area.CurrentArea`)
is the higher-level wrapper most plugins use for area name/level — see
[game-controller.md](game-controller.md).

---

## ServerData

`IngameState.ServerData`. Server-authoritative state: network status, player skills/passives,
inventories, stash tabs, and league/atlas data. Backed by `ServerDataOffsets`
(see [../offsets.md](../offsets.md)); the raw struct is `ServerDataStruct`.

| Member | Type | Notes |
| --- | --- | --- |
| `IsInGame` | `bool` | `NetworkState == Connected`. |
| `NetworkState` | `NetworkStateE` | `None`/`Disconnected`/`Connecting`/`Connected`. |
| `Latency` | `int` | Round-trip latency (ms). Commonly added to action delays. |
| `League` | `string` | Current league name. |
| `Guild` | `string` | Player's guild name. |
| `PlayerClass` | `CharacterClass` | Masked low nibble of the raw class byte. |
| `CharacterLevel` | `int` | Player level. |
| `MonsterLevel` | `byte` | Effective monster level of the area. |
| `MonstersRemaining` | `byte` | Remaining monsters (51 == "more than 50"; 255 == unsupported map). |
| `LastActionId` | `ushort` | Id of the last server action. |
| `PassiveRefundPointsLeft`, `FreePassiveSkillPointsLeft`, `QuestPassiveSkillPoints` | `int` | Passive-point counters. |
| `TotalAscendencyPoints`, `SpentAscendencyPoints` | `int` | Ascendancy points. |
| `SkillBarIds` | `IList<ushort>` | The 13 skill-bar slot ids. |
| `PassiveSkillIds` | `IList<ushort>` | Allocated passive-skill ids. |
| `PartyAllocationType` | `PartyAllocation` | Loot allocation mode. |
| `PartyStatusType` | `PartyStatus` | Party membership status. |
| `NearestPlayers` | `IList<Player>` | Nearby player components (party/area players). |
| `CurrentSulphiteAmount` | `ushort` | Niko/Azurite sulphite. |
| `CurrentAzuriteAmount` | `int` | Azurite currency. |
| `BetrayalData` | `BetrayalData` | Immortal Syndicate / Betrayal board state. |
| `TradeChatChannel`, `GlobalChatChannel` | `ushort` | Chat channel ids. |

### Inventories (on ServerData)

| Member | Type | Notes |
| --- | --- | --- |
| `PlayerInventories` | `IList<InventoryHolder>` | All of the player's server-side inventories. |
| `NPCInventories` | `IList<InventoryHolder>` | NPC/vendor inventories. |
| `GuildInventories` | `IList<InventoryHolder>` | Guild stash inventories. |
| `GetPlayerInventoryBySlot(InventorySlotE)` | `ServerInventory` | First player inventory with the given slot. |
| `GetPlayerInventoryByType(InventoryTypeE)` | `ServerInventory` | First player inventory with the given type. |
| `GetPlayerInventoryBySlotAndType(InventoryTypeE, InventorySlotE)` | `ServerInventory` | Matches both. |

See [inventories.md](inventories.md) for the higher-level inventory UI helpers.

### Stash tabs (on ServerData)

| Member | Type | Notes |
| --- | --- | --- |
| `PlayerStashTabs` | `IList<ServerStashTab>` | Player stash tabs (may include hidden). |
| `GuildStashTabs` | `IList<ServerStashTab>` | Guild stash tabs. |

### Completed areas / Atlas

| Member | Type | Notes |
| --- | --- | --- |
| `CompletedAreas` | `IList<WorldArea>` | Areas completed on the atlas. |
| `BonusCompletedAreas` | `IList<WorldArea>` | Bonus-objective completed areas. |
| `GetAtlasRegionUpgradesByRegion(int)` / `(AtlasRegion)` | `byte` | Watchstone-upgrade tier for a region. |
| `GetBeastCapturedAmount(BestiaryCapturableMonster)` | `int` | Captured-beast count. |

> `ShapedMaps`, `ElderGuardiansAreas`, `MasterAreas`, `ShaperElderAreas` exist but currently
> return empty lists in this build (their reads are commented out in source).

---

## ServerInventory

Returned by `InventoryHolder.Inventory` and the `ServerData.GetPlayerInventory*` helpers. A
ServerInventory is a grid of item slots plus its slot/type classification.

| Member | Type | Notes |
| --- | --- | --- |
| `InventType` | `InventoryTypeE` | Inventory category (`MainInventory`, `Currency`, `Map`, ...). |
| `InventSlot` | `InventorySlotE` | Equipment/grid slot (`MainInventory1`, `BodyArmour1`, ...). |
| `Columns` / `Rows` | `int` | Grid dimensions. |
| `IsRequested` | `bool` | Whether the server data was requested/populated. |
| `TotalItemsCounts` | `int` | Item count. |
| `CountItems` | `long` | Item count (long read of the same field). |
| `ServerRequestCounter` | `int` | Server request counter. |
| `InventorySlotItems` | `IList<InventSlotItem>` | All occupied slots (item + grid rect). |
| `Items` | `IList<Entity>` | Just the item entities. |
| `this[int x, int y]` | `InventSlotItem` | Item at a grid cell, or `null`. |
| `Hash` | `long` | Backing hash-map hash. |

`ServerInventory.InventSlotItem` exposes `Item` (`Entity`), `InventoryPosition`
(`Vector2`), `PosX`/`PosY`, `SizeX`/`SizeY`, and `GetClientRect()` (on-screen rect within the
player inventory panel). `InventoryHolder` simply pairs an `Id` (`int`) with its `Inventory`.

### InventorySlotE / InventoryTypeE

`InventorySlotE` (slot identity): `MainInventory1`, `BodyArmour1`, `Weapon1`, `Offhand1`,
`Helm1`, `Amulet1`, `Ring1`, `Ring2`, `Gloves1`, `Boots1`, `Belt1`, `Flask1`, `Cursor1`,
`Map1`, ... through stash/crafting/league slots.

`InventoryTypeE` (category): `MainInventory`, `BodyArmour`, `Weapon`, `Offhand`, `Helm`,
`Amulet`, `Ring`, `Gloves`, `Boots`, `Belt`, `Flask`, `Cursor`, `Map`, `Currency`,
`Divination`, `Essence`, `Fragment`, ... See [enums.md](enums.md).

---

## ServerStashTab

An element of `ServerData.PlayerStashTabs` / `GuildStashTabs`.

| Member | Type | Notes |
| --- | --- | --- |
| `Name` | `string` | Tab name. |
| `TabType` | `InventoryTabType` | Normal / Premium / Currency / Map / etc. |
| `VisibleIndex` | `ushort` | Display order index. |
| `Color` / `Color2` | `uint` / `Color` | Tab color (raw / SharpDX). |
| `Flags` | `InventoryTabFlags` | Raw flags. |
| `RemoveOnly` | `bool` | Remove-only tab. |
| `IsHidden` | `bool` | Hidden tab (e.g. premium map sub-tabs). |
| `MemberFlags` / `OfficerFlags` | `InventoryTabPermissions` | Guild permissions. |

---

## Game state machine (TheGame)

The game's state machine lives on `TheGame` (`GameController.Game`); there is no separate
`GameStateController` class — `GameStateContoller.cs` defines `TheGame`, `GameState`, and
`AreaLoadingState`. `TheGame` reads the named hash-map of all game states and exposes
boolean checks for the active one:

| Member | Type | Notes |
| --- | --- | --- |
| `AllGameStates` | `Dictionary<string, GameState>` | All states keyed by name (`InGameState`, `LoginState`, `LoadingState`, ...). |
| `IngameState` | `IngameState` | The `InGameState` cast to `IngameState`. |
| `LoadingState` | `AreaLoadingState` | The `AreaLoadingState`; exposes `IsLoading`, `AreaName`. |
| `IsInGameState` | `bool` | In game with a selected character. |
| `IsPreGame`, `IsLoginState`, `IsSelectCharacterState`, `IsWaitingState`, `IsLoadingState`, `IsEscapeState` | `bool` | Other state checks. |
| `IsLoading` | `bool` | `LoadingState.IsLoading`. |
| `InGame` | `bool` | True when address/data/serverdata are valid and not loading. |
| `AreaChangeCount` | `int` | Increments on each area change. |
| `CurrentAreaHash` | `uint` | Convenience read of the current area hash. |
| `CurrentGameStates` / `ActiveGameStates` | `IList<GameState>` | State stacks. |
| `Files` | `FilesContainer` | In-memory data files. See [files-in-memory.md](files-in-memory.md). |

---

## Examples

### Current area name and level

```csharp
var data = GameController.IngameState.Data;
LogMessage($"Area: {data.CurrentArea.Name} (level {data.CurrentAreaLevel})");

// Higher-level alternative most plugins use:
var area = GameController.Area.CurrentArea;        // AreaInstance
LogMessage($"{area.Area.Name} hash={area.Hash}");
```

### Read a map mod (MapStat)

```csharp
var mapStats = GameController.IngameState.Data.MapStats;
if (mapStats != null &&
    mapStats.TryGetValue(GameStat.MapItemDropQuantityPct, out var iiq))
{
    LogMessage($"Map IIQ: {iiq}%");
}
```

### Enumerate player inventory items via ServerData

Adapted from Stashie (`Stashie.cs`) and FullRareSetManager (`FullRareSetManagerCore.cs`),
which iterate `ServerData.PlayerInventories[0].Inventory.InventorySlotItems`:

```csharp
var inventory = GameController.IngameState.ServerData.PlayerInventories[0].Inventory;
foreach (var slotItem in inventory.InventorySlotItems)
{
    Entity item = slotItem.Item;                 // the item entity
    var pos = slotItem.InventoryPosition;        // grid cell (x, y)
    LogMessage($"{pos}: {item?.Path}");
}

// Or address a single equipment slot:
var helm = GameController.IngameState.ServerData
    .GetPlayerInventoryBySlot(InventorySlotE.Helm1);
```

> The reference plugins call `IngameState.Data.ServerData.PlayerInventories` (older API).
> In this build the verified path is `IngameState.ServerData.PlayerInventories`.

### Gate work on game state / latency

```csharp
if (!GameController.Game.IsInGameState || GameController.Game.IsLoading)
    return;

// Add server latency to action delays (as Stashie/FRSM do):
var delay = (int)GameController.IngameState.ServerData.Latency + Settings.ExtraDelay;
```

---

## Source

- `Core/PoEMemory/MemoryObjects/IngameState.cs`
- `Core/PoEMemory/MemoryObjects/IngameData.cs`
- `Core/PoEMemory/MemoryObjects/ServerData.cs`
- `Core/PoEMemory/MemoryObjects/ServerInventory.cs`
- `Core/PoEMemory/MemoryObjects/InventoryHolder.cs`
- `Core/PoEMemory/MemoryObjects/ServerStashTab.cs`
- `Core/PoEMemory/MemoryObjects/GameStateContoller.cs` (defines `TheGame`)
- `Core/PoEMemory/MemoryObjects/AreaTemplate.cs`, `WorldArea.cs`
- `Core/Shared/Enums/{InventorySlotE,InventoryTypeE,NetworkStateE,GameStat,CharacterClass}.cs`
- `GameOffsets/{IngameStateOffsets,IngameDataOffsets,ServerDataOffsets}.cs`
