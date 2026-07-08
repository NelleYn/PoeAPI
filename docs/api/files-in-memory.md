# Static game data (FilesInMemory)

> The game's own static data tables (its `.dat` files) parsed straight from process memory, reached through `GameController.Files`. Use them to turn raw metadata paths and pointers stored on entities/components into named, typed records.

[API reference index](README.md)

## FilesContainer

`ExileCore.PoEMemory.FilesContainer` owns every static data table. A single instance hangs off the game controller:

```csharp
FilesContainer files = GameController.Files;
```

Each table is a property that is **lazily constructed on first access** (`prop ?? (prop = new ...())`) and the underlying `.dat` rows are then read and cached. The first touch of a large table (e.g. `Mods`, `BaseItemTypes`) does real memory work, so prefer caching the resolved record rather than re-looking it up every frame.

On top of the typed tables, `FilesContainer` also exposes the raw file directory it read from the game's in-memory file table:

| Member | Type | Purpose |
| --- | --- | --- |
| `AllFiles` | `Dictionary<string, FileInformation>` | Every file discovered in memory, keyed by path. |
| `Data` | `Dictionary<string, FileInformation>` | Files classified as `.dat` data tables. |
| `Metadata` | `Dictionary<string, FileInformation>` | Files classified as metadata. |
| `OtherFiles` | `Dictionary<string, FileInformation>` | Everything else. |
| `LoadedInThisArea` | `Dictionary<string, FileInformation>` | Files touched during the current area. |
| `ItemClasses` | `ItemClasses` | Parsed item-class definitions. |
| `FindFile(string name)` | `long` | Pointer to a named file (e.g. `"Data/Mods.dat"`); shows an error box and exits if missing. |
| `LoadFiles()` | `void` | Reloads the full file directory synchronously. |
| `event LoadedFiles` | `EventHandler<Dictionary<string, FileInformation>>` | Raised after per-area files are parsed. |

`FileInformation` is an immutable struct describing one file: `Ptr` (pointer to its info record), `ChangeCount` (area-change count when last touched), and the implementation-specific `Test1`/`Test2`.

## The UniversalFileWrapper&lt;T&gt; pattern

Most tables are a `UniversalFileWrapper<RecordType>` (namespace `ExileCore.PoEMemory.FilesInMemory`) where `RecordType : RemoteMemoryObject`. A wrapper is a **keyed collection of records**: a `.dat` file is an array of fixed-width rows, and each row's address becomes a `RecordType` instance.

```csharp
public class UniversalFileWrapper<RecordType> : FileInMemory where RecordType : RemoteMemoryObject, new()
{
    public List<RecordType> EntriesList { get; }          // all rows (cache filled on first access)
    public RecordType GetByAddress(long address);          // row by its record address, or null
    public void CheckCache();                              // force the cache to populate
    protected virtual void EntryAdded(long addr, RecordType entry); // hook for extra indexes
}
```

- `EntriesList` enumerates every record; the backing list/dictionary are populated once on first read and reused.
- `GetByAddress(address)` is the core lookup: components and other records store a pointer into a table, and you pass that pointer to recover the typed record. This is how the framework resolves, e.g., an [`AreaTransition`](components-world.md) pointer back into a `WorldArea`.
- Subclasses override `EntryAdded` to build secondary indexes (by string id, numeric id, metadata path, etc.) while the cache populates.

`FileInMemory` (the base class) provides the row-walking machinery: it holds the file `Address`, reads the record count, and yields each record address via `RecordAddresses()`. `FilesFromMemory` is the lower-level reader that walks the game's file table and projects it into the `AllFiles` dictionary keyed by path. Plugin authors rarely touch these directly — they go through the typed wrappers above.

Some tables (`BaseItemTypes`, `ModsDat`, `StatsDat`, `TagsDat`) extend `FileInMemory` directly rather than `UniversalFileWrapper<T>` and parse their rows into plain dictionaries / nested record classes instead of `RemoteMemoryObject`s.

## The data tables

