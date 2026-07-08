# BaseTreeRoutine (ported candidate)

> **EXPERIMENTAL — not compiled in this environment.**
> This is Windows + live-game only. It cannot be built or run here (no `ExileCore.dll`, no game
> process, and the repo targets `net10.0-windows` reading a live PoE process). Every file starts with
> an `// EXPERIMENTAL candidate ...` banner and lives under `proposals/` so it is **outside the build**
> and cannot break `Core/`, `GameOffsets/` or `Loader/`. Nothing under `Core/` was modified.

## What BaseTreeRoutine / TreeSharp is

[`BasicFlaskRoutine`](https://github.com/exApiTools/BasicFlaskRoutine) vendors a small reusable
behaviour-tree library, `TreeRoutine`, whose core is a copy of the classic **TreeSharp** node set
(`Composite`, `Decorator`, `PrioritySelector`, `Sequence`, `Action`, `RunStatus`). A behaviour tree
turns "read the player state every tick, press a key when a condition is met" automation into a
declarative graph:

- a **`Decorator`** runs its single child only when a guard predicate is true (the "condition" node);
- a **`PrioritySelector`** runs children in order and stops at the first success (an OR / priority list);
- a **`Sequence`** runs children in order and stops at the first failure (an AND);
- an **`Action`** is a leaf that does the work and reports `Success`/`Failure`/`Running`.

`BaseTreeRoutinePlugin` is the base plugin that builds such a tree once and **ticks it on a timer**
(off the render thread) so a slider can throttle it. `BasicFlaskRoutine` derives from it and builds a
flask/defensive-skill tree; this port provides the reusable base, not the concrete flask logic.

The tree is ticked in a coroutine loop, matching upstream:

```csharp
// Built once (subclass CreateTree), reads like BasicFlaskRoutine.CreateTree():
Tree = new Decorator(_ => TreeHelper.CanTick(GameController) && ReadPlayerState(),
    new PrioritySelector(
        new Decorator(_ => Settings.AutoFlask.Value,
            new PrioritySelector(CreateInstantHpComposite(), CreateHpComposite(), CreateManaComposite())),
        CreateAilmentComposite(),
        CreateDefensiveComposite()));

// A leaf branch: "HP below threshold AND no life-flask buff -> press the life-flask key".
private Composite CreateHpComposite() =>
    new Decorator(_ => _player.IsHealthBelow(Settings.HpThreshold.Value) && !_player.HasBuff("flask_effect_life"),
        new UseHotkeyAction(() => Settings.LifeFlaskKey.Value));   // HotkeyNode -> Keys
```

## How this port maps onto our fork

**Plugin's own copied types (the TreeSharp graph — NOT engine API).** `RunStatus`, `Composite`,
`GroupComposite`, `Decorator`, `PrioritySelector`, `Sequence` and `Action` are copied/adapted from the
bundled `TreeRoutine/TreeSharp/*` and live in namespace `ExileCore.TreeRoutine.TreeSharp`. They have no
engine dependency at all — pure C#/coroutines. The execution model is faithful to upstream TreeSharp:
`Execute(context)` is an iterator that yields `Running` while working and finally a terminal
`Success`/`Failure`; `Tick(context)` advances it one step; the plugin re-`Start()`s the root only when
the previous tick did not leave it `Running`.

**Engine types the base plugin builds on.** `BaseTreeRoutinePlugin<TSettings>` extends this fork's
`BaseSettingsPlugin<TSettings>` and, in `Initialise`, wraps the tick in an engine `Coroutine` with a
`WaitTime` period and registers it with `Core.ParallelRunner.Run(...)`. `Coroutine`, `WaitTime`,
`Runner`/`Core.ParallelRunner` are engine types (see [../../docs/api/utilities.md](../../docs/api/utilities.md)).

**Fork-native default behaviours.** The `DefaultBehaviors` helpers are rewritten against this fork:
`TreeHelper.CanTick` gates on the real `GameController`/`AreaInstance` members; `PlayerHelper` reads
HP/ES/Mana % and buffs off `Life` (this fork has **no `Buffs` component**); `FlaskHelper` computes usable
flask charges from the `Charges` component; and `UseHotkeyAction` presses keys through the engine
`Input` API instead of upstream's `SendMessage` P/Invoke.

The whole pattern, mapped onto this fork's API, is documented at
[../../docs/api/cookbook/automation-routines.md](../../docs/api/cookbook/automation-routines.md),
section *"(a) Behaviour tree — BaseTreeRoutine / BasicFlaskRoutine"*.

### Structure & namespace choice

Files are organised into subfolders mirroring upstream's `TreeRoutine/` layout
(`TreeSharp/`, `DefaultBehaviors/Helpers/`, `DefaultBehaviors/Actions/`) rather than kept flat, because
the node classes read most clearly as a self-contained `TreeSharp` unit. The `RunStatus` enum plus the
six TreeSharp classes are kept in separate files (the task's suggested split) rather than a single
`TreeSharp.cs`. Namespaces follow upstream (`TreeRoutine`, `TreeRoutine.TreeSharp`,
`TreeRoutine.DefaultBehaviors.*`) prefixed with this fork's root: **`ExileCore.TreeRoutine`** and its
sub-namespaces. All `.cs` files are loose (no `.csproj`), matching `proposals/IconsBuilder` and
`proposals/Compat`.

## Files in this port

| File | Namespace | Role |
|------|-----------|------|
| `TreeSharp/RunStatus.cs` | `ExileCore.TreeRoutine.TreeSharp` | The `Success`/`Failure`/`Running` enum. |
| `TreeSharp/Composite.cs` | `ExileCore.TreeRoutine.TreeSharp` | Abstract base node: coroutine-based `Start`/`Tick`/`Execute` + `LastStatus`. |
| `TreeSharp/GroupComposite.cs` | `ExileCore.TreeRoutine.TreeSharp` | Abstract base holding an ordered `Children` list (shared by selector/sequence). |
| `TreeSharp/Decorator.cs` | `ExileCore.TreeRoutine.TreeSharp` | Runs its child only when a `Func<object,bool>` guard passes; the "condition" node. |
| `TreeSharp/PrioritySelector.cs` | `ExileCore.TreeRoutine.TreeSharp` | OR / priority list: first child to succeed wins. |
| `TreeSharp/Sequence.cs` | `ExileCore.TreeRoutine.TreeSharp` | AND: fails at the first child that fails. |
| `TreeSharp/Action.cs` | `ExileCore.TreeRoutine.TreeSharp` | Leaf: runs a delegate or an overridden `Run`, returns a `RunStatus`. |
| `BaseTreeRoutineSettings.cs` | `ExileCore.TreeRoutine` | `ITreeSettings : ISettings` (adds `TicksPerSecond`) + a ready `BaseTreeRoutineSettings` base. |
| `BaseTreeRoutinePlugin.cs` | `ExileCore.TreeRoutine` | Base plugin: `CreateTree()` once, tick on a `Coroutine`/`WaitTime` via `Core.ParallelRunner`. |
| `DefaultBehaviors/Helpers/TreeHelper.cs` | `ExileCore.TreeRoutine.DefaultBehaviors.Helpers` | `CanTick(GameController)` gating. |
| `DefaultBehaviors/Helpers/PlayerHelper.cs` | `ExileCore.TreeRoutine.DefaultBehaviors.Helpers` | HP/ES/Mana %, buff checks, skill-usable — via `Life`/`Actor`. |
| `DefaultBehaviors/Helpers/FlaskHelper.cs` | `ExileCore.TreeRoutine.DefaultBehaviors.Helpers` | Per-slot usable-charge check via the `Charges` component. |
| `DefaultBehaviors/Actions/UseHotkeyAction.cs` | `ExileCore.TreeRoutine.DefaultBehaviors.Actions` | Key-press leaf via the engine `Input` API. |

## Exact fork members this port builds on (verified in `master`)

Line numbers are from `git show master:<path>` at the time of writing.

Plugin host & settings
- `Core/BaseSettingsPlugin.cs:13` — `BaseSettingsPlugin<TSettings> where TSettings : ISettings, new()`; `:27` `GameController`, `:29` `Settings`, `:38` `Name`, `:68` `Dispose`, `:105` `Initialise`.
- `Core/Shared/Interfaces/ISettings.cs:5` — `ISettings`; `:7` `ToggleNode Enable`.
- `Core/Shared/Interfaces/IPlugin.cs:15` — `IPlugin` (the `Coroutine` owner type; `BaseSettingsPlugin` implements it).
- `Core/Shared/Nodes/ToggleNode.cs:6` — `ToggleNode`; `:19` `Value`.
- `Core/Shared/Nodes/RangeNode.cs:7` — `RangeNode<T>`; `:22` `Value`.
- `Core/Shared/Nodes/HotkeyNode.cs:8` — `HotkeyNode`; `:25` `Keys Value`; `:46` `implicit operator Keys`.

Coroutine / runner (engine tick loop)
- `Core/Shared/Coroutine.cs:10` — `Coroutine`; `:21` ctor `(Action, IYieldBase, IPlugin, string name, bool infinity, bool autoStart)`; `:115` `UpdateCondtion(IYieldBase)`; `:159` `Done(bool)`.
- `Core/Shared/Coroutine.cs:279` — `WaitTime : YieldBase`; `:323` `IYieldBase`.
- `Core/Shared/Runner.cs:32` — `Runner`; `:87` `Run(Coroutine)`.
- `Core/Core.cs:24` — `Core`; `:178` `static Runner ParallelRunner`.

Game model & gating
- `Core/GameController.cs:121` `Game`, `:122` `Area`, `:123` `Window`, `:124` `IngameState`, `:126` `Player`, `:129` `IsLoading`.
- `Core/PoEMemory/MemoryObjects/GameStateContoller.cs:11` `TheGame`; `:61` `IngameState`; `:70` `IsEscapeState` (available; used as an optional extra gate).
- `Core/PoEMemory/MemoryObjects/IngameState.cs:9` `IngameState`; `:68` `Data`; `:70` `ServerData`.
- `Core/PoEMemory/MemoryObjects/IngameData.cs:11` `IngameData`; `:39` `LocalPlayer`.
- `Core/PoEMemory/MemoryObjects/ServerData.cs:13` `ServerData`; `:79` `IsInGame`; `:238` `GetPlayerInventoryByType(InventoryTypeE)`.
- `Core/AreaController.cs:8` `AreaController`; `:18` `CurrentArea`.
- `Core/AreaInstance.cs:7` `AreaInstance`; `:26` `IsTown`; `:27` `IsHideout`.
- `Core/GameWIndow.cs:10` `GameWindow`; `:43` `IsForeground()`.
- `Core/Shared/Enums/InventoryTypeE.cs:7` `InventoryTypeE`; `:19` `Flask`.

Entity & components
- `Core/PoEMemory/RemoteMemoryObject.cs:12` — `Address`.
- `Core/PoEMemory/MemoryObjects/Entity.cs:97` `IsValid`; `:279` `Buffs`; `:580` `HasComponent<T>()`; `:605` `GetComponent<T>()`. (No `TryGetComponent`.)
- `Core/PoEMemory/MemoryObjects/ServerInventory.cs:11` `ServerInventory`; `:31` `InventorySlotItems`; `:115` `InventSlotItem`; `:119` `Item`; `:120` `PosX`.
- `Core/PoEMemory/MemoryObjects/ActorSkill.cs:8` `ActorSkill`; `:13` `CanBeUsed`; `:24` `Name`.
- `Core/PoEMemory/Components/Life.cs:14` `Life`; `:37` `CurHP`; `:64` `HPPercentage`; `:67` `MPPercentage`; `:70` `ESPercentage`; `:79` `Buffs`; `:122` `HasBuff(string)`. (Re-verified
  against the current file; these members gained XML doc comments since this table was first written,
  shifting line numbers — content/signatures are unchanged.)
- `Core/PoEMemory/Components/Buff.cs:9` `Buff`; `:41` `Name`; `:44` `Charges` (byte); `:49` `MaxTime`; `:52` `Timer`. (Re-verified;
  same drift as `Life.cs` above.)
- `Core/PoEMemory/Components/Charges.cs:3` `Charges`; `:5` `NumCharges`; `:6` `ChargesPerUse`; `:7` `ChargesMax`.
- `Core/PoEMemory/Components/Flask.cs` — `Flask` (empty marker component).
- `Core/PoEMemory/Components/Actor.cs:10` `Actor`; `:77` `ActorSkills`.

Input
- `Core/Input.cs:14` `Input`; `:194` `IEnumerator KeyPress(Keys)`; `:201` `KeyDown(Keys)`; `:206` `KeyUp(Keys)`; `:247` `KeyPressRelease(Keys)`.

## Members deliberately NOT used / adapted

- **`SendMessage` P/Invoke (`WM_KEYDOWN`/`WM_KEYUP`) and `WindowsInput.InputSimulator`** — upstream's
  `UseHotkeyAction` and `BuffUtil` press keys via P/Invoke / an external simulator. Neither is used here:
  `UseHotkeyAction` calls the engine `Input.KeyPressRelease(Keys)` (rate-limited ~10 ms). `Input.KeyDown`/
  `KeyUp`/`KeyPress` are available for held/coroutine presses.
- **`Buffs` component (`player.GetComponent<Buffs>().BuffsList`)** — does not exist on this fork
  (confirmed by grep: no `Buffs` type under `Core/PoEMemory/Components/`). `PlayerHelper` reads the fix
  directly, not a workaround: `Life.Buffs` (`List<Buff>`) and `Life.HasBuff(string)` on
  `Core/PoEMemory/Components/Life.cs:79,122`. `Entity.Buffs` (`Core/PoEMemory/MemoryObjects/Entity.cs:279`)
  is the same data via a convenience accessor, but `PlayerHelper.Life` is fetched once via
  `GameController.Player.GetComponent<Life>()` and `.Buffs`/`.HasBuff` are read straight off it, because
  `Life.Buffs` is backed by a `FrameCache<List<Buff>>` (recomputed at most once per rendered frame; see
  `Core/Shared/Cache/FrameCache.cs`), whereas `Entity.Buffs` is backed by a `ValidCache<List<Buff>>` whose
  `Update()` returns `true` (i.e. re-reads process memory) on **every** access while the entity is valid
  (`Core/Shared/Cache/ValidCache.cs`) — going through `Life` avoids redundant memory reads when a tree
  checks several buffs in one pass. Behaviorally identical; `Entity.Buffs` is fine for a single ad-hoc read.
- **`Entity.TryGetComponent<T>(out ...)`** — does not exist (confirmed by grep over
  `Core/PoEMemory/MemoryObjects/Entity.cs`; only `GetComponent<T>()` and `HasComponent<T>()` are present,
  at lines 605 and 580). `PlayerHelper`/`FlaskHelper` call `GetComponent<T>()` and null-check the result
  (`?.`) instead of the `out`-parameter pattern; this is a straight substitution, not a workaround — there
  is no loss of information versus a hypothetical `TryGetComponent`, since `GetComponent<T>()` already
  returns `null` on a missing component (see `Entity.cs:605-617`).
- **`Life.Health` / `Life.Mana` / `Life.EnergyShield` aggregate objects** — absent. Percentages come from
  the flat `HPPercentage`/`MPPercentage`/`ESPercentage` helpers (× 100 for a 0..100 value).
- **`Buff.DisplayName` / `Buff.BuffCharges` / `Buff.FlaskSlot`** — absent. Only `Name`/`Charges`/`Timer`/
  `MaxTime` exist; `BuffCharges` maps to `Charges`.
- **`AreaInstance.IsPeaceful`** — absent. `TreeHelper.CanTick` gates on `IsTown`/`IsHideout` instead.
  (`GameController.Game.IsEscapeState` *is* present and can be added as an extra gate if wanted.)
- **A usable-charge member on `Flask`** — `Flask` is an empty marker; usable charges are computed from the
  flask entity's `Charges` component (`NumCharges` / `ChargesPerUse`). The `flaskchargesused` mod / the
  `GameStat.FlaskChargesUsedPct` refinement (upstream's `FlaskHelper`) is left out to keep the port minimal.
- **TreeSharp events / `Guid` equality / `Parallel` composite** — the upstream TreeSharp carries a few
  extras (behaviour-tree events, node identity, a parallel composite) not needed by the flask pattern;
  this port keeps the minimal node set the cookbook and `BasicFlaskRoutine.CreateTree()` actually use.

**Verification note:** the two bullets above (`Buffs` component / `TryGetComponent`) were re-audited
against `Core/PoEMemory/Components/Life.cs`, `Core/PoEMemory/Components/Buff.cs`,
`Core/PoEMemory/MemoryObjects/Entity.cs` and `Core/Shared/Cache/{FrameCache,ValidCache,CachedValue}.cs`
line-by-line. The port's `.cs` files never called a `Buffs` component or `TryGetComponent` to begin with
— `PlayerHelper`/`FlaskHelper` were written directly against the real accessors above. There is nothing
left to swap out.

## How to integrate

Namespaces: `ExileCore.TreeRoutine` (+ `.TreeSharp`, `.DefaultBehaviors.Helpers`, `.DefaultBehaviors.Actions`).

1. **Derive a plugin.** Make your settings extend `BaseTreeRoutineSettings` (or implement `ITreeSettings`)
   and add your routine toggles/thresholds/`HotkeyNode`s. Make your plugin extend
   `BaseTreeRoutinePlugin<YourSettings>` and implement `CreateTree()`:

   ```csharp
   public class MyFlaskRoutine : BaseTreeRoutinePlugin<MyFlaskSettings>
   {
       private PlayerHelper _player;

       protected override Composite CreateTree()
       {
           _player = new PlayerHelper(GameController);
           return new Decorator(_ => TreeHelper.CanTick(GameController) && _player.IsAlive,
               new PrioritySelector(
                   new Decorator(_ => _player.IsHealthBelow(Settings.HpThreshold.Value)
                                      && !_player.HasBuff("flask_effect_life"),
                       new UseHotkeyAction(() => Settings.LifeFlaskKey.Value))));
       }
   }
   ```

   The base handles the tick loop: it builds the tree once, ticks it `TicksPerSecond` times per second on
   a `Core.ParallelRunner` coroutine, respects `Settings.Enable`, and disposes the coroutine on unload.

2. **Move into `Core/`** (optional). Copy these files under `Core/` (e.g. `Core/Shared/TreeRoutine/`),
   keep or re-root the namespaces, drop the experimental banners, and remove this `proposals/` copy. The
   TreeSharp classes have no engine dependency, so nothing else needs to change.

### Dependencies

- `ExileCore.dll` (this fork): `BaseSettingsPlugin<>`, `GameController`, `Core.ParallelRunner`,
  `Coroutine`/`WaitTime`, `Input`, the `ExileCore.PoEMemory.*` entity/component model, and
  `ExileCore.Shared.*` nodes/enums/interfaces.
- `System.Windows.Forms` for `Keys` (as the rest of the fork's input surface uses).
- No third-party packages (no `WindowsInput`, no `TreeSharp` NuGet — the nodes are bundled here).

## Provenance

- **Upstream original:** [`exApiTools/BasicFlaskRoutine`](https://github.com/exApiTools/BasicFlaskRoutine)
  — the reusable base lives under `TreeRoutine/` (`TreeSharp/*`, `BaseTreeRoutinePlugin.cs`,
  `DefaultBehaviors/{Helpers,Actions}/*`); the concrete routine is `BasicFlaskRoutine/BasicFlaskRoutine.cs`.
- **Could not clone upstream in this environment.** `git clone https://github.com/exApiTools/BasicFlaskRoutine`
  and a direct `curl` to `github.com` both returned **HTTP 403** from the egress proxy (organization egress
  policy — `github.com` git/raw access is not allowed for this session), and the GitHub MCP is scoped to
  `nelleyn/poeapi` only (access to `exApiTools/*` denied). This was **not** a 404 — the repos exist; they
  are egress-blocked here.
- **Grounding used instead:** (1) this fork's cookbook page
  [../../docs/api/cookbook/automation-routines.md](../../docs/api/cookbook/automation-routines.md), which
  already maps `BasicFlaskRoutine`'s `CreateTree()`, tick loop, `UseHotkeyAction`, `TreeHelper.CanTick`,
  `PlayerHelper` and `FlaskHelper` onto this fork and cites the upstream files; and (2) the well-known
  canonical TreeSharp node semantics (`Composite`/`Decorator`/`Selector`/`Sequence`/`Action`/`RunStatus`)
  that `TreeRoutine/TreeSharp/*` copies. Every fork member referenced by the code was re-verified directly
  against `master` (see the table above), not taken on faith from the docs.
