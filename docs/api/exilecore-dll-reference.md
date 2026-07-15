# ExileCore.dll — compiled engine reference

A complete, organized inventory of the **public surface of the compiled `ExileCore.dll`**
(the ExileApi-Compiled engine) for plugin authors. Emphasis is on **functions/methods and
members**: this is the full engine API, not just the slim modernized fork in this repo.

See also: [API reference index](README.md) ·
[compatibility-exileapi-compiled.md](compatibility-exileapi-compiled.md)

---

## 1. Intro & provenance

**ExileCore** is a Windows-only .NET HUD/plugin framework for Path of Exile. It runs as a
separate process, reads the live game's memory read-only, interprets it as a tree of typed
objects (entities, components, UI elements, static data files), and draws an overlay on top
of the game window via DirectX 11. Plugins consume that parsed data and render or act on it.
The unit of consumption is `ExileCore.dll` — it contains `Core`, `GameController`,
`Graphics`, `Input`, the plugin host, and the entire `ExileCore.PoEMemory.*` /
`ExileCore.Shared.*` object model.

**This breakdown is reconstructed, not hand-written.** The actual binary is **not shipped in
this repo**, and there is no .NET/IL tooling in this environment. The compiled
`ExileCore.dll` — assembly version **10.0.14.7603**, target **net10** — was decompiled with
**ICSharpCode.Decompiler (ILSpy master, built for net10)** in branch
`origin/claude/ecstatic-ritchie-cje3s8` (PR #18). That committed reconstruction **is** the
disassembly of the DLL, and every type name, field, property, **method signature** and enum
value quoted below is pulled directly from those reconstructed source files (via
`git show`), not from memory.

Caveats — read these before trusting any *behavior*:

- The DLL's **metadata is intact**, so type definitions, members, signatures and enums are
  **faithful**.
- The DLL is **hardened**: ~17% of method *bodies* are protected (invalid-at-rest IL,
  restored at runtime by the module initializer). Those bodies are **not present in static
  IL** and cannot be decompiled by any tool. They were replaced with
  `throw new NotImplementedException("Body protected in source DLL")`. **Signatures are
  reliable; stubbed bodies are not behaviorally guaranteed.**
- A small set of types could only be recovered as **signature-only stubs** (no member
  logic) — listed and flagged below.
- **Offsets are build-specific.** Any hard-coded `Address + 0x…` literal in a member is
  valid only for this exact game build; treat them as illustrative.

Status legend used throughout: ✅ fully recovered · ◑ partially stubbed (structure + most
logic real, protected methods stubbed) · ○ signature-only stub.

---

## 2. Assembly overview

| | |
|---|---|
| Assembly | `ExileCore`, version **10.0.14.7603** |
| Target framework | **net10** (`net10.0-windows`, WinForms + DX11) |
| Reconstructed types | **389** added to close the gap to the full engine surface |
| Reconstructed `.cs` files (Core/) | **698** (many parents split into `partial` files) |

### Namespaces / categories (file counts in the reconstruction)

| Area | Namespace | Files |
|---|---|---|
| Engine root | `ExileCore` | 60 |
| Memory objects | `ExileCore.PoEMemory.MemoryObjects` (+ Ancestor/Heist/Metamorph) | 95 |
| UI elements | `ExileCore.PoEMemory.Elements` (+ Village/Sanctum/Expedition/Necropolis/Atlas/Inventory) | 92 + subdirs |
| Components | `ExileCore.PoEMemory.Components` | 92 |
| Static data files | `ExileCore.PoEMemory.FilesInMemory` (+ Atlas/Ancestor/Sanctum/Village/Labyrinth/Metamorph/Harvest/Ultimatum/Archnemesis) | 83 + subdirs |
| Memory base | `ExileCore.PoEMemory` | 11 |
| Models | `ExileCore.PoEMemory.Models` | 4 |
| Enums | `ExileCore.Shared.Enums` | 48 |
| Shared infra | `ExileCore.Shared` (+ Nodes/Cache/Helpers/Interfaces/Attributes/SomeMagic/PInvoke/AtlasHelper/Static) | 32 + subdirs |
| Renderer | `ExileCore.RenderQ` | 7 |

### Real vs. stubbed status (from `docs/ported-from-dll.md`)

| Status | Count | Meaning |
|---|---|---|
| ✅ Fully recovered | **128** | every member decompiled cleanly |
| ◑ Partially stubbed | **236** | structure + most logic real; protected methods stubbed |
| ○ Signature-only | **25** | only signatures recoverable |

Total protected method bodies stubbed: **610** (the per-marker count `Body protected in
source DLL` reads **531** across **208** files in the tree; `signature-only stub` markers
appear in **24** files — the doc's 25 counts the nested `ImguiVariadic`/`PluginCompiler`
pair separately). 20 missing nested types were restored as `partial` extension files.

Find remaining work — note these greps target the **reconstruction branch**
(`origin/claude/ecstatic-ritchie-cje3s8`, PR #18), not this repo's `Core/`, which contains
no such stubs (if the ref is absent locally, fetch it first:
`git fetch origin claude/ecstatic-ritchie-cje3s8`):

```
git grep -n  "Body protected in source DLL" origin/claude/ecstatic-ritchie-cje3s8 -- Core/   # stubbed individual methods
git grep -ln "signature-only stub"          origin/claude/ecstatic-ritchie-cje3s8 -- Core/   # types recovered as signatures only
```

**○ Signature-only types (need the most attention):**
`ActionOverlay`, `BackgroundTask`, `ControllerInput`, `DelegateCompiler`, `IInputManager`,
`ImGuiHelpers`, `Limits`, `PagedMemoryBackend`,
`PoEMemory.FilesInMemory.GemEffects`, `PoEMemory.MemoryObjects.EnvironmentSettingValue`,
`PoEMemory.MemoryObjects.TypedEnvironmentData`, `PoEMemory.StructuredRemoteMemoryObject`,
`ProcessPicker`, `Shared.NextFrameTask`, `Shared.Nodes.ContentNode`,
`Shared.Nodes.ContentNodeConverter`, `Shared.Nodes.HotkeyNodeV2`,
`Shared.PluginAssemblyLoadContext`, `Shared.PluginCompiler`, `Shared.SyncAwaiter`,
`Shared.SyncTaskMethodBuilder`, `Shared.TaskUtils`, `Shared.WaitFunctionTimed`,
`SnapshotBuilder`, `StatCollector`.

> External-reference gaps in the reconstruction: `Shared/PluginCompiler.cs` +
> `Shared/MsBuildLogger.cs` reference `Microsoft.Build.*` (upstream compiled plugins via
> MSBuild — superseded in this fork by `RoslynCompiler`); `ImguiVariadic.cs` references
> `SharpGen.Runtime`.

---

## 3. Type inventory by area

### 3.1 Root `ExileCore`

Engine bootstrap, the plugin-facing facades, settings, and host plumbing.

| Type | Status | Role |
|---|---|---|
| `Core` | ◑ | Engine entry; owns the render loop, `Graphics`, runners, `FindPoe()` |
| `GameController` | ✅ | The plugin's window into game state (also `PluginBridge`) |
| `Graphics` | ✅ | Drawing facade (text, lines, boxes, frames, images) |
| `Input` | ✅ | Keyboard/mouse send + key-state polling |
| `BaseSettingsPlugin<TSettings>` | ✅ | Base class every plugin derives from |
| `AreaController` | ✅ | Area-change tracking (`OnAreaChange`, `RefreshState`) |
| `AreaInstance` | ◑ | Parsed current-area data |
| `EntityListWrapper` | ◑ | Entity collection, add/remove events, type buckets |
| `EntityCacheContainer` | ◑ | Entity cache backing store |
| `Memory` (+ `.BooleanHolder`, `.EmptyDisposable`, `.PackedFlagStore`) | ◑ | `IMemory` impl — process reads, pattern scans |
| `DefaultMemoryBackend`, `IMemoryBackend`, `PagedMemoryBackend` ○, `SnapshotMemoryBackend`, `SnapshotBuilder` ○, `SnapshotSettings` | ◑/○ | Memory backends + snapshot capture |
| `GameWindow` (`GameWIndow.cs`) | ◑ | Target-window geometry/foreground |
| `MultiThreadManager` | ◑ | Worker-thread pool for `Job`s |
| `SoundController` | ◑ | Plugin sound playback |
| `SettingsContainer`, `SettingsParser` | ◑ | Settings load/save/reflection |
| `CoreSettings` + `CoreDebugSettings`/`CoreFontSettings`/`CoreLoggingSettings`/`CorePerformanceSettings`/`CorePluginSettings` | ◑ | Engine settings tree |
| `MenuWindow` (+ `.EDebugKind`, `.MainDebugTableRecord`), `DebugWindow`, `PluginPanel` | ◑ | Built-in menu / debug overlay |
| `Logger`, `DebugMessage` | ◑ | Logging |
| `DefaultInputManager`, `IInputManager` ○, `ControllerInput` ○ | ◑/○ | Input abstraction layer |
| `ActionOverlay` ○, `BackgroundTask` ○, `Limits` ○, `StatCollector` ○, `DelegateCompiler` ○, `ProcessPicker` ○ | ○ | Misc host services |
| `CommandExecutor`, `NamedPipeHandler`, `DataExporter`, `Time`, `DisposableAction`, `IStatusDisposable`, `FuncAttachment`, `MouseMoveStroke`/`MouseMoveStrokePoint`, `SortedListExtensions`, `WorldPositionExtensions` | ◑/✅ | Utilities |

**`Core`** (◑) — selected members:
```csharp
public Core(RenderForm form);
public Graphics Graphics { get; }
public static ILogger Logger { get; set; }
public static Runner MainRunner { get; set; }
public static Runner ParallelRunner { get; set; }
public Runner CoroutineRunner { get; set; }
public Runner CoroutineRunnerParallel { get; set; }
public double TargetPcFrameTime { get; set; }
public static ObservableCollection<DebugInformation> DebugInformations { get; }
public static Memory FindPoe();   // attaches to the live PoE process
public void Tick();
public void Render();
public void FixImGui();
public void Dispose();
```

**`GameController`** (✅) — the most-used plugin object:
```csharp
public TheGame Game { get; }
public AreaController Area { get; }
public GameWindow Window { get; }
public IngameState IngameState => Game.IngameState;
public FilesContainer Files { get; }
public Entity Player => EntityListWrapper.Player;
public ICollection<Entity> Entities => EntityListWrapper.Entities;
public IMemory Memory { get; }
public EntityListWrapper EntityListWrapper { get; }
public MultiThreadManager MultiThreadManager { get; }
public SettingsContainer Settings { get; }
public SoundController SoundController { get; }
public Cache Cache { get; set; }
public PluginBridge PluginBridge;          // GetMethod<T>(name) / SaveMethod(name, method)
public PluginPanel LeftPanel { get; }
public PluginPanel UnderPanel { get; }
public bool IsForeGroundCache { get; set; }
public bool Initialized { get; }
public long ElapsedMs => sw.ElapsedMilliseconds;
public double DeltaTime => debDeltaTime.Tick;
public Dictionary<string, object> Debug { get; }
public Vector2 GetLeftCornerMap();
public void Tick();
public static event Action<bool> eIsForegroundChanged;
```
Nested **`PluginBridge`** (✅): `T GetMethod<T>(string name) where T : class;` ·
`void SaveMethod(string name, object method);` — the cross-plugin RPC channel.

**`BaseSettingsPlugin<TSettings>`** (✅, `where TSettings : ISettings, new()`) — the plugin
contract:
```csharp
public TSettings Settings { get; }
public List<ISettingsHolder> Drawers { get; }
public string Name { get; set; }
public string InternalName { get; }
public string DirectoryName { get; set; }
public string DirectoryFullName { get; set; }
public int Order { get; protected set; }
public bool Initialized { get; set; }
public bool CanUseMultiThreading { get; protected set; }
// lifecycle / per-frame hooks:
public virtual bool Initialise();
public virtual void OnLoad();
public virtual void OnUnload();
public virtual Job  Tick();                       // logic; may return an off-thread Job
public virtual void Render();                      // drawing
public virtual void AreaChange(AreaInstance area);
public virtual void EntityAdded(Entity entity);
public virtual void EntityAddedAny(Entity entity);
public virtual void EntityRemoved(Entity entity);
public virtual void EntityIgnored(Entity entity);
public virtual void DrawSettings();
public virtual void OnClose();
public virtual void OnPluginSelectedInMenu();
public virtual void OnPluginDestroyForHotReload();
public virtual void ReceiveEvent(string eventId, object args);
public void PublishEvent(string eventId, object args);
public void SetApi(GameController gameController, Graphics graphics, PluginManager pluginManager);
public AtlasTexture GetAtlasTexture(string textureName);
public void LogMessage(string msg, float time, Color clr);
public void LogError(string msg, float time = 1f);
public void _LoadSettings();  public void _SaveSettings();
```

**`Graphics`** (✅) — drawing facade over DX11:
```csharp
public DX11 LowLevel { get; }
public FontContainer Font { get; }
public FontContainer LastFont { get; }
public bool TransparentState { get; }
// text (many overloads — by color/font/height/align):
public Vector2N DrawText(string text, Vector2 position, Color color, string fontName = null, FontAlign align = FontAlign.Left);
public Vector2N DrawText(string text, Vector2N position, Color color, int height, FontAlign align = FontAlign.Left);
public Vector2N MeasureText(string text);
public Vector2N MeasureText(string text, int height);
// primitives:
public void DrawLine(Vector2N p1, Vector2N p2, float borderWidth, Color color);
public void DrawBox(Vector2N p1, Vector2N p2, Color color, float rounding = 0);
public void DrawBox(RectangleF rect, Color color, float rounding);
public void DrawFrame(Vector2N p1, Vector2N p2, Color color, float rounding, int thickness, int flags);
public void DrawFrame(RectangleF rect, Color color, int thickness);
// images / textures:
public void DrawImage(string fileName, RectangleF rectangle, Color color);
public void DrawImage(string fileName, RectangleF rectangle, RectangleF uv, Color color);
public void DrawImage(AtlasTexture atlasTexture, RectangleF rectangle, Color color);
public void DrawImageGui(string fileName, Vector2N TopLeft, Vector2N BottomRight, Vector2N TopLeft_UV, Vector2N BottomRight_UV);
public bool InitImage(string name, bool textures = true);
public void DisposeTexture(string name);
```
Nested `Graphics.SetTextScaleDisposable` (◑) — scoped text-scale guard.

**`Input`** (✅, all static):
```csharp
public static bool IsKeyDown(int nVirtKey);   public static bool IsKeyDown(Keys nVirtKey);
public static bool GetKeyState(Keys key);
public static void RegisterKey(Keys key);      public static event EventHandler<Keys> ReleaseKey;
public static void Update(IntPtr windowPtr);
public static void SetCursorPos(Vector2 vec);  public static IEnumerator SetCursorPositionSmooth(Vector2 vec);
public static void Click(MouseButtons buttons);
public static void LeftDown();  public static void LeftUp();  public static void MouseMove();
public static void VerticalScroll(bool forward, int clicks);
public static IEnumerator KeyPress(Keys key);
public static void KeyDown(Keys key[, IntPtr handle]);  public static void KeyUp(Keys key[, IntPtr handle]);
public static void KeyPressRelease(Keys key[, IntPtr handle]);
```

**`EntityListWrapper`** (◑):
```csharp
public ICollection<Entity> Entities => entityCache.Values;
public uint EntitiesVersion { get; }
public List<Entity> OnlyValidEntities { get; }
public List<Entity> NotOnlyValidEntities { get; }
public Dictionary<EntityType, List<Entity>> ValidEntitiesByType { get; }
public event Action<Entity> EntityAdded;   // also EntityAddedAny / EntityRemoved / EntityIgnored
public event EventHandler<Entity> PlayerUpdate;
public static Entity GetEntityById(uint id);
public string GetLabelForEntity(Entity entity);
public void StartWork();   public void RefreshState();
```
(Companion config struct `EntityCollectSettingsContainer` carries the parallel-collection
thresholds and the multithread manager.)

### 3.2 `ExileCore.PoEMemory` (base layer)

| Type | Status | Role |
|---|---|---|
| `RemoteMemoryObject` | ✅ | Base for every memory-backed object; `Address`, `M`, `GetObject<T>`, `ReadObject<T>` |
| `Element` | ✅ | Base UI element node (tree, rects, visibility, text, children) |
| `Component` | ✅ | Base entity component; `Owner`, `OwnerAddress`, `DumpObject()` |
| `FilesContainer` | ◑ | Static `.dat` registry + typed accessors (`Mods`, `Stats`, `WorldAreas`, `AtlasNodes`, …) |
| `FileInMemory` / `FilesFromMemory` (`FileInformation`) | ✅ | Base file wrapper + raw file table reader |
| `Offsets`, `Pattern`, `StringPattern`, `ElementType` | ◑ | Offset table, pattern scan, element kind enum |
| `StructuredRemoteMemoryObject` | ○ | Struct-backed RMO base |

**`RemoteMemoryObject`** (✅) — the universal read API every object inherits:
```csharp
public long Address { get; set; }
public IMemory M => pM;
public TheGame TheGame => pTheGame;
public static Cache Cache => pCache;
public T ReadObjectAt<T>(int offset)   where T : RemoteMemoryObject, new();
public T ReadObject<T>(long addressPointer) where T : RemoteMemoryObject, new();
public T GetObjectAt<T>(int offset)    where T : RemoteMemoryObject, new();
public T GetObjectAt<T>(long offset)   where T : RemoteMemoryObject, new();
public T GetObject<T>(long address)    where T : RemoteMemoryObject, new();
public T GetObject<T>(IntPtr address)  where T : RemoteMemoryObject, new();
public T AsObject<T>()                 where T : RemoteMemoryObject, new();
```

**`Element`** (✅) — selected members:
```csharp
public bool IsValid => Elem.SelfPointer == Address;
public bool IsVisible { get; }   public bool IsVisibleLocal { get; }
public Element Parent { get; }   public Element Root { get; }
public IList<Element> Children => GetChildren<Element>();
public long ChildCount { get; }
public Vector2 Position { get; }  public float X/Y/Width/Height/Scale { get; }
public virtual string Text { get; }
public RectangleF GetClientRectCache { get; }
public virtual RectangleF GetClientRect();
public Vector2 GetParentPos();
public Element this[int index] { get; }
public Element GetChildAtIndex(int index);
public Element GetChildFromIndices(params int[] indices);
public List<T> GetChildrenAs<T>() where T : Element, new();
```

### 3.3 `ExileCore.PoEMemory.Components` (all 92)

```
ActiveAnimationData · Actor · Animated · AnimatedRender · AnimationController ·
AnimationStage · AnimationStageList · AreaTransition · Armour · ArmourStatRange ·
AttachedAnimatedObject · AttachedAnimatedObjectAttachment · AttributeRequirements · Base ·
Beam · BlightTower · BrequelFruit · Buff · Buffs · CapturedMonster · Charges · Chest ·
ClientAnimationController · ClientBetrayalChoice · CurrencyInfo · DelveLight ·
DeployedObject · DiesAfterTime · EffectPack · ExpeditionSaga · Flask · GroundEffect ·
HarvestInfrastructure · HarvestInfrastructureMod · HarvestInfrastructureModUnmanaged ·
HarvestSeedSpawnDescriptor · HarvestWorldObject · HeistBlueprint · HeistContract ·
HeistEquipment · HeistRewardDisplay · HideoutDoodad · Inventories · InventoryVisual ·
ItemInfoData · Life · LocalStats · Magnetic · Map · MapKey · MinimapIcon · Mods
(+ Mods.StatWithValue) · Monolith · Monster · Movement · NPC · NecropolisCorpse ·
ObjectMagicProperties · ParticleEffects · Pathfinding · Player · Portal · Positioned ·
Preload · Prophecy · Quality · Render · RenderItem · Sector · SentinelDrone · Shield ·
Shrine · SkillGem · Sockets (+ Sockets.Socket) · SoundEvents · Stack · StateMachine ·
StateMachineState · Stats · SupportedAnimationList · Targetable · TimerComponent ·
Tincture · Transitionable · TriggerableBlockage · UltimatumTrial · Usable · Weapon ·
WorldDescription · WorldItem
```

Key components expanded:

**`Life`** (✅): `MaxHP`/`CurHP`/`ReservedFlatHP`/`ReservedPercentHP`/`HPRegen`,
`MaxMana`/`CurMana`/`ReservedFlatMana`/`ReservedPercentMana`/`ManaRegen`, `MaxES`/`CurES`;
computed `HPPercentage`/`MPPercentage`/`ESPercentage`; `List<Buff> Buffs`;
`bool HasBuff(string buff)`.

**`Actor`** (◑): `short ActionId`; `ActionFlags Action`; `bool isMoving`/`isAttacking`;
`int AnimationId`; `AnimationE Animation`; `ActionWrapper CurrentAction`;
`List<ActorSkill> ActorSkills`; `List<DeployedObject> DeployedObjects`;
`long DeployedObjectsCount`. Nested `Actor.ActionWrapper` (◑): `Destination`/
`CastDestination`, `Entity Target`, `ActorSkill Skill`.

**`Mods`** (◑): `string UniqueName`; `bool Identified`; `ItemRarity ItemRarity`;
`List<ItemMod> ItemMods`; `int ItemLevel`/`RequiredLevel`; `bool IsMirrored`/`Synthesised`/
`HaveFractured`; `int CountFractured`; `ItemStats ItemStats`; `List<string> HumanStats`/
`HumanCraftedStats`/`HumanImpStats`/`FracturedStats`. Nested `Mods.StatWithValue`.

**`Sockets`** (◑): `int LargestLinkSize`; `int NumberOfSockets`; `bool IsRGB`;
`List<int[]> Links`; `List<int> SocketList`; `List<string> SocketGroup`;
`List<SocketedGem> SocketedGems`. Nested `Sockets.Socket` and `Sockets.SocketedGem`
(`Entity GemEntity`, `int SocketIndex`).

**`Stats`** (◑): `long StatsCount`; `Dictionary<GameStat,int> ParseStats()`;
`Dictionary<string,int> HumanStats()`.

**`Buffs`** (◑): `List<Buff> ParseBuffs()`; `bool HasBuff(string buff)`;
`bool TryGetBuff(string name, out Buff buff)`.

**`Render`** (◑): `Vector3 Pos`/`Rotation`/`Bounds`; `float X`/`Y`/`Z`/`Height`/
`TerrainHeight`; `Vector3 InteractCenter`; `string Name`.

**`Positioned`** (◑): `int GridX`/`GridY`; `Vector2 GridPos`/`WorldPos`/`GridPosition`;
`float Rotation`/`RotationDeg`/`WorldX`/`WorldY`; `byte Reaction`.

**`Player`** (◑): `string PlayerName`; `uint XP`; `int Strength`/`Dexterity`/
`Intelligence`/`Level`; `HideoutWrapper Hideout`; `PantheonGod PantheonMajor`/
`PantheonMinor`; `IList<ProphecyDat> Prophecies`; `bool IsTrialCompleted(...)` (3 overloads);
`IList<TrialState> TrialStates`.

**`Base`** (◑): `int ItemCellsSizeX`/`ItemCellsSizeY`; `bool isCorrupted`/`isShaper`/
`isElder`.

### 3.4 `ExileCore.PoEMemory.MemoryObjects` (all 95)

```
ActiveSkillWrapper · ActorSkill · ActorSkillCooldown · ActorVaalSkill ·
AncestorFightOption · AncestorFightOptionReward · AncestorServerData · AreaTemplate ·
AtlasInfluencedMap · AtlasInfluencedRegion · AtlasMissionType · AtlasNode ·
BestiaryCapturableMonster · BestiaryFamily · BestiaryGenus · BestiaryGroup ·
BestiaryRecipe · BestiaryRecipeComponent · BetrayalChoiceAction · BetrayalData ·
BetrayalDialogue · BetrayalRelationshipStatus · BetrayalSyndicateState ·
Camera (+ Camera.CameraSnapshot) · CardTradeWindow · CraftBenchWindow · Cursor ·
DefaultEnvironmentSetting · DeployedObject · DiagnosticElement · EffectEnvironment ·
Entity · EntityEffect · EntityList · EnvironmentData · EnvironmentDataEnvironment ·
EnvironmentSettingValue ○ · EscapeState · ExpeditionAreaData · GameConfig ·
GameConfigKeyValue · GameConfigSection · GameStateContoller · GameStateTypes · GameUi ·
GemLevelUpElement · GemLvlUpPanel · GrantedEffectsPerLevel · HideoutWrapper ·
HoverItemState · IngameData (+ IngameData.TileIndexStruct) · IngameState ·
IngameUIElements (+ IngameUIElements.QuestListNode) · Inventory · InventoryHolder ·
InventoryList · ItemMod · LabyrinthData · LoginState · MapDeviceCraftingSelectorElement ·
MapDevicePrimordialChoiceElement · MapDeviceWindow · MapStashTabElement ·
MechanicHandler · MercenaryEncounterWindow · MonsterVariety · NativeStringReader ·
PartyPlayerInfo · PartyPlayerInfoType · PlacedCurrencyExchangeOrder · PlayerInventory ·
ProphecyDat · Quest · QuestRewardWindow · QuestState · RemotePlayerInfo · SellWindow ·
SellWindowHideout · SentinelData · SentinelState · ServerData (+ ServerData.QuestFlagStore) ·
ServerDataMinimapIcon · ServerInventory · ServerPlayerData · ServerStashTab ·
SkillCooldown · SkillGemWrapper · TradeWindow · TypedEnvironmentData ○ · VendorInventory ·
WorldArea
```
Plus subfolders: **Ancestor/** (6: AncestorFightSelectionWindow,
AncestorFightSelectionOpponentLine, AncestorMainShopWindow, AncestorMainShopWindowOption,
AncestorSidePanelOption, AncestorSideShopPanel), **Heist/** (4: HeistChestRecord,
HeistChestRewardTypeRecord, HeistJobRecord, HeistNpcRecord), **Metamorph/** (1:
MetamorphWindowElement).

**`Entity`** (◑) — the central object model node:
```csharp
public static Entity Player { get; set; }
public uint Id { get; }   public uint InventoryId { get; }   public uint Version { get; set; }
public bool IsValid { get; set; }   public bool IsAlive { get; }   public bool IsDead { get; }
public bool IsHidden { get; }   public bool IsHostile { get; }   public bool IsTargetable { get; }
public bool IsOpened { get; }
public Vector3 Pos { get; }   public Vector3 BoundsCenterPos { get; }   public Vector2 GridPos { get; }
public float DistancePlayer { get; }   public float Distance(Entity entity);
public string Path { get; }   public string Metadata { get; }   public string RenderName { get; }
public MonsterRarity Rarity { get; }
public Dictionary<GameStat,int> Stats { get; }   public List<Buff> Buffs { get; }
public long ComponentList { get; }   public Dictionary<string,long> CacheComp { get; }
public bool HasComponent<T>()            where T : Component, new();
public T    GetComponent<T>()            where T : Component, new();
public T    GetComponentFromMemory<T>()  where T : Component, new();
public bool CheckComponentForValid<T>()  where T : Component, new();
public T    GetHudComponent<T>()         where T : class;       // plugin-attached HUD data
public void SetHudComponent<T>(T data);
public void UpdatePointer(long newAddress);   public bool Check(uint entityId);
public event EventHandler<Entity> OnUpdate;
```

**`Camera`** (◑) — world→screen projection:
```csharp
public int Width { get; }   public int Height { get; }   public Vector2 Size { get; }
public float ZFar { get; }   public Vector3 Position { get; }
public Vector2 WorldToScreen(Vector3 vec);   // 4x4 matrix transform, divide-by-W
```
Nested `Camera.CameraSnapshot` (◑).

**`IngameState`** (✅) — root of the live UI/state tree:
```csharp
public Camera Camera { get; }   public IngameData Data { get; }   public ServerData ServerData { get; }
public bool InGame => ServerData.IsInGame;
public IngameUIElements IngameUi { get; }
public Element UIRoot { get; }   public Element UIHover { get; }   public Element UIHoverTooltip { get; }
public float UIHoverX/UIHoverY { get; }   public float CurentUElementPosX/Y { get; }
public DiagnosticElement LatencyRectangle/FrameTimeRectangle/FPSRectangle { get; }
public float CurLatency/CurFrameTime/CurFps { get; }
public TimeSpan TimeInGame { get; }   public float TimeInGameF { get; }
public void UpdateData();
```

**`ServerData`** (◑) — non-rendered server-side state:
```csharp
public NetworkStateE NetworkState { get; }   public bool IsInGame { get; }   public int Latency { get; }
public string League { get; }   public string Guild { get; }
public CharacterClass PlayerClass { get; }   public int CharacterLevel { get; }   public byte MonsterLevel { get; }
public byte MonstersRemaining { get; }
public int CurrentAzuriteAmount { get; }   public ushort CurrentSulphiteAmount { get; }
public int PassiveRefundPointsLeft/FreePassiveSkillPointsLeft/QuestPassiveSkillPoints { get; }
public int TotalAscendencyPoints/SpentAscendencyPoints { get; }
public PartyAllocation PartyAllocationType { get; }   public PartyStatus PartyStatusType { get; }
public IList<Player>  NearestPlayers { get; }
public IList<ushort>  SkillBarIds { get; }   public IList<ushort> PassiveSkillIds { get; }
public IList<ServerStashTab> PlayerStashTabs/GuildStashTabs { get; }
public IList<InventoryHolder> PlayerInventories/NPCInventories/GuildInventories { get; }
public ServerInventory GetPlayerInventoryBySlot(InventorySlotE slot);
public ServerInventory GetPlayerInventoryByType(InventoryTypeE type);
public ServerInventory GetPlayerInventoryBySlotAndType(InventoryTypeE type, InventorySlotE slot);
public IList<WorldArea> CompletedAreas/BonusCompletedAreas { get; }
public int GetBeastCapturedAmount(BestiaryCapturableMonster monster);
public byte GetAtlasRegionUpgradesByRegion(int regionId | AtlasRegion region);
public BetrayalData BetrayalData { get; }
```

### 3.5 `ExileCore.PoEMemory.Elements` (all 92 + subfolders)

Top-level UI elements:
```
AltarEntity · ArchnemesisAltarElement · ArchnemesisAltarInventorySlot ·
ArchnemesisInventorySlot · ArchnemesisPanelElement · AsyncItemRightClickPriceMenu ·
AtlasPanel · Atlasbonus · AzmeriData · AzmeriElement · BanditDialog · BanditType ·
BestiaryTab · BlightTowerUpgradeButton · CapturedBeast · CapturedBeastsTab ·
ChallengePanel · ChallengePanelTabContainer · ChallengePanelTabContainerTabInfo ·
ChatPanel · DelveElement · DivineFontPanel · DropdownElement · DropdownElementOption ·
EntityLabel · ExpeditionDetonator · ExpeditionDetonatorInfo · HPbarElement ·
HarvestCraftElement · HarvestWindow · HoverItemIcon · IItemRightClickPriceMenu ·
IncursionWindow · InstanceManagerPanel · InventoryElement · InvitesPanel · InvitesPanelItem ·
InvitesPanelItemKind · ItemOnGroundTooltip · ItemRightClickPriceMenu ·
ItemsOnGroundLabelElement (+ .VisibleGroundItemDescription) · KalandraTabletWindow ·
LabelOnGround · LeagueMechanicButtonsElement · Map · MapReceptacleWindow ·
MapStashTabElementQ · NpcDialog · NpcLine · PartyElement · PartyElementPlayerElement ·
PartyElementPlayerInfo · PartyElementPlayerInfoWrapper · PartyInvite · PartyTabElement ·
PoeChatElement · PopUpWindow · PurchaseWindow · ResurrectPanel · RitualWindow ·
SearchBarElement · SentinelPanel · SentinelSubPanel · ShortcutSettings · SkillBarElement ·
SkillElement · SocialElement · SocialPartyMember · SocialTabTypes · StashElement ·
StashTabContainer · StashTabContainerInventory · StashTabElement · StashTopTabSwitcher ·
SubMap · SubterraneanChart · SyndicatePanel · TabletChoiceElement · TabletTileElement ·
TooltipItemFrameElement · TrappedStashWindow · TreePanel · TreePassiveElement ·
UltimatumChoiceElement · UltimatumChoicePanel · UltimatumPanel · VendorStashTabContainer ·
VendorStashTabContainerInventory · VillageRewardWindow · WindowState · WorldMapElement
```
- **InventoryElements/** (12): `NormalInventoryItem`, `MapStashTabElement`,
  `BlightInventoryItem`, `CurrencyInventoryItem`, `DeliriumInventoryItem`,
  `DelveInventoryItem`, `DivinationInventoryItem`, `EssenceInventoryItem`,
  `FlaskInventoryItem`, `FragmentInventoryItem`, `GemInventoryItem`,
  `MetamorphInventoryItem`.
- **AtlasElements/** (3): `AtlasMasterMissionPanelElement`, `MasterMissionColour`,
  `VoidStoneSlot`.
- **ExpeditionElements/** (4): `ArtifactSliderElement`,
  `ExpeditionVendorCurrencyInfoElement`, `ExpeditionVendorElement`,
  `TujenHaggleWindowElement`.
- **Necropolis/** (3): `NecropolisCollectableCorpse`, `NecropolisMonsterPanel`,
  `NecropolisMonsterPanelMonsterAssociation`.
- **Sanctum/** (6): `SanctumFloorData`, `SanctumFloorWindow`,
  `SanctumFloorWindowDataSelector`, `SanctumRewardWindow`, `SanctumRoomData`,
  `SanctumRoomElement`.
- **Village/** (19): `VillageScreen`, `VillageInfo`, `VillageRecruitmentPanel`(+ worker
  element), `VillageWorkerManagementPanel`(+ worker element), `VillageShipmentScreen`,
  `VillageShipInfo`/`VillageShipmentRequest`/`VillagePortRequest`,
  `VillageResourceContainer`, `VillageWorker`/`VillageWorkerForSale`/`BaseVillageWorker`,
  and **Currency Exchange**: `CurrencyExchangePanel`(+ order element),
  `CurrencyExchangeCurrencyPickerElement`(+ option), `CurrencyExchangeStock`.

### 3.6 `ExileCore.PoEMemory.FilesInMemory` (all — static `.dat` wrappers)

Top-level `.dat` wrappers:
```
AlternateQualityType · ArchnemesisMod · Ascendancy · AtlasPrimordialAltarChoice
(+ Type) · AtlasPrimordialBossOption · BaseItemTypes · BestiaryCapturableMonsters ·
BestiaryRecipeCategory · BetrayalChoice/Job/Rank/Reward/Target · BlightTowerDat ·
BuffDefinition · BuffVisual · CachedStatDescription (+ Section) · Character · ChestRecord ·
ClientString · CurrencyExchangeCategory · CurrencyExchangeEntry · CurrencyItemDat ·
DelveBiome · DelveFeature · GemEffect · GemEffects ○ · GrantedEffect ·
GrantedEffectPerLevel · GroundEffectDat · GroundEffectTypeDat · IndexableSupportGem ·
ItemVisualIdentities · ItemVisualIdentity · LabyrinthTrial · LabyrinthTrials · LakeRoom ·
MapKeyDat · MapPin · MinimapIconDat · MiscAnimatedArtVariation/Dat/List ·
ModTranslationReplacerInput · ModsDat · MonsterVarieties · NecropolisCraftingMod ·
NecropolisPack (+ Mod, ModTier) · PackFrequencyName · PassiveSkill · PassiveSkills ·
PropheciesDat · QuestFlagDat · QuestFlagsDat · QuestReward · QuestRewardOffer ·
QuestStates · Quests · SkillArtVariation · SkillGemDat (+ SocketType) · StampChoice ·
StatDescription (+ Section/StringContainer/Wrapper) · StatHandling · StatTranslationUtils ·
StatsDat · TagsDat · TinctureDat · UltimatumItemisedReward (+ Type) ·
UniqueItemDescription(s) · UniversalFileWrapper<T> · VillageUniqueDisenchantValue ·
WordEntry · WorldAreas
```
League / mechanic subfolders:
- **Atlas/** — `AtlasNodes`, `AtlasRegion`, `AtlasRegions`
- **Ancestor/** — `AncestralTrialItem`, `AncestralTrialTribe`, `AncestralTrialUnit`
- **Archnemesis/** — `ArchnemesisRecipe`
- **Harvest/** — `HarvestSeed`
- **Labyrinth/** — `LabyrinthArea`, `LabyrinthNodeOverride`, `LabyrinthSectionDat`,
  `LabyrinthSectionLayout`
- **Metamorph/** — `MetamorphMetaMonster`, `MetamorphMetaSkill`, `MetamorphMetaSkillType`,
  `MetamorphRewardType`, `MetamorphRewardTypeItemsClient`
- **Sanctum/** — `SanctumRoom`, `SanctumRoomType`, `SanctumDeferredReward(Category)`,
  `SanctumPersistentEffect(Category)`
- **Ultimatum/** — `UltimatumModifier`
- **Village/** (11, incl. **Currency Exchange** data) — `VillageJob`/`VillageJobType`/
  `VillageJobs`, `VillageJobSkillLevel(s)`, `VillageProduction`, `VillageResource`,
  `VillageShippingPort`, `VillageUpgrade`/`VillageUpgradeCategory`, `VillageExport`

> **Bestiary, Atlas, Betrayal, Delve and Necropolis** subsystems are present both as
> `.dat` wrappers here and as memory objects in §3.4. **Heist** and **Expedition** live
> mostly as components (§3.3) + memory objects (Heist/) + elements (§3.5).

**`FileInMemory`** (✅, abstract base): `IMemory M { get; }`, `long Address { get; }`.
**`UniversalFileWrapper<RecordType>`** (✅, `where RecordType : RemoteMemoryObject, new()`):
`List<RecordType> EntriesList { get; }`, `RecordType GetByAddress(long address)`,
`void CheckCache()`. Most `.dat` accessors on `FilesContainer` return either a bespoke
wrapper or `UniversalFileWrapper<T>` (e.g. `UniversalFileWrapper<AtlasNode> AtlasNodes`,
`UniversalFileWrapper<BetrayalTarget> BetrayalTargets`, …), each resolved lazily via
`FindFile("Data/<Name>.dat")`.

### 3.7 `ExileCore.Shared.*` and engine infra

**Interfaces** (`Shared.Interfaces`): `IMemory`, `IPlugin`, `ISettings`,
`ISettingsHolder`, `IStaticCache`, `IPattern`, `INativePtrArray`, `IYieldBase`,
`MemoryBackendMode`. **`IMemory`** is the read contract (full signature in §4).

**Nodes** (`Shared.Nodes`) — JSON-serialized settings widgets:
`ToggleNode`(✅, `implicit operator bool`, `OnValueChanged`, `SetValueNoEvent`),
`HotkeyNode`(✅, `Keys Value`, `PressedOnce()`, `UnpressedOnce()`, `implicit operator Keys`)
+ `HotkeyNodeV2`○, `RangeNode<T>`(✅, `Value`/`Min`/`Max`, `OnValueChanged`),
`ButtonNode`, `ColorNode`(+ converter), `TextNode`, `FileNode`(+ converter), `ListNode`,
`StashTabNode`, `EmptyNode`, `CustomNode`, `ContentNode`○(+ converter), `IContentNodeBase`,
`JsonSerializationHelper`, `SortContractResolver`.

**Cache** (`Shared.Cache`): `CachedValue` / `CachedValue<T>`(✅ — `Value`, `RealValue`,
`ForceUpdate()`, `OnUpdate` event; nested `CacheUpdateEvent` delegate), `TimeCache<T>`(✅ —
`NewTime(long)`), `FrameCache`, `FramesCache`, `ConditionalCache`, `StaticCache`,
`StaticStringCache`, `StaticValueCache`, `KeyTrackingCache`, `LatancyCache`, `ValidCache`,
`AreaCache`, `Cache`, `CacheUtils`.

**Coroutines** (`Shared.Coroutine` / `Runner` / `SyncTask`): `Coroutine`(✅ — ctors over
`Action`/`IEnumerator`/interval, `Priority`, `Pause/Resume/Done`, `MoveNext()`,
`WhenDone`), yields `WaitTime`, `WaitRender`(+ static `Frame()`), `WaitFunction`,
`WaitRandom`, `WaitFunctionTimed`○, base `YieldBase`/`IYieldBase`; plus `SyncTask`,
`SyncTaskMethodBuilder`○, `SyncAwaiter`○, `TaskUtils`○, `CoroutineTask`, `NextFrameTask`○,
`Runner`.

**Helpers** (`Shared.Helpers`): `Extensions`, `MathHepler`, `MiscHelpers`, `ConvertHelper`,
`InputHelper`, `IntPtrExtensions`, `DictionaryExtensions`, `ActionExtensions`,
`PairwiseExtension`, `PerformanceTimer`, `PoeMapExtension`, `SpriteHelper`,
`WindowsUtils`, `MoreLinq`.

**Attributes** (`Shared.Attributes`): `MenuAttribute`, `SubmenuAttribute`,
`ConditionalDisplayAttribute`, `HideInReflectionAttribute`.

**Plugin host** (`Shared`): `PluginManager`(◑ — `Plugins`, `AllPluginsLoaded`,
`RootDirectory`, `CloseAllPlugins()`, `ReceivePluginEvent(...)`; nested `LoadedAssembly`,
`PluginMountConfig`, `NotificationId`), `PluginWrapper`, `PluginKind`, `PluginNotification`,
`PluginAssemblyLoadContext`○, `RoslynCompiler`, `PluginCompiler`○ + `MsBuildLogger`,
`BuildTarget`/`BuildError`/`BuildWarning`.

**Other Shared**: `Constants`, `IntRange`, `CircularBuffer`, `DebugInformation`(+
`MeasureHolder`), `MultiThreadProp`, `HudTexture`, `SpanExtensions`, `WinApi`; plus the
low-level memory subnamespaces `Shared.SomeMagic` (`MemoryLiterate`, `SafeMemoryHandle`,
`NativeMethods`, `Pointer`, `MarshalType`, `TypeConverter`, …), `Shared.PInvoke`
(`DynamicImport`, `ClientID`, `ObjectAttributes`), `Shared.AtlasHelper`
(`AtlasTexture`/`AtlasConfigData`/`AtlasTexturesProcessor`), `Shared.Static`
(`HudSkin`, `ItemClasses`), `Shared.Abstract.BaseIcon`.

**Renderer** (`ExileCore.RenderQ`): `DX11`(◑ — `D11Device`, `DeviceContext`,
`RenderTargetView`, `ImGuiRender`, `SpritesRender`, `Clear()`, `Render(double, Core)`,
`AddOrUpdateTexture`/`GetTexture`/`HasTexture`/`DisposeTexture`), `ImGuiRender`(+
`PopFont`), `SpritesRender`, `TextureLoader`, `FontContainer`, `ThemeEditor`.

**Enums** (`Shared.Enums`, 48): `GameStat`, `BuffEnums`, `EntityType`, `ItemRarity`,
`MonsterRarity`, `CharacterClass`, `League`, `InventorySlotE`/`InventoryTypeE`/
`InventoryIndex`/`InventoryNameE`/`InventoryEnums`, `MapType`/`MapIconsIndex`/
`MyMapIconsIndex`/`MapPin`, `ActionFlags`, `AnimationE`, `DamageType`, `Direction`,
`FontAlign`, `NetworkStateE`, `PantheonGod`, `PartyAllocation`/`PartyStatus`, `QuestFlag`,
`SocketColor`, `SkillGemQualityTypeE`, `StatType`/`ItemStatEnum`, `ModDomain`/`ModType`,
`Influence`/`InfluenceTypes`, `HeistJobE`, `ChestType`, `CreatureType`, `IconPriority`,
`ToolTipType`, `MouseActionType`, `CoroutinePriority`, `OffsetsName`, `AreaTransitionType`,
`ItemType`, `DiagnosticInfoType`, `EnumerationsMagic`, `PackFrequencyName` (file),
`AlternateQualityType` (file).

> **Large enums** — do not inline. `GameStat`
> (`Core/Shared/Enums/GameStat.cs`, ~53k lines / values) and `BuffEnums`
> (`Core/Shared/Enums/BuffEnums.cs`, ~900 lines) are exhaustive game-data enumerations;
> consult the files directly. `EntityType` has ~35 members.

---

## 4. Key engine entry points / functions

The methods a plugin actually calls each frame:

```csharp
// --- draw (Graphics) ---
Vector2N Graphics.DrawText(string text, Vector2N position, Color color, int height, FontAlign align);
Vector2N Graphics.MeasureText(string text, int height);
void     Graphics.DrawLine(Vector2N p1, Vector2N p2, float borderWidth, Color color);
void     Graphics.DrawBox(RectangleF rect, Color color, float rounding);
void     Graphics.DrawFrame(RectangleF rect, Color color, int thickness);
void     Graphics.DrawImage(string fileName, RectangleF rectangle, Color color);

// --- project world to screen (Camera) ---
Vector2  Camera.WorldToScreen(Vector3 vec);

// --- entities & components ---
T        Entity.GetComponent<T>()           where T : Component, new();
bool     Entity.HasComponent<T>()           where T : Component, new();
T        Entity.GetHudComponent<T>()        where T : class;     // your per-entity HUD cache
ICollection<Entity> GameController.Entities;
static Entity EntityListWrapper.GetEntityById(uint id);

// --- raw memory (IMemory / Memory) ---
T        IMemory.Read<T>(long addr, params int[] offsets) where T : struct;   // + IntPtr/Pointer overloads
string   IMemory.ReadString (long addr, int length = 256, bool replaceNull = true);  // ASCII
string   IMemory.ReadStringU(long addr, int length = 256, bool replaceNull = true);  // Unicode
string   IMemory.ReadNativeString(long addr);
byte[]   IMemory.ReadBytes(long addr, int size);
List<T>  IMemory.ReadStructsArray<T>(long start, long end, int structSize, RemoteMemoryObject game)
                                                          where T : RemoteMemoryObject, new();
IList<T> IMemory.ReadDoublePtrVectorClasses<T>(long address, RemoteMemoryObject game, bool noNullPointers = false)
                                                          where T : RemoteMemoryObject, new();
IList<T> IMemory.ReadNativeArray<T>(INativePtrArray ptrArray, int offset = 8) where T : struct;
IList<long> IMemory.ReadPointersArray(long start, long end, int offset = 8);
IList<T> IMemory.ReadList<T>(IntPtr head) where T : struct;
long[]   IMemory.FindPatterns(params IPattern[] patterns);
// object-graph helpers on RemoteMemoryObject:
T        RemoteMemoryObject.GetObject<T>(long address) where T : RemoteMemoryObject, new();
T        RemoteMemoryObject.ReadObjectAt<T>(int offset) where T : RemoteMemoryObject, new();

// --- static data (FilesContainer) ---
long     FilesContainer.FindFile(string name);
//   typed lazy accessors: .Mods .Stats .Tags .WorldAreas .PassiveSkills .Quests
//   .MonsterVarieties .AtlasNodes .BetrayalTargets .BestiaryCapturableMonsters …

// --- input (Input, static) ---
bool     Input.GetKeyState(Keys key);   void Input.RegisterKey(Keys key);
void     Input.SetCursorPos(Vector2 vec);   IEnumerator Input.KeyPress(Keys key);
void     Input.Click(MouseButtons buttons);   void Input.VerticalScroll(bool forward, int clicks);

// --- cross-plugin RPC (PluginBridge) ---
T        PluginBridge.GetMethod<T>(string name) where T : class;
void     PluginBridge.SaveMethod(string name, object method);

// --- plugin lifecycle (BaseSettingsPlugin) ---
bool     Initialise();   Job Tick();   void Render();   void AreaChange(AreaInstance area);
void     SetApi(GameController gc, Graphics gfx, PluginManager pm);
```

---

## 5. Caveats & how to use

- **Signatures and enum values are authoritative** (metadata is intact). **Stubbed method
  bodies are not** — ~17% of bodies (610 markers) are protected and replaced with
  `throw new NotImplementedException("Body protected in source DLL")`; do not rely on a ◑/○
  type's *behavior* without re-checking against a running build.
- **Offsets are build-specific.** `Address + 0x…` literals (e.g. in `Player`, `Base`,
  `ServerData`) match only assembly 10.0.14.7603. A different game patch shifts them; this
  fork's structured offsets live under `GameOffsets` (see [offsets.md](../offsets.md)).
- **This is a reference for understanding the full engine and for porting.** The slim fork
  in this repo (`docs/api/*.md`) documents only the symbols present in `Core/`; this file
  documents the *entire* compiled surface so plugin authors can see what the upstream
  engine offers. For what does and does not carry over, read
  [compatibility-exileapi-compiled.md](compatibility-exileapi-compiled.md).
- **Plugin shape**: derive from `BaseSettingsPlugin<TSettings>`; the host injects
  `GameController` + `Graphics` via `SetApi`, then drives `Tick()`/`Render()` each frame —
  see [plugins.md](plugins.md) and the [API reference index](README.md).

### Notable engine functions / types present in the compiled DLL but absent from this fork

The reconstruction adds 389 types; ~15 of the most consequential gaps versus this fork's
`Core/`:

1. **`AtlasPanel`** / `AtlasNode` / `AtlasRegion(s)` / `WorldMapElement` — full Atlas
   tree + region upgrade reads.
2. **Village + Currency Exchange** (`CurrencyExchangePanel`, `VillageScreen`,
   `VillageShipmentScreen`, `VillageWorkerManagementPanel`, …) — the entire 30-type
   Settlers/CE subsystem.
3. **Sanctum** (`SanctumFloorWindow`, `SanctumRoomData`/`Element`, `SanctumRewardWindow`,
   `SanctumRoom(Type)`, persistent/deferred reward data).
4. **Necropolis** (`NecropolisMonsterPanel`, `NecropolisCorpse`, `NecropolisPack(Mod/Tier)`,
   `NecropolisCraftingMod`).
5. **Ancestor / Tribal** (`AncestorFightSelectionWindow`, `AncestorMainShopWindow`,
   `AncestorServerData`, `AncestralTrial*`).
6. **Heist** (`HeistBlueprint`/`HeistContract`/`HeistEquipment`/`HeistRewardDisplay`
   components + `HeistJobRecord`/`HeistChestRecord`/`HeistNpcRecord`).
7. **Expedition** (`ExpeditionDetonator(Info)`, `ExpeditionVendorElement`,
   `TujenHaggleWindowElement`, `ExpeditionSaga`, `ExpeditionAreaData`).
8. **Harvest** (`HarvestWindow`, `HarvestCraftElement`, `HarvestInfrastructure*`,
   `HarvestSeed*`).
9. **Sentinel** (`SentinelPanel`/`SentinelSubPanel`, `SentinelData`/`SentinelState`,
   `SentinelDrone`).
10. **Snapshot memory backend** (`SnapshotMemoryBackend`, `SnapshotBuilder`,
    `SnapshotSettings`, `PagedMemoryBackend`, `IMemoryBackend`) — offline/recorded-memory
    replay not present in the live-only fork.
11. **`DataExporter`** — bulk dump/export of parsed game data.
12. **`StatCollector` / `EntityEffect` / `EffectEnvironment` / `EnvironmentData`** —
    environment-effect and stat-collection machinery.
13. **Party / Social** (`PartyElement(+Player*)`, `SocialElement`, `InvitesPanel`,
    `RemotePlayerInfo`, `PartyPlayerInfo`).
14. **`ProcessPicker` / `NamedPipeHandler` / `CommandExecutor`** — process attach UI and
    IPC/command surfaces.
15. **`Tincture` component + `TinctureDat`**, **`ArchnemesisPanelElement` + `ArchnemesisMod`/
    `ArchnemesisRecipe`**, **`UltimatumPanel`/`UltimatumModifier`** — additional league
    mechanic types.

---

## 6. Source

- **Reconstruction branch**: `origin/claude/ecstatic-ritchie-cje3s8` (PR #18),
  commit **`9dde92e4882fb8c79d2cff84b420379d70086479`** — the ILSpy decompilation of
  `ExileCore.dll` (assembly 10.0.14.7603, net10).
- **Methodology / status counts**: `docs/ported-from-dll.md` (on that branch).
- **Files inventoried** (via `git show <branch>:<path>`): the 698 `Core/**/*.cs`
  reconstruction files, in particular `Core/Core.cs`, `Core/GameController.cs`,
  `Core/Graphics.cs`, `Core/Input.cs`, `Core/Memory.cs`,
  `Core/Shared/Interfaces/IMemory.cs`, `Core/BaseSettingsPlugin.cs`,
  `Core/EntityListWrapper.cs`, `Core/AreaController.cs`, `Core/Shared/PluginManager.cs`,
  `Core/RenderQ/DX11.cs`, `Core/PoEMemory/RemoteMemoryObject.cs`,
  `Core/PoEMemory/Element.cs`, `Core/PoEMemory/Component.cs`,
  `Core/PoEMemory/FilesContainer.cs`, `Core/PoEMemory/FileInMemory.cs`,
  `Core/PoEMemory/FilesFromMemory.cs`, `Core/PoEMemory/FilesInMemory/UniversalFileWrapper.cs`,
  `Core/PoEMemory/MemoryObjects/{Entity,Camera,IngameState,ServerData}.cs`,
  `Core/PoEMemory/Components/{Life,Actor,Mods,Sockets,Stats,Buffs,Render,Positioned,Player,Base}.cs`,
  and the `Core/Shared/{Nodes,Cache,Coroutine}` sources.
- The binary `ExileCore.dll` itself is **not shipped in this repository**.