| Property | File | Record type | Key lookup(s) | Typical use |
| --- | --- | --- | --- | --- |
| `BaseItemTypes` | `Data/BaseItemTypes.dat` | `BaseItemType` (in `.Models`) | `Translate(metadata)`, `GetFromAddress(addr)`, `Contents`, `ContentsAddr` | Resolve an item's base name / class / tags from its metadata path |
| `Mods` | `Data/Mods.dat` | `ModsDat.ModRecord` | `records[key]`, `GetModByAddress(addr)`, `recordsByTier` | Mod names, stats, groups, tiers |
| `Stats` | `Data/Stats.dat` | `StatsDat.StatRecord` | `records[key]`, `recordsById[id]`, `GetStatByAddress(addr)` | Stat keys, display names, value formatting |
| `Tags` | `Data/Tags.dat` | `TagsDat.TagRecord` | `Records[key]` | Tag keys / hashes referenced by mods and bases |
| `WorldAreas` | `Data/WorldAreas.dat` | `WorldArea` | `GetByAddress(addr)`, `GetAreaByAreaId(int)`, `GetAreaByAreaId(string)`, `AreasIndexDictionary` | Area id / name / level / act |
| `MonsterVarieties` | `Data/MonsterVarieties.dat` | `MonsterVariety` | `GetByAddress(addr)`, `TranslateFromMetadata(path)`, `EntriesList` | Monster metadata, mods, multipliers |
| `PassiveSkills` | `Data/PassiveSkills.dat` | `PassiveSkill` | `GetPassiveSkillByPassiveId(int)`, `GetPassiveSkillById(string)`, `GetByAddress(addr)` | Passive tree nodes + granted stats |
| `Quests` | `Data/Quest.dat` | `Quest` | `GetByAddress(addr)`, `EntriesList` | Quest id / act / name |
| `QuestStates` | `Data/QuestStates.dat` | `QuestState` | `GetQuestState(questId, stateId)`, `GetByAddress(addr)` | Quest progress state |
| `Prophecies` | `Data/Prophecies.dat` | `ProphecyDat` | `GetProphecyById(int)`, `GetByAddress(addr)` | Prophecy id / text / seal cost |
| `LabyrinthTrials` | `Data/LabyrinthTrials.dat` | `LabyrinthTrial` | `GetLabyrinthTrialByAreaId(string)`, `GetLabyrinthTrialById(int)`, `GetLabyrinthTrialByArea(WorldArea)` | Trial → world area mapping |
| `AtlasNodes` | `Data/AtlasNode.dat` | `AtlasNode` | `GetByAddress(addr)`, `EntriesList` | Atlas node data |
| `AtlasRegions` | `Data/AtlasRegions.dat` | `AtlasRegion` | `RegionIndexDictionary`, `GetByAddress(addr)` | Atlas region data |
| `BestiaryCapturableMonsters` | `Data/BestiaryCapturableMonsters.dat` | `BestiaryCapturableMonster` | `GetByAddress(addr)`, `EntriesList` | Capturable beasts + capture counts |
| `BestiaryRecipes` | `Data/BestiaryRecipes.dat` | `BestiaryRecipe` | `GetByAddress(addr)` | Bestiary recipe components |
| `BestiaryRecipeComponents` | `Data/BestiaryRecipeComponent.dat` | `BestiaryRecipeComponent` | `GetByAddress(addr)` | Single recipe slot (family/group/genus/mod) |
| `BestiaryGroups` | `Data/BestiaryGroups.dat` | `BestiaryGroup` | `GetByAddress(addr)` | Beast group → family |
| `BestiaryFamilies` | `Data/BestiaryFamilies.dat` | `BestiaryFamily` | `GetByAddress(addr)` | Beast family metadata |
| `BestiaryGenuses` | `Data/BestiaryGenus.dat` | `BestiaryGenus` | `GetByAddress(addr)` | Beast genus → group |
| `BetrayalTargets` | `Data/BetrayalTargets.dat` | `BetrayalTarget` | `GetByAddress(addr)` | Syndicate member |
| `BetrayalJobs` | `Data/BetrayalJobs.dat` | `BetrayalJob` | `GetByAddress(addr)` | Syndicate job |
| `BetrayalRanks` | `Data/BetrayalRanks.dat` | `BetrayalRank` | `GetByAddress(addr)` | Syndicate rank |
| `BetrayalRewards` | `Data/BetrayalTraitorRewards.dat` | `BetrayalReward` | `GetByAddress(addr)` | Reward (job + target + rank) |
| `BetrayalChoises` | `Data/BetrayalChoices.dat` | `BetrayalChoice` | `GetByAddress(addr)` | Syndicate choice |
| `BetrayalChoiceActions` | `Data/BetrayalChoiceActions.dat` | `BetrayalChoiceAction` | `GetByAddress(addr)` | Choice action |
| `BetrayalDialogue` | `Data/BetrayalDialogue.dat` | `BetrayalDialogue` | `GetByAddress(addr)` | Syndicate dialogue |
| `MetamorphMetaSkills` | `Data/MetamorphosisMetaSkills.dat` | `MetamorphMetaSkill` | `GetByAddress(addr)` | Metamorph organ skill |
| `MetamorphMetaSkillTypes` | `Data/MetamorphosisMetaSkillTypes.dat` | `MetamorphMetaSkillType` | `GetByAddress(addr)` | Metamorph skill type + base item |
| `MetamorphMetaMonsters` | `Data/MetamorphosisMetaMonsters.dat` | `MetamorphMetaMonster` | `GetByAddress(addr)` | Metamorph monster (no fields parsed) |
| `MetamorphRewardTypes` | `Data/MetamorphosisRewardTypes.dat` | `MetamorphRewardType` | `GetByAddress(addr)` | Metamorph reward type |
| `MetamorphRewardTypeItemsClient` | `Data/MetamorphosisRewardTypeItemsClient.dat` | `MetamorphRewardTypeItemsClient` | `GetByAddress(addr)` | Metamorph reward items |

