# Compatibility: this fork vs. ExileApi‑Compiled

A porting reference for plugin authors moving an upstream plugin onto this fork. It maps,
concretely and by member, what the widely‑used **ExileApi‑Compiled** distribution exposes
that this fork lacks or names differently. For the plugin‑author API as it actually exists
here, start at the [API reference index](README.md); this document is the *delta*.

## Why this exists

This repository is a **modernized but slimmer/older snapshot** of ExileCore: 309 `Core/`
source files, 53 components. The compiled distribution most reference plugins are written
against is much larger — it adds whole league/mechanic subsystems (Heist, Sanctum, Village
currency exchange, Azmeri, Ancestor, Necropolis, Ultimatum, Expedition, Harvest, Bestiary,
Delve…) plus richer members on types that already exist here. The doc workers who wrote
`docs/api/*.md` repeatedly hit this: properties such as `Entity.PosNum`,
`Mods.EnchantedMods`, `Stack.MaxSize`, `Inventory.NestedVisibleInventoryIndex` and the path
`IngameState.Data.ServerData` appear in real plugins but did **not** compile here. This file
collects those gaps in one place. The highest‑traffic gaps have since been closed: some as
real Core members (`Entity.PosNum`/`GridPosNum`), the rest as extension methods in
`Core/Shared/Compat/` (namespace `ExileCore.Shared`) — rows below marked **compat‑helper**.

### Ground truth and its caveats