Namespaces: tables live in `ExileCore.PoEMemory.FilesInMemory` (and `.FilesInMemory.Atlas` / `.FilesInMemory.Metamorph`); record types in `ExileCore.PoEMemory.MemoryObjects`, except `BaseItemType` which is in `ExileCore.PoEMemory.Models`.

### BaseItemTypes

`BaseItemTypes : FileInMemory`. The single most-used table: it maps an item's metadata path to its base definition.

```csharp
public Dictionary<string, BaseItemType> Contents { get; }      // keyed by metadata path
public Dictionary<long, BaseItemType> ContentsAddr { get; }    // keyed by record address
public BaseItemType Translate(string metadata);                // by metadata path, null if unknown
public BaseItemType GetFromAddress(long address);              // by record address
```

`BaseItemType` (namespace `ExileCore.PoEMemory.Models`) fields:

| Member | Type | Notes |
| --- | --- | --- |
| `Metadata` | `string` | The metadata path that keys this base. |
| `ClassName` | `string` | Item class, e.g. `"Body Armour"`, `"Bow"`. |
| `BaseName` | `string` | Display base name, e.g. `"Vaal Regalia"`. |
| `Width` / `Height` | `int` | Inventory cell size. |
| `DropLevel` | `int` | Minimum drop level. |
| `Tags` | `string[]` | Tags declared on the base. |
| `MoreTagsFromPath` | `string[]` | Extra tags derived from the metadata path (e.g. `two_hand_weapon`). |

`Translate` is what you call against an [entity's](entities.md) `Path` or an [item component's](components-items.md) metadata; it returns `null` (and logs the missing key) for unknown paths, so null-guard the result.

### ModsDat (+ ModRecord)

`ModsDat : FileInMemory`. Constructed with the `Stats` and `Tags` tables so it can resolve each mod's stat names and tags up front.

```csharp
public IDictionary<string, ModRecord> records { get; }                                 // by key (case-insensitive)
public IDictionary<long, ModRecord> DictionaryRecords { get; }                         // by record address
public IDictionary<Tuple<string, ModType>, List<ModRecord>> recordsByTier { get; }     // (group, affix) -> tiers, sorted by level
public ModRecord GetModByAddress(long address);
```

`ModsDat.ModRecord` members: `Key`, `UserFriendlyName`, `Group`, `Tier`, `TypeName`, `AffixType` (`ModType`), `Domain` (`ModDomain`), `MinLevel`, `StatNames` (`StatsDat.StatRecord[]`, up to `NumberOfStats` = 4), `StatRange` (`IntRange[]`), `Tags` (`TagsDat.TagRecord[]`), `TagChances` (`IDictionary<string,int>`), `IsEssence`, `Address`. This is the static mod definition; per-item rolled mods are `ItemMod` (below), reached through the [`Mods` component](components-items.md).

### ItemMod

`ItemMod : RemoteMemoryObject` (namespace `ExileCore.PoEMemory.MemoryObjects`) is the *rolled* mod on an actual item, exposed by the `Mods` component, not a row in `Mods.dat`. Members: `Value1`–`Value4` (rolled values), `RawName`, `Name`, `DisplayName`, `Group`, `Level`. Use the `Group` / `Name` to correlate with a `ModsDat.ModRecord` when you need the static definition.

### StatsDat (+ StatRecord)

`StatsDat : FileInMemory`. Stat descriptions referenced by mods and passives.

```csharp
public IDictionary<string, StatRecord> records { get; }     // by key (case-insensitive)
public IDictionary<int, StatRecord> recordsById { get; }    // by sequential id
public StatRecord GetStatByAddress(long address);
```

`StatsDat.StatRecord` members: `Key`, `UserFriendlyName`, `Type` (`StatType`), `ID`, `Address`, and `ValueToString(int)` which formats a value per the stat type.

### TagsDat (+ TagRecord)

`TagsDat : FileInMemory`. `Records` is keyed by tag key (case-insensitive). `TagsDat.TagRecord` has `Key` and `Hash`.

### WorldAreas (+ WorldArea, AreaTemplate)

`WorldAreas : UniversalFileWrapper<WorldArea>`. Lookups: `GetByAddress(long)`, `GetAreaByAreaId(int)`, `GetAreaByAreaId(string)`, and `AreasIndexDictionary` (by sequential `Index`).

`WorldArea` (in `.MemoryObjects`) members include: `Id`, `Name`, `Act`, `AreaLevel`, `WorldAreaId`, `Index`, `IsTown`, `HasWaypoint`, `IsUnique`, and many id-based predicates (`IsAtlasMap`, `IsMapWorlds`, `IsCorruptedArea`, `IsLabyrinthArea`, `IsMapTrialArea`, `IsAbyssArea`, …), plus `Connections` and `CorruptedAreas` (lists of linked `WorldArea`s).

`AreaTemplate` (in `.MemoryObjects`) is a related record describing an area template: `RawName`, `Name`, `Act`, `IsTown`, `HasWaypoint`, `NominalLevel`, `MonsterLevel`, `WorldAreaId`, `CorruptedAreasVariety`, `PossibleCorruptedAreas`. The live current area itself is `GameController.Area.CurrentArea` (an `AreaInstance`); see [ingame-state.md](ingame-state.md).

### MonsterVarieties (+ MonsterVariety)

`MonsterVarieties : UniversalFileWrapper<MonsterVariety>`. Lookups: `GetByAddress(long)`, `TranslateFromMetadata(string path)`, `EntriesList`.

`MonsterVariety` members include: `VarietyId` (metadata path), `MonsterName`, `BaseMonsterTypeIndex`, `ObjectSize`, `MinimumAttackDistance`/`MaximumAttackDistance`, `ModelSizeMultiplier`, `ExperienceMultiplier`, `DamageMultiplier`, `LifeMultiplier`, `CriticalStrikeChance`, file paths (`ACTFile`, `AOFile`, `AISFile`), and `Mods` (`IEnumerable<ModsDat.ModRecord>`).

### PassiveSkills (+ PassiveSkill)

`PassiveSkills : UniversalFileWrapper<PassiveSkill>`. Lookups: `GetPassiveSkillByPassiveId(int)`, `GetPassiveSkillById(string)`, `GetByAddress(long)`, plus `PassiveSkillsDictionary` (by numeric `PassiveId`).