Type‑level gaps are derived from a faithful **reconstruction of the compiled `ExileCore.dll`**
(assembly `10.0.14.7603`), kept on branch `origin/claude/ecstatic-ritchie-cje3s8` (PR #18):
698 `Core/` files, 92 components, 428 types. Its metadata — type/field/property/**method
signatures** and enum values — is reliable; see its `docs/ported-from-dll.md`. Two caveats:

- **Stubbed bodies.** ~17% of method bodies (610 methods) were protected in the DLL and are
  replaced with `throw new NotImplementedException("Body protected in source DLL")`.
  Signatures are still trustworthy; *behavior* of stubbed methods is not.
- **Build‑specific offsets.** The reconstruction targets a specific PoE build (328.8 offsets).
  Hard‑coded offsets in either tree are version‑bound and not portable as numbers.

> **Important nuance.** The reconstruction was extended *on top of this fork's `Core/`*
> (shared merge‑base `b5a9038`). It therefore **adds 389 new files but leaves the
> pre‑existing shared types byte‑for‑byte identical** to this fork. So a member like
> `Mods.ImplicitMods` is absent from the reconstruction *too* — it is a genuine
> ExileApi‑Compiled member that lives only in the upstream distribution. For those rows the
> evidence is **real reference‑plugin call sites** (cited in the Notes column), not the
> reconstruction tree. Type‑level rows in §2 *are* verified directly against the
> reconstruction tree.

---

## 2. Types absent in this fork

Counts below are `.cs` files present in `origin/claude/ecstatic-ritchie-cje3s8` `Core/` but
not in this fork's `Core/` (389 files total). All verified by diffing
`git ls-tree -r --name-only`.

### Components (39 absent)

`AnimationController`, `AnimationStage`, `AnimationStageList`, `ActiveAnimationData`,
`AnimatedRender`, `SupportedAnimationList`, `AttachedAnimatedObject`(`Attachment`),
`ParticleEffects`, `EffectPack`, `GroundEffect` (animation/FX); `Buffs` (entity buff list as
a component — here buffs are read via `Entity.Buffs`), `LocalStats`, `Shield`, `Movement`,
`Usable`, `CapturedMonster`, `SentinelDrone`, `Tincture`, `ItemInfoData`, `MapKey`,
`StateMachineState`, `SoundEvents`, `NecropolisCorpse`, `ArmourStatRange`; league‑specific
`HeistBlueprint`, `HeistContract`, `HeistEquipment`, `HeistRewardDisplay`, `ExpeditionSaga`,
`HarvestInfrastructure(+Mod/+ModUnmanaged)`, `HarvestSeedSpawnDescriptor`,
`HarvestWorldObject`, `BrequelFruit`, `UltimatumTrial`; plus nested types
`Mods.StatWithValue`, `Sockets.Socket`.

### MemoryObjects (61 absent)

Skill/cooldown: `ActorSkillCooldown`, `SkillCooldown`. Atlas: `AtlasInfluencedMap`,
`AtlasInfluencedRegion`, `AtlasMissionType`. Heist (`Heist/`): `HeistChestRecord`,
`HeistChestRewardTypeRecord`, `HeistJobRecord`, `HeistNpcRecord`. Ancestor (`Ancestor/`):
`AncestorFightSelectionWindow`, `AncestorFightSelectionOpponentLine`,
`AncestorMainShopWindow(+Option)`, `AncestorSidePanelOption`, `AncestorSideShopPanel`,
plus `AncestorFightOption(+Reward)`, `AncestorServerData`. Trade/vendor:
`TradeWindow`, `CardTradeWindow`, `VendorInventory`, `SellWindowHideout`, `PurchaseWindow`
(element), `CraftBenchWindow`, `MapDeviceWindow`(+`CraftingSelectorElement`,
`PrimordialChoiceElement`), `QuestRewardWindow`, `MercenaryEncounterWindow`,
`GemLvlUpPanel`/`GemLevelUpElement`. Sentinel: `SentinelData`, `SentinelState`. Party/social:
`PartyPlayerInfo(+Type)`, `RemotePlayerInfo`, `ServerPlayerData`. Currency exchange:
`PlacedCurrencyExchangeOrder`. Environment/FX: `EnvironmentData(+Environment)`,
`EffectEnvironment`, `EntityEffect`, `DefaultEnvironmentSetting`, `EnvironmentSettingValue`,
`TypedEnvironmentData`. Game state/config: `GameStateTypes`, `GameConfig(+Section,+KeyValue)`,
`LoginState`, `EscapeState`, `GameUi`, `MechanicHandler`, `HoverItemState`, `Camera.CameraSnapshot`.
Misc/nested: `BetrayalRelationshipStatus`, `ServerDataMinimapIcon`, `PlayerInventory`,
`ServerData.QuestFlagStore`, `IngameData.TileIndexStruct`, `IngameUIElements.QuestListNode`,
`ExpeditionAreaData`.

### Elements & InventoryElements (114 absent)

Whole panels/windows: `AtlasPanel`, `Atlasbonus`, `AtlasMasterMissionPanelElement`,
`VoidStoneSlot`, `MasterMissionColour` (`AtlasElements/`); `TreePanel`, `TreePassiveElement`;
`ChatPanel`, `SocialElement`, `PartyElement`(+`PartyElementPlayerElement`,
`PartyElementPlayerInfo(+Wrapper)`), `InvitesPanel(+Item,+ItemKind)`, `PartyInvite`,
`PartyTabElement`, `SocialPartyMember`, `SocialTabTypes`; `ChallengePanel`(+`TabContainer`,
`TabContainerTabInfo`), `ResurrectPanel`, `PopUpWindow`, `InstanceManagerPanel`,
`NpcDialog`/`NpcLine`, `BanditDialog`/`BanditType`, `DivineFontPanel`, `SearchBarElement`,
`DropdownElement(+Option)`, `ShortcutSettings`, `TooltipItemFrameElement`. Stash/vendor:
`StashTabElement`, `StashTabContainer(+Inventory)`, `StashTopTabSwitcher`,
`VendorStashTabContainer(+Inventory)`, `TrappedStashWindow`, `MapReceptacleWindow`,
`KalandraTabletWindow`, `TabletChoiceElement`, `TabletTileElement`. Price‑check:
`ItemRightClickPriceMenu`/`IItemRightClickPriceMenu`/`AsyncItemRightClickPriceMenu`.
League panels: `RitualWindow`; `SentinelPanel`/`SentinelSubPanel`; `Necropolis/`
(`NecropolisMonsterPanel`, `NecropolisCollectableCorpse`, `NecropolisMonsterPanelMonsterAssociation`);
`Sanctum/` (`SanctumFloorWindow(+Data,+DataSelector)`, `SanctumRoomElement`, `SanctumRoomData`,
`SanctumRewardWindow`); `Ultimatum*` (`UltimatumPanel`, `UltimatumChoicePanel`,
`UltimatumChoiceElement`); `Expedition*` (`ExpeditionDetonator(+Info)`, `ArtifactSliderElement`,
`ExpeditionVendorElement`, `ExpeditionVendorCurrencyInfoElement`, `TujenHaggleWindowElement`);
`Harvest*` (`HarvestWindow`, `HarvestCraftElement`); `Archnemesis*`
(`ArchnemesisPanelElement`, `ArchnemesisAltarElement`, `Archnemesis(Altar)InventorySlot`,
`AltarEntity`); `Azmeri*` (`AzmeriElement`, `AzmeriData`); `Bestiary*` (`BestiaryTab`,
`CapturedBeast`, `CapturedBeastsTab`); `Village/` (~24 elements: `CurrencyExchangePanel`,
`VillageScreen`, `VillageRecruitmentPanel`, `VillageWorkerManagementPanel`, `VillageShipmentScreen`,
…). Mapping: **`SubMap`** (the cast target for `Map.LargeMap`, carries `MapCenter`/`MapScale`/`Zoom`).
New **InventoryElements** (5): `BlightInventoryItem`, `DeliriumInventoryItem`,
`FlaskInventoryItem`, `GemInventoryItem`, `MetamorphInventoryItem` (this fork has only
`Normal/Currency/Delve/Divination/Essence/Fragment/MapStashTab`).

### FilesInMemory — static data tables (90 absent)

Stat translation pipeline: `StatDescription(+Section,+StringContainer,+Wrapper)`,
`CachedStatDescription(+Section)`, `StatHandling`, `StatTranslationUtils`,
`ModTranslationReplacerInput`. Gems/skills: `SkillGemDat(+SocketType)`, `GemEffect`,
`GemEffects`, `GrantedEffect`, `GrantedEffectPerLevel`, `IndexableSupportGem`,
`SkillArtVariation`, `AlternateQualityType`. Items/uniques: `UniqueItemDescription(s)`,
`ItemVisualIdentity(ies)`, `CurrencyItemDat`, `TinctureDat`, `MapKeyDat`, `Character`,
`Ascendancy`. Visuals/icons: `BuffDefinition`, `BuffVisual`, `MinimapIconDat`,
`MiscAnimatedDat(+List,+ArtVariation)`, `MapPin`, `GroundEffect(Type)Dat`, `BlightTowerDat`.
Quests: `QuestFlagDat`, `QuestFlagsDat`, `QuestReward(+Offer)`. Mechanic tables: `ArchnemesisMod`,
`Archnemesis/ArchnemesisRecipe`, `Sanctum/` (`SanctumRoom(+Type)`, `SanctumDeferredReward(+Category)`,
`SanctumPersistentEffect(+Category)`), `Ultimatum/UltimatumModifier`, `UltimatumItemisedReward(+Type)`,
`Necropolis*` (`NecropolisCraftingMod`, `NecropolisPack(+Mod,+ModTier)`, `PackFrequencyName`),
`Harvest/HarvestSeed`, `DelveBiome`, `DelveFeature`, `LakeRoom`, `StampChoice`,
`AtlasPrimordial*` (`AltarChoice(+Type)`, `BossOption`), `Labyrinth/` (`LabyrinthArea`,
`LabyrinthSectionDat`, `LabyrinthSectionLayout`, `LabyrinthNodeOverride`), `BestiaryRecipeCategory`,
`Ancestor/` (`AncestralTrialItem`, `AncestralTrialTribe`, `AncestralTrialUnit`),
`CurrencyExchangeCategory`, `CurrencyExchangeEntry`, `Village/` (~14: `VillageJob(s,+Type)`,
`VillageUpgrade(+Category)`, `VillageResource`, `VillageExport`, `VillageProduction`,
`VillageShippingPort`, …), `VillageUniqueDisenchantValue`, `ChestRecord`, `ClientString`,
`WordEntry`.

### Shared/Nodes (6 absent)

`ContentNode`, `ContentNodeConverter`, `IContentNodeBase`, `CustomNode`, `HotkeyNodeV2`,
`JsonSerializationHelper`. This fork ships `Toggle/Range/Hotkey/Color/Button/List/Text/File/
StashTab/Empty` only — see [settings.md](settings.md).

### Shared/Attributes (2 absent)

`SubmenuAttribute` (`[Submenu]`), `ConditionalDisplayAttribute` (`[ConditionalDisplay]`).
This fork has only `[Menu]` and `[HideInReflection]`.

### Shared/Enums (8 absent)

`HeistJobE`, `Influence`, `InfluenceTypes`, `InventoryNameE`, `InventoryTabAffinity`,
`QuestFlag`, `SkillGemQualityTypeE`, `SocketColor`.

### Shared/Helpers (3 absent)

`InputHelper`, `WindowsUtils`, `MoreLinq/PairwiseExtension`.

### Shared/Cache (3 absent)

`CacheUtils`, `KeyTrackingCache`, `CachedValue.CacheUpdateEvent`.

### Whole new subsystems

By league/mechanic (file groups absent here): **Heist**, **Expedition**, **Harvest**,
**Sanctum**, **Ultimatum**, **Necropolis**, **Village / Currency Exchange**, **Azmeri**,
**Ancestor (Trial of the Ancestors)**, **Archnemesis**, **Bestiary**, **Delve** (Dat tables),
**Labyrinth**, **Atlas Primordial / Influence**, **Sentinel**, **Party / Social**, and the
full **stat‑description translation pipeline**.

---

## 3. Member / function divergences

The heart of the doc. *Status* legend: **missing** (member not in this fork at all),
**renamed** (same concept, different name here), **signature‑diff** (present but different
shape/type), **compat‑helper** (emulated by an extension method shipped in
`Core/Shared/Compat/`, namespace `ExileCore.Shared` — note these are *methods*, so upstream
*property* call sites need parentheses, e.g. `render.RotationNum().X` instead of
`render.RotationNum.X`). Unless the Notes say "in reconstruction", the upstream member is
absent from the reconstruction tree too and is confirmed via the cited reference‑plugin call
site; the *This fork* column names the in‑repo equivalent (file cited where non‑obvious).

### Entity & EntityListWrapper

| Upstream (compiled) member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `Entity.PosNum` : `System.Numerics.Vector3` | `Entity.PosNum` (instance property) | present | Used in `WAYG/WhereAreYouGoing.cs:187`. Now a real instance property — `Core/PoEMemory/MemoryObjects/Entity.cs:140` (SharpDX `Pos` remains at `Entity.cs`). |
| `Entity.GridPosNum` : `Vector2` (Numerics) | `Entity.GridPosNum` (instance property) | present | `WAYG:374`. Now a real instance property — `Entity.cs:179`. |
| `Entity.BoundsNum` (via Render) | `Entity.BoundsNum()` extension | compat‑helper | `WAYG:315`. Still no `Bounds` on `Entity`; the helper (`Core/Shared/Compat/NumericsCompat.cs`) reads `Render.Bounds`, returns `Vector3.Zero` without a `Render`. |
| `Entity.TryGetComponent<T>(out T)` : `bool` | `Entity.TryGetComponent<T>(out T)` extension | compat‑helper | `WAYG:179` `player.TryGetComponent<Positioned>(out var p)` now compiles via `Core/Shared/Compat/EntityCompat.cs` (thin wrapper over `GetComponent<T>()`). |
| `EntityListWrapper.NotOnlyPlayer` | `Entities` / `OnlyValidEntities` / `NotOnlyValidEntities` | renamed/missing | Fork list members in `Core/EntityListWrapper.cs:128‑142`; there is no `NotOnlyPlayer`. |

### Components — combat & character

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `Actor.ActorVaalSkills` | `Actor.ActorSkills` only | missing | `Core/PoEMemory/Components/Actor.cs:79` has `ActorSkills`/`DeployedObjects`/`CurrentAction`; no Vaal‑skills list. |
| `Buffs` (component) | `Entity.Buffs` (`List<Buff>`) | missing | No `Buffs` *component*; here buffs come off the entity. `Core/PoEMemory/MemoryObjects/Entity.cs`. (Component exists in reconstruction: `Core/PoEMemory/Components/Buffs.cs`.) Null‑safe accessors `Life.GetBuffs()` / `Life.HasBuffSafe(string)` ship in `Core/Shared/Compat/ComponentCompat.cs`. |
| `Render.RotationNum` : Numerics `Vector3` | `Render.RotationNum()` extension | compat‑helper | `WAYG:348` `renderComp.RotationNum.X` → `renderComp.RotationNum().X`; `Core/Shared/Compat/NumericsCompat.cs` (also `Render.PosNum()` / `Render.BoundsNum()`). |
| `Render.Size` | — | missing | No `Size` on `Render` in either tree; fork exposes `Bounds`/`Height`. `Core/PoEMemory/Components/Render.cs`. |

### Components — items

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `Mods.ImplicitMods` : `List<ItemMod>` | `Mods.ImplicitMods()` extension | compat‑helper | `gcv/Ninja Price/Main/CustomItem.cs:196`. `Core/Shared/Compat/ComponentCompat.cs` walks `ModsStruct.implicitMods` with the same stride/guard as the fork's private `Mods.GetMods`; `Mods.ItemMods` remains the combined list. |
| `Mods.ExplicitMods` : `List<ItemMod>` | `Mods.ExplicitMods()` extension | compat‑helper | `gcv/.../CustomItem.cs:194`. Same helper family over `ModsStruct.explicitMods` (`Core/Shared/Compat/ComponentCompat.cs`). |
| `Mods.EnchantedMods` | — | missing | `stashie/ItemData.cs:109` `modsComp?.EnchantedMods?.Count`. Not in fork `Mods`. |
| `Mods.IncubatorName` | — | missing | No incubator member on fork `Mods`. |
| `Stack.MaxSize` | `Stack.MaxSize()` extension | compat‑helper | `Core/PoEMemory/Components/Stack.cs` exposes `Size` and `Info`; the helper (`Core/Shared/Compat/ComponentCompat.cs`) reads `Stack.Info.MaxStackSize` via `CurrencyInfo`, returning `0` when unavailable. |
| `SkillGem.SkillExperience` / `ExperienceMax` | `ExperienceMaxLevel`, `ExperiencePrevLevel`, `ExperienceToNextLevel` | renamed | `Core/PoEMemory/Components/SkillGem.cs:31‑34`. Names/semantics differ; map carefully. |

### Components — world & interactables

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `Chest.Rarity` | — | missing | `Core/PoEMemory/Components/Chest.cs` exposes `IsOpened/IsLocked/IsStrongbox/IsLarge/…`; no `Rarity`. Use `Entity.Rarity` / `ObjectMagicProperties.Rarity`. |
| `Transitionable.CurrentState` | `Flag1` / `Flag2` (`byte`) | renamed/signature‑diff | `Core/PoEMemory/Components/Transitionable.cs:9‑12`. Upstream `CurrentState`; here read `Flag1`. |
| `Positioned.WorldPosNum` : `Vector2` (Numerics) | `Positioned.WorldPosNum()` extension | compat‑helper | `Core/Shared/Compat/NumericsCompat.cs` (also `Positioned.GridPosNum()`), converting `Positioned.WorldPos` — `Core/PoEMemory/Components/Positioned.cs:40` (also `GridPos`/`GridPosI`/`GridPosition`). |

### IngameState / IngameData / ServerData

| Upstream path | This fork | Status | Notes |
| --- | --- | --- | --- |
| `IngameState.Data.ServerData` | `IngameState.ServerData` | renamed/moved | `IngameData` exposes **no** `ServerData` member in either tree. Fork: `Core/PoEMemory/MemoryObjects/IngameState.cs:70`. Plugin (old path): `Stashie/.../StashieSettingsHandler.cs:27`, `stashie/Stashie.cs:337`. |
| `IngameState.UIHoverElement` | `IngameState.UIHover` : `Element` | renamed/missing | Used by ~5 plugins. Both trees expose `UIHover` (+`UIHoverTooltip`/`UIHoverX`/`UIHoverY`); the upstream distribution adds `UIHoverElement`. Fork: `Core/PoEMemory/MemoryObjects/IngameState.cs:73`. |

### UI Elements

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `Element.PositionNum` : Numerics `Vector2` | `Element.PositionNum()` extension | compat‑helper | `Core/Shared/Compat/NumericsCompat.cs`, converting `Element.Position` (`Core/PoEMemory/Element.cs:61`). |
| `Element.TextColour` / `HighlightBackgroundColor` | — | missing | Base `Element` exposes no colour members in either tree; read `Text`, supply your own colour. |
| `Map.LargeMap.AsObject<SubMap>().MapCenter` / `.MapScale` / `.Zoom` | `Map.LargeMapShiftX/Y`, `LargeMapZoom` (floats) | missing | `Radar/Radar.cs:238‑242` casts to `SubMap`. Fork's `Map.LargeMap` is a plain `Element` and there is **no `SubMap` type**; `Core/PoEMemory/Elements/Map.cs`. (`SubMap` *is* in the reconstruction.) |

### Inventories & stash

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `InventoryIndex.PlayerExpandedInventory` | `InventoryIndex.PlayerInventory` | missing | `Stashie/.../FilterManager.cs:114`. Enum value absent in both trees; `Core/Shared/Enums/InventoryIndex.cs`. |
| `InventorySlotE.ExpandedMainInventory1` | `InventorySlotE.MainInventory1` (no expanded slot) | missing | `Stashie/.../StashieSettingsHandler.cs:31`. Fork enum lists 36 values, none "Expanded"; `Core/Shared/Enums/InventorySlotE.cs`. |
| `Inventory.NestedVisibleInventoryIndex` / `NestedTabSwitchBar` | `StashElement.IndexVisibleStash` / `VisibleStash` | renamed/missing | `FullRareSetManager/DropAllToInventory.cs:33`. Fork members in `Core/PoEMemory/Elements/StashElement.cs:54‑59`. |
| `InventorySlotE.Expanded*` (extra expanded slots) | — | missing | Whole expanded‑inventory slot family is upstream‑only. |

### Graphics, fonts & sound

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `FontAlign.VerticalCenter` (`Top`/`Bottom`) | `FontAlign` = `Left`/`Center`/`Right` | missing | `ProximityAlert/Proximity.cs:200` `FontAlign.Center \| FontAlign.VerticalCenter`. Fork enum has no vertical flags; `Core/Shared/Enums/FontAlign.cs`. |
| `SoundController.PlaySound(string file, float volume)` | `SoundController.PlaySound(string name)` | signature‑diff | `gcv/Ninja Price/Main/Render.cs:1116` passes a volume. Fork has the 1‑arg overload only; `Core/SoundController.cs:71`. Deliberately **not** given a compat helper: the only composable fork member, `SetVolume` (`SoundController.cs:138`), mutates the global **master** volume (upstream's parameter is per‑playback) and throws on an uninitialized controller. Call `SetVolume` + `PlaySound` yourself if global‑volume semantics are acceptable. |

### FilesInMemory (static data lookups)

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `BaseItemTypes.GetByAddress(long)` | `BaseItemTypes.GetFromAddress(long)` | renamed | `Core/PoEMemory/FilesInMemory/BaseItemTypes.cs:35`. |
| `BaseItemTypes.Translate(string metadata)` | same | present | Identical here; `…/BaseItemTypes.cs:43`, used by `FullRareSetManagerCore.cs:364`, `gcv/.../CustomItem.cs:119`. Listed so porters keep using it. |

### Memory (low‑level)

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `IMemory.ReadStdVector<T>(...)` / `Memory.ReadStdVector<T>` | `IMemory.ReadStdVector<T>` (instance + extensions) | present / compat‑helper | `Radar/Radar.Pathfinding.cs:202`. Fork now has instance overloads reading the vector header from a pointer (`Core/Shared/Interfaces/IMemory.cs:58,64`); `Core/Shared/Compat/MemoryCompat.cs` adds the upstream shapes over `NativePtrArray` / raw begin‑end bounds for unmanaged structs. For `RemoteMemoryObject` vectors / pointer vectors use `ReadStructsArray<T>` / `ReadNativeArray<T>` (`Core/Memory.cs:138`, `:531`). |

### Settings / Nodes / Attributes

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `ContentNode<T>` / `IContentNodeBase` | — | missing | List‑of‑sub‑settings node; not in fork. (`Core/Shared/Nodes/ContentNode.cs` exists in reconstruction.) |
| `CustomNode`, `HotkeyNodeV2` | `HotkeyNode` | missing | Fork has the v1 `HotkeyNode` only. |
| `[Submenu]` (`SubmenuAttribute`) | — | missing | Use `[Menu]` nesting; `Core/Shared/Attributes/`. |
| `[ConditionalDisplay]` (`ConditionalDisplayAttribute`) | — | missing | No conditional‑visibility attribute here. |

### Utilities / extensions

| Upstream member | This fork | Status | Notes |
| --- | --- | --- | --- |
| `DictionaryExtensions.GetValueOrDefault` (engine helper) | — (use BCL `Dictionary.GetValueOrDefault`) | missing | `gcv/.../CustomItem.cs:188,200` — those resolve to the BCL / `ItemFilterLibrary`, not an engine extension here. No `GetValueOrDefault` defined in `Core/`. |

---

## 4. New subsystems / file groups (compiled build only)

Beyond the per‑category lists in §2, these are the largest whole groups a porter will find
referenced upstream but missing here:

- **Stat‑description translation pipeline** — `StatDescription*`, `CachedStatDescription*`,
  `StatHandling`, `StatTranslationUtils`, `ModTranslationReplacerInput`,
  `StatTranslator.AddStat` (human‑readable mod text generation).
- **Heist** — `Heist*` components, `Heist/` records, `HeistJobE` enum.
- **Expedition** — detonator + Tujen/vendor haggle elements, `ExpeditionAreaData`,
  `ExpeditionSaga`.
- **Harvest** — `Harvest*` components, `HarvestWindow`/`HarvestCraftElement`, `HarvestSeed` Dat.
- **Sanctum** — floor/room/reward elements + `Sanctum/` Dat tables.
- **Ultimatum** — panel/choice elements + `UltimatumModifier`/`UltimatumItemisedReward` Dats.
- **Necropolis** — monster panels, corpses, `NecropolisPack*`/`NecropolisCraftingMod` Dats.
- **Village / Currency Exchange** — ~38 element + Dat files (recruitment, shipments, worker
  management, currency‑exchange panel & order book).
- **Azmeri, Ancestor (ToTA), Archnemesis, Bestiary, Delve, Labyrinth, Atlas Primordial/
  Influence, Sentinel, Party/Social** — see §2 for the concrete files.
- **FilesInMemory** broadly grew from this fork's set to **90 additional** static‑data
  wrappers (gems/skills, uniques, visuals, quests, all the mechanic tables above).

---

## 5. Caveats

- **Reconstruction stubs.** ~17% of method bodies in the ground‑truth tree are
  `NotImplementedException` stubs (signatures faithful, behavior not). Treat signatures as
  authoritative and bodies as advisory. See `docs/ported-from-dll.md`.
- **Offsets are build‑specific.** Any hard‑coded address/offset is bound to a PoE build and is
  not portable as a literal.
- **Member rows are evidence‑based, not exhaustive.** §3 lists the high‑value divergences a
  porter actually trips over (seeded from `docs/api/*.md` and reference‑plugin call sites).
  ExileApi‑Compiled exposes additional members on the absent *types* in §2 that aren't
  enumerated member‑by‑member here.
- **This is a porting aid, not a compatibility guarantee.** When a member is "missing", the
  fix is usually to (a) use the named in‑repo equivalent, (b) convert SharpDX↔Numerics at the
  boundary, or (c) accept that the feature/subsystem isn't available in this fork.

---

## Source

Compared trees (commits):

- This fork: `master` @ `37b6a20` — `Core/` (309 `.cs`, 53 components).
- Reconstruction: `origin/claude/ecstatic-ritchie-cje3s8` @ `9dde92e` (PR #18) — `Core/`
  (698 `.cs`, 92 components, 428 types); shared merge‑base with `master` = `b5a9038`.
- `origin/claude/ecstatic-ritchie-cje3s8:docs/ported-from-dll.md` — reconstruction method
  and caveats.

Type‑level gaps from `git ls-tree -r --name-only` diffs of `Core/`. Member‑level rows
verified against both trees (`git show <ref>:<path>`) and the reference plugins
(`WAYG`, `Stashie`, `FullRareSetManager`, `Radar`, `ProximityAlert`, `gcv/Ninja Price`).