`PassiveSkill` members: `PassiveId`, `Id`, `Name`, `Icon`, and `Stats` (`IEnumerable<Tuple<StatsDat.StatRecord, int>>` — granted stats paired with values).

### Quests / QuestStates

`Quests : UniversalFileWrapper<Quest>`. `Quest` members: `Id`, `Act`, `Name`, `Icon`.

`QuestStates : UniversalFileWrapper<QuestState>`. Lookup `GetQuestState(string questId, int stateId)` builds a `questId → stateId → QuestState` index on first call. `QuestState` members: `Quest` (resolved via `QuestPtr`), `QuestStateId`, `QuestStateText`, `QuestProgressText`, `TestOffset`.

### Prophecies (PropheciesDat + ProphecyDat)

`PropheciesDat : UniversalFileWrapper<ProphecyDat>`. Lookup `GetProphecyById(int)` (and `GetByAddress`). `ProphecyDat` members: `Id`, `Name`, `ProphecyId`, `Index`, `PredictionText`, `FlavourText`, `IsEnabled`, `SealCost`, `ProphecyChainPtr`, `ProphecyChain` (a `ProphecyChainDat`, resolved from `ProphecyChainPtr`), `ProphecyChainPosition`.

### Bestiary

`BestiaryCapturableMonsters : UniversalFileWrapper<BestiaryCapturableMonster>`; the remaining bestiary tables are plain `UniversalFileWrapper<T>` over the records below. Each record resolves its relatives through `GameController.Files`:

- `BestiaryCapturableMonster`: `Id`, `MonsterName`, `MonsterVariety`, `BestiaryGroup`, `BestiaryGenus`, `AmountCaptured`, `BestiaryEncountersPtr`.
- `BestiaryRecipe`: `Id`, `RecipeId`, `Description`, `Notes`, `HintText`, `Components` (`IList<BestiaryRecipeComponent>`), `RequireSpecialMonster`, `SpecialMonster`.
- `BestiaryRecipeComponent`: `Id`, `RecipeId`, `MinLevel`, `BestiaryFamily`, `BestiaryGroup`, `BestiaryGenus`, `Mod` (`ModsDat.ModRecord`, may be null), `BestiaryCapturableMonster`.
- `BestiaryFamily`: `Id`, `FamilyId`, `Name`, plus art paths (`Icon`, `SmallIcon`, `Illustration`, `PageArt`) and `Description`.
- `BestiaryGenus`: `Id`, `GenusId`, `Name`, `Name2`, `Icon`, `BestiaryGroup`, `MaxInStorage`.
- `BestiaryGroup`: `Id`, `GroupId`, `Name`, `Description`, `Illustration`, `SmallIcon`, `ItemIcon`, `Family`.

### Betrayal (Immortal Syndicate)

All are `UniversalFileWrapper<T>`; records resolve cross-references via `GameController.Files`:

- `BetrayalTarget`: `Id`, `Name`, `FullName`, `MonsterVariety`, `Art`.
- `BetrayalJob`: `Id`, `Name`, `Art`.
- `BetrayalRank`: `Id`, `Name`, `Art`, `Unknown`.
- `BetrayalReward`: `Job`, `Target`, `Rank`, `Reward`.
- `BetrayalChoice`: `Id`, `Name`.
- `BetrayalChoiceAction`: `Id`, `Choice` (`BetrayalChoice`).
- `BetrayalDialogue`: `Target`, `Job`, `Upgrade`, `DialogueText`, key lists, plus several unidentified fields.

### Metamorph

All are `UniversalFileWrapper<T>` in `ExileCore.PoEMemory.FilesInMemory.Metamorph`:

- `MetamorphMetaSkill`: `MonsterVarietyMetadata`, `MetaSkill` (`MetamorphMetaSkillType`), `SkillName`, `GrantedEffect1`, `GrantedEffect2`.
- `MetamorphMetaSkillType`: `Id`, `Name`, `Description`, `BaseItemType`, `BodyPart`.
- `MetamorphRewardType`: `Id`, `Name`, `Art`.
- `MetamorphRewardTypeItemsClient`: `RewardType`, `Description`, `Unknown`.
- `MetamorphMetaMonster`: no fields parsed.

### LabyrinthTrials (+ LabyrinthTrial)

`LabyrinthTrials : UniversalFileWrapper<LabyrinthTrial>`. Lookups: `GetLabyrinthTrialByAreaId(string)`, `GetLabyrinthTrialById(int)`, `GetLabyrinthTrialByArea(WorldArea)`. The static `LabyrinthTrials.LabyrinthTrialAreaIds` lists the known trial area ids. `LabyrinthTrial` members: `Id` and `Area` (a `WorldArea`, resolved through `GameController.Files.WorldAreas`).

## Example: resolve a ground item's base type and read the current area

```csharp
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects; // Entity
using ExileCore.PoEMemory.Models;        // BaseItemType

// Resolve a ground item entity's base type from its metadata path.
// Translate returns null for unknown paths, so null-guard the result.
BaseItemType baseItemType = GameController.Files.BaseItemTypes.Translate(itemEntity.Path);
string className = baseItemType?.ClassName ?? string.Empty;  // e.g. "Bow"
string baseName  = baseItemType?.BaseName  ?? string.Empty;  // e.g. "Imperial Bow"

if (baseName == "Imperial Bow")
{
    // ... act on the item ...
}

// Look up a world area by its string id, and read what area you are in now.
WorldArea hideout = GameController.Files.WorldAreas.GetAreaByAreaId("1_HideoutLuxurious");
string currentArea = GameController.Area.CurrentArea.Name;   // live area (AreaInstance)
int currentLevel   = GameController.Area.CurrentArea.RealLevel;
```

The base-type lookup above is the canonical pattern used across real plugins. From Get-Chaos-Value (`Ninja Price/Main/CustomItem.cs`), adapted:

```csharp
var baseItemType = GameController.Files.BaseItemTypes.Translate(itemEntity.Path);
ClassName = baseItemType?.ClassName ?? string.Empty;
BaseName  = baseItemType?.BaseName  ?? string.Empty;
```

FullRareSetManager (`FullRareSetManagerCore.cs`) does the same against an inventory item's metadata:

```csharp
var itemName = GameController.Files.BaseItemTypes.Translate(item.Metadata).BaseName;
```

## Source

- `Core/PoEMemory/FilesContainer.cs`
- `Core/PoEMemory/FileInMemory.cs`
- `Core/PoEMemory/FilesFromMemory.cs`
- `Core/PoEMemory/FilesInMemory/` (`UniversalFileWrapper.cs`, `BaseItemTypes.cs`, `ModsDat.cs`, `StatsDat.cs`, `TagsDat.cs`, `WorldAreas.cs`, `MonsterVarieties.cs`, `PassiveSkills.cs`, `PassiveSkill.cs`, `Quests.cs`, `QuestStates.cs`, `PropheciesDat.cs`, `LabyrinthTrials.cs`, `LabyrinthTrial.cs`, `BestiaryCapturableMonsters.cs`, `BetrayalChoice.cs`, `BetrayalJob.cs`, `BetrayalRank.cs`, `BetrayalReward.cs`, `BetrayalTarget.cs`, `Atlas/`, `Metamorph/`)
- `Core/PoEMemory/Models/BaseItemType.cs`
- `Core/PoEMemory/MemoryObjects/` (`WorldArea.cs`, `AreaTemplate.cs`, `MonsterVariety.cs`, `ItemMod.cs`, `Quest.cs`, `QuestState.cs`, `ProphecyDat.cs`, `ProphecyChainDat.cs`, `Bestiary*.cs`)
- Plugin cross-check: Get-Chaos-Value (Ninja Price), FullRareSetManager, stashie, EZVendor — `GameController.Files.BaseItemTypes.Translate(...)`.

## Related

- [API reference index](README.md)
- [Entities](entities.md)
- [Item components](components-items.md)
- [In-game state](ingame-state.md)
- [Memory model](memory.md)
