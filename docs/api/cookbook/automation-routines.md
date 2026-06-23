# Recipe: flask/buff automation & condition→action routines

The "read the player state every tick, press a key when a condition is met" pattern — flask
pots, defensive skills, aura/cast keep-up — as built by `BasicFlaskRoutine`, `BuffUtil` and
`ReAgent`, rewritten against **this fork's** `Core/` API.

[API reference index](../README.md) · [cookbook index](README.md)

## The shape of the problem

Every flask/buff bot is the same loop:

1. **Gate** — are we actually playing? (in game, player valid, window focused, not in town,
   not dead). Bail early otherwise.
2. **Read** — snapshot the player's `Life` (HP/ES/Mana %, charges), buff list, skills and
   flask charges *once* per tick.
3. **Decide** — for each routine, test conditions against that snapshot
   (`HP% < 40`, `!HasBuff("...")`, `charges >= N`, cooldown elapsed).
4. **Act** — press the bound key via [`Input`](../input.md), respecting a global rate limit.

The three reference plugins differ only in **how step 3 is structured**: a behaviour tree
(`BasicFlaskRoutine`), a flat list of hand-written handlers (`BuffUtil`), or a generic
rule engine where the user writes the conditions (`ReAgent`).

> All three were written for **ExileApi‑Compiled**, which reads buffs off a dedicated
> `Buffs` component (`player.GetComponent<Buffs>().BuffsList`) and exposes aggregate
> `Life.Health`/`Life.Mana`/`Life.EnergyShield` sub-objects. **This fork has neither.** Here,
> buffs are read off `Life` (`Life.Buffs` / `Life.HasBuff`) and life pools are the flat
> `CurHP`/`MaxHP` + `HPPercentage` helpers. Every snippet below is already adapted; the
> upstream-only members are called out and linked to
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

## Step 1 — gating (when *not* to tick)

Adapted from `BasicFlaskRoutine`'s `TreeHelper.CanTick()` and `BuffUtil`'s `OnPreExecute()`.
All members below exist in this fork (`GameController`, `Core/GameController.cs`):

```csharp
private bool CanTick()
{
    if (GameController.IsLoading) return false;                       // GameController.IsLoading
    if (!GameController.Game.IngameState.ServerData.IsInGame) return false; // ServerData.IsInGame
    var player = GameController.Player;                               // == IngameState.Data.LocalPlayer
    if (player == null || player.Address == 0 || !player.IsValid) return false;
    if (!GameController.Window.IsForeground()) return false;          // don't drive input into a background window
    if (GameController.Area.CurrentArea.IsTown ||
        GameController.Area.CurrentArea.IsHideout) return false;      // AreaInstance.IsTown / .IsHideout
    return true;
}
```

> **Gate differences.** `BasicFlaskRoutine`/`ReAgent` also test
> `GameController.Game.IngameState.Data.LocalPlayer` (this fork: use `GameController.Player`,
> they resolve to the same entity). `ReAgent`'s escape-menu gate **is** available here as
> `GameController.Game.IsEscapeState` (`bool`, on `TheGame`). But its `Area.CurrentArea.IsPeaceful`
> gate is **not** — there is no `AreaInstance.IsPeaceful` on this fork; gate on
> `IsTown`/`IsHideout` instead (see
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md)).

## Step 2 — snapshot the player once per tick

Read the components once and reuse the values across all routines; don't re-`GetComponent` in
every condition. `GameController.Player` is the local-player [`Entity`](../entities.md); pull
its components with `GetComponent<T>()`. Members verified against
`Core/PoEMemory/Components/Life.cs`, `.../Actor.cs`, `.../Charges.cs`
(see [components-combat.md](../components-combat.md)).

```csharp
private float _hpPct, _mpPct, _esPct;
private List<Buff> _buffs;
private List<ActorSkill> _skills;

private bool ReadPlayerState()
{
    var player = GameController.Player;                  // the local player Entity (entities.md)

    // This fork: GetComponent<T>() returns null if absent — there is NO Entity.TryGetComponent<T>.
    var life = player.GetComponent<Life>();
    if (life == null || life.CurHP <= 0) return false;  // dead -> skip

    // Pools as 0..100 percentages. HPPercentage/MPPercentage are fractions of *unreserved* max.
    _hpPct = life.HPPercentage * 100f;                  // Life.HPPercentage
    _mpPct = life.MPPercentage * 100f;                  // Life.MPPercentage
    _esPct = life.ESPercentage * 100f;                  // 0 when MaxES == 0

    // Buffs live on Life in this fork (NOT a Buffs component). Cached per-frame internally.
    _buffs = life.Buffs;                                 // List<Buff>; or player.Buffs

    _skills = player.GetComponent<Actor>()?.ActorSkills; // Actor.ActorSkills (no ActorVaalSkills here)
    return true;
}
```

> **Upstream divergence.** `BuffUtil`/`BasicFlaskRoutine` write
> `player.GetComponent<Buffs>().BuffsList` and `player.TryGetComponent<Buffs>(out var b)`.
> On this fork there is **no `Buffs` component** and **no `TryGetComponent`**: read
> `Life.Buffs` (or `Entity.Buffs`) and null-check `GetComponent<T>()` yourself. `ReAgent`'s
> `VitalsInfo` reads `lifeComponent.Health`/`.Mana`/`.EnergyShield` aggregate objects — those
> do **not** exist here; use the flat `Cur*`/`Max*` pairs and the `*Percentage` helpers.
> `ReAgent` also reads `Actor.AnimationController` (upstream-only). All flagged in
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

### Buff checks

`Life` already provides the membership test; `Buff` carries stacks and timers
(`Core/PoEMemory/Components/Buff.cs`):

```csharp
var life = GameController.Player.GetComponent<Life>();

bool hasFortify = life.HasBuff("fortify");           // Life.HasBuff(string) — case-sensitive, exact match

// Stacks / time-remaining on a specific buff:
var bloodRage = life.Buffs?.FirstOrDefault(b => b.Name == "blood_rage");
byte rageStacks = bloodRage?.Charges ?? 0;           // Buff.Charges (stacks)
float timeLeft  = bloodRage?.Timer ?? 0f;            // Buff.Timer (MaxTime is +infinity for auras)
```

`BuffUtil`'s own helper is just a case-insensitive scan over the snapshot, which you can keep
verbatim (it does not depend on the `Buffs` component once you feed it `Life.Buffs`):

```csharp
private bool? HasBuff(string name) =>
    _buffs?.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
```

> `Buff` exposes `Name`, `Charges` (byte), `Timer`, `MaxTime`. Upstream's `Buff.DisplayName`,
> `Buff.BuffCharges`, `Buff.FlaskSlot`, `Buff.SourceEntity`/`SourceSkillId` (used by
> `ReAgent`'s `BuffDictionary`/`FlaskInfo`) are **not** on this fork's `Buff` — map
> `BuffCharges` → `Charges`.

### Flask charges

There is no usable-charge member; compute it from the `Charges` component. `Flask` itself is
an empty marker (`Core/PoEMemory/Components/Flask.cs`); read charges off the flask entity's
`Charges` component (`Core/PoEMemory/Components/Charges.cs`):

```csharp
// The flask inventory, the fork-native way (ServerData.GetPlayerInventoryByType):
var flaskInv = GameController.IngameState.ServerData
    .GetPlayerInventoryByType(InventoryTypeE.Flask);          // ServerInventory

foreach (var slot in flaskInv?.InventorySlotItems ?? Enumerable.Empty<ServerInventory.InventSlotItem>())
{
    var item = slot.Item;                                     // the flask Entity
    if (item?.GetComponent<Flask>() == null) continue;        // it is a flask

    var ch = item.GetComponent<Charges>();
    if (ch == null) continue;
    bool canUse = ch.NumCharges >= ch.ChargesPerUse;          // Charges.NumCharges / .ChargesPerUse / .ChargesMax
    int  uses   = ch.ChargesPerUse > 0 ? ch.NumCharges / ch.ChargesPerUse : 0;
    int  slotX  = (int)slot.PosX;                             // 0-based flask slot -> bind key Settings.FlaskKeys[slotX]
}
```

`BasicFlaskRoutine` adds charge-reduction maths (the `flaskchargesused` mod and the
`GameStat.FlaskChargesUsedPct` stat) to refine the per-use cost — both are available here via
`item.GetComponent<Mods>().ItemMods` and
`player.GetComponent<Stats>().StatDictionary.GetValueOrDefault(GameStat.FlaskChargesUsedPct)`
(see [components-combat.md](../components-combat.md) for `Stats`). It maps a flask's base name
to its buff string from a bundled `flaskinfo.json`; that lookup table is plugin data, not
engine API.

## Step 3 — three ways to structure the decision

### (a) Behaviour tree — `BaseTreeRoutine` / `BasicFlaskRoutine`

`BasicFlaskRoutine` ships a small TreeSharp-style behaviour tree (`Composite`, `Decorator`,
`PrioritySelector`, `Sequence`, `Action`). A **`Decorator`** runs its child only when a
predicate is true; a **`PrioritySelector`** runs children in order until one succeeds; an
**`Action`** does the work and returns `RunStatus.Success`/`Failure`/`Running`. The whole
tree is built once and ticked on a timer.

```csharp
// Built once (CreateTree). Reads almost verbatim from BasicFlaskRoutine.CreateTree():
Tree = new Decorator(_ => CanTick() && ReadPlayerState(),
    new PrioritySelector(
        new Decorator(_ => Settings.AutoFlask,
            new PrioritySelector(
                CreateInstantHpComposite(),     // emergency instant-HP first
                CreateHpComposite(),
                CreateManaComposite())),
        CreateAilmentComposite(),               // remove freeze/bleed/etc.
        CreateDefensiveComposite()));           // steelskin / molten shell ...

// A leaf: "HP below threshold AND no life-flask buff -> press the life-flask key".
private Composite CreateHpComposite() =>
    new Decorator(_ => _hpPct < Settings.HpThreshold &&
                       !(_buffs?.Any(b => b.Name == "flask_effect_life") ?? false),
        new UseFlaskAction(FlaskActions.Life));   // Action subclass that presses the bound key
```

The action presses a key. `BasicFlaskRoutine`'s `UseHotkeyAction` posts `WM_KEYDOWN`/`WM_KEYUP`
to the window via P/Invoke; the **fork-native equivalent** is to drive `Input` directly (no
P/Invoke needed — `Input` already wraps Win32). See [input-automation.md](input-automation.md)
and [input.md](../input.md):

```csharp
protected override RunStatus Run(object context)
{
    var key = ResolveKeyForAction();           // e.g. Settings.LifeFlaskKey (a HotkeyNode -> Keys)
    if (key == null) return RunStatus.Failure;
    Input.KeyPressRelease(key.Value);          // fork: Input.KeyPressRelease(Keys) is rate-limited (~10ms)
    return RunStatus.Success;
}
```

Tick the tree off a timer rather than every render frame, so a slider can throttle it
(`BasicFlaskRoutine` runs it from a `Coroutine` at `Settings.TicksPerSecond`):

```csharp
// In Initialise: run the tree at N ticks/sec on a coroutine (utilities.md).
// The (Action, IYieldBase, IPlugin, name) ctor makes an infinite-loop coroutine; register
// it with a runner so the engine ticks it. BasicFlaskRoutine uses Core.ParallelRunner.Run.
var period = new WaitTime(1000 / Settings.TicksPerSecond);
var treeCoroutine = new Coroutine(() => TickTree(Tree), period, this, "FlaskTree");
Core.ParallelRunner.Run(treeCoroutine);

private void TickTree(Composite root)
{
    if (!Settings.Enable || root == null) return;
    if (root.LastStatus != RunStatus.Running) { root.Start(null); }
    root.Tick(null);                           // walks Decorators -> Selectors -> Actions
}
```

> `Composite`/`Decorator`/`PrioritySelector`/`Action`/`RunStatus` are the plugin's own
> bundled TreeSharp classes (`TreeRoutine/TreeSharp/*`), **not** engine types — copy them into
> your plugin. `Coroutine`/`WaitTime` and `Job` *are* engine types (see
> [utilities.md](../utilities.md)).

### (b) Direct handlers — `BuffUtil`

No tree. `Render()` snapshots state, then calls a flat list of `HandleX()` methods; each is a
self-contained guard chain ending in a key press. This is the simplest pattern and the
easiest to read.

```csharp
public override void Render()
{
    if (!ReadPlayerState()) return;            // OnPreExecute: snapshot once, bail if dead/invalid
    HandleBloodRage();
    HandleSteelSkin();
    HandleMoltenShell();
    // ... one method per routine ...
}

private DateTime? _lastSteelSkinCast;

private void HandleSteelSkin()
{
    if (!Settings.SteelSkin) return;                                   // routine enabled?
    if (_lastSteelSkinCast is { } t && DateTime.UtcNow - t < TimeSpan.FromSeconds(2)) return; // cooldown
    if (_hpPct > Settings.SteelSkinMaxHp) return;                      // condition: only when hurt
    if (HasBuff("steelskin_buff") == true) return;                    // already active -> skip
    if (!SkillUsable("SteelSkin")) return;                            // skill slotted & off cooldown

    Input.KeyPressRelease(Settings.SteelSkinKey.Value);              // act (input.md)
    _lastSteelSkinCast = DateTime.UtcNow;
}

private bool SkillUsable(string internalName) =>
    _skills?.Any(s => string.Equals(s.Name, internalName, StringComparison.OrdinalIgnoreCase)
                      && s.CanBeUsed) ?? false;                        // ActorSkill.Name / .CanBeUsed
```

> `BuffUtil` uses the external `WindowsInput.InputSimulator`
> (`inputSimulator.Keyboard.KeyPress((VirtualKeyCode)key)`) instead of `Input`. That library
> is **not** part of this fork — use `Input.KeyPressRelease` / `Input.KeyDown`/`KeyUp`
> ([input.md](../input.md)). It also reads stacks via `Buff.BuffCharges` (here: `Buff.Charges`)
> and (for Blade Flurry/Scourge Arrow channel stacks) inspects buff charges the same way.

### (c) Generic rule engine — `ReAgent`

`ReAgent` doesn't hard-code routines. It builds a **`RuleState`** snapshot each tick and lets
the *user* write the condition as a text expression (Dynamic LINQ / Roslyn) that is compiled
once and evaluated against the state. A rule returns a **side effect** — most commonly "press
this key". Conceptually:

```
user types:   Vitals.HP.Percent < 35 && !Buffs.Has("flask_effect_life")
engine does:  compile -> evaluate(state) -> if true emit PressKeySideEffect(key)
```

The reusable idea, ported to this fork: expose a plain snapshot object that mirrors the
player, then evaluate predicates against it. A fork-native, hard-typed equivalent of
`ReAgent`'s `RuleState`/`VitalsInfo`:

```csharp
public sealed class PlayerSnapshot
{
    public float HpPercent, EsPercent, ManaPercent;
    public Func<string, bool> HasBuff;       // wraps Life.HasBuff
    public Func<string, int>  BuffStacks;    // wraps Life.Buffs -> Buff.Charges

    public static PlayerSnapshot From(GameController gc)
    {
        var life = gc.Player?.GetComponent<Life>();
        if (life == null) return null;
        return new PlayerSnapshot
        {
            HpPercent   = life.HPPercentage * 100f,
            EsPercent   = life.ESPercentage * 100f,
            ManaPercent = life.MPPercentage * 100f,
            HasBuff     = life.HasBuff,
            BuffStacks  = n => (byte)(life.Buffs?.FirstOrDefault(b => b.Name == n)?.Charges ?? 0),
        };
    }
}

// A rule = (predicate over snapshot, key to press). Evaluate them every tick:
private readonly List<(Func<PlayerSnapshot, bool> When, Keys Key)> _rules = new();

private void EvaluateRules()
{
    var s = PlayerSnapshot.From(GameController);
    if (s == null) return;
    if (!_canPressKey()) return;                       // global rate limit (see below)
    foreach (var (when, key) in _rules)
        if (when(s)) { Input.KeyPressRelease(key); break; }  // one key per tick, like ReAgent
}

// Example rule registration:
_rules.Add((s => s.HpPercent < 35 && !s.HasBuff("flask_effect_life"), Settings.LifeFlaskKey));
```

The full `ReAgent` engine compiles the predicate string with `System.Linq.Dynamic.Core`
(`DynamicExpressionParser.ParseLambda<RuleState, bool>`) or a Roslyn `ScriptFunc<bool>`,
caches the compiled delegate, and only re-presses after a global cooldown. Its key-press is a
deferred **`PressKeySideEffect`**: rules merely *request* a key, and the plugin applies at
most one per tick under a `GlobalKeyPressCooldown` gate — a good idea worth copying so two
rules can't fight over the keyboard.

> `ReAgent`'s state object marks every queryable member with an `[Api]` attribute, exposes
> `Vitals.HP.Percent`, `Buffs.Has(...)`, `Flasks[i].CanBeUsed`, `MonsterCount(range, rarity)`,
> `IsKeyPressed`, `SinceLastActivation`, timers/flags, etc. It depends on upstream-only
> symbols: `player.TryGetComponent<Buffs>(out ...)`, `Life.Health/.Mana/.EnergyShield`,
> `Buff.DisplayName/.FlaskSlot`, `Actor.AnimationController`, `Base.Info.BaseItemTypeDat`,
> `HotkeyNodeV2`/`HotkeyNodeValue`, and `ServerData.PartyMembers`/`WorldMousePositionNum`.
> Re-point each to the fork member named above, or to the gap row in
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). The compiled-
> expression machinery (Dynamic LINQ / Roslyn) is third-party, not engine API.

## Step 4 — pressing the key safely

All three plugins funnel through one rate-limited press so routines can't spam. On this fork,
use [`Input`](../input.md) (see [input-automation.md](input-automation.md) for humanized
timing and key-hold patterns):

- `Input.KeyPressRelease(Keys)` — rate-limited (~10 ms) down/up toggle; call repeatedly.
- `Input.KeyDown(Keys)` / `Input.KeyUp(Keys)` — for held inputs (channelled skills, hold-to-move).
- `IEnumerator Input.KeyPress(Keys)` — a coroutine that presses, waits, releases; `yield return` it.

Bind keys with a `HotkeyNode` setting (`Settings.SteelSkinKey`), which converts implicitly to
`Keys`. If you read it with cached `Input.IsKeyDown`, register it in `Initialise`
(`Input.RegisterKey`); `Input.KeyPressRelease` and `GetKeyState` need no registration. Always
gate on `GameController.Window.IsForeground()` before pressing — never drive input into a
background window.

```csharp
// Single global gate shared by every routine (mirrors ReAgent's GlobalKeyPressCooldown):
private readonly Stopwatch _sinceKey = Stopwatch.StartNew();
private bool CanPressKey() =>
    GameController.Window.IsForeground() &&
    _sinceKey.ElapsedMilliseconds >= Settings.GlobalKeyPressCooldown;

// On a successful press: _sinceKey.Restart();
```

## Member quick-reference (this fork)

| Need | Fork member | File |
| --- | --- | --- |
| Local player entity | `GameController.Player` (== `IngameState.Data.LocalPlayer`) | `Core/GameController.cs`, `Core/PoEMemory/MemoryObjects/IngameData.cs` |
| HP/ES/Mana % | `Life.HPPercentage` / `ESPercentage` / `MPPercentage` | `Core/PoEMemory/Components/Life.cs` |
| Raw pools | `Life.CurHP`/`MaxHP`, `CurES`/`MaxES`, `CurMana`/`MaxMana` | `Core/PoEMemory/Components/Life.cs` |
| Has a buff | `Life.HasBuff(string)` | `Core/PoEMemory/Components/Life.cs` |
| Buff list | `Life.Buffs` (or `Entity.Buffs`) | `Life.cs`, `Core/PoEMemory/MemoryObjects/Entity.cs` |
| Buff stacks / time | `Buff.Charges` / `Buff.Timer` / `Buff.MaxTime` | `Core/PoEMemory/Components/Buff.cs` |
| Skills + usability | `Actor.ActorSkills` → `ActorSkill.CanBeUsed`/`.Name` | `Core/PoEMemory/Components/Actor.cs` |
| Stats lookup | `Stats.StatDictionary[GameStat]` | `Core/PoEMemory/Components/Stats.cs` |
| Flask inventory | `ServerData.GetPlayerInventoryByType(InventoryTypeE.Flask)` | `Core/PoEMemory/MemoryObjects/ServerData.cs` |
| Flask charges | `Charges.NumCharges`/`ChargesPerUse`/`ChargesMax` | `Core/PoEMemory/Components/Charges.cs` |
| Is a flask | `entity.GetComponent<Flask>() != null` | `Core/PoEMemory/Components/Flask.cs` |
| Press a key | `Input.KeyPressRelease` / `KeyDown`/`KeyUp` / `KeyPress` | `Core/Input.cs` |
| In game? loading? | `ServerData.IsInGame`, `GameController.IsLoading` | `ServerData.cs`, `GameController.cs` |
| Town/hideout? | `Area.CurrentArea.IsTown` / `.IsHideout` | `Core/AreaInstance.cs` |
| Window focused? | `GameController.Window.IsForeground()` | `Core/GameWIndow.cs` |

## Upstream-only API used by these plugins (do not port verbatim)

See [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) for each.

- `player.GetComponent<Buffs>().BuffsList` and `TryGetComponent<Buffs>(out ...)` — **no `Buffs`
  component, no `TryGetComponent`** here → `Life.Buffs` + null-checked `GetComponent<T>()`.
- `Life.Health` / `Life.Mana` / `Life.EnergyShield` aggregate objects (`ReAgent.VitalsInfo`)
  → flat `Cur*`/`Max*` + `*Percentage`.
- `Buff.DisplayName`, `Buff.BuffCharges`, `Buff.FlaskSlot`, `Buff.SourceEntity`/`SourceSkillId`
  → only `Name`/`Charges`/`Timer`/`MaxTime` exist (`BuffCharges` → `Charges`).
- `Actor.ActorVaalSkills`, `Actor.AnimationController` → not present.
- `AreaInstance.IsPeaceful` → not present; gate on `IsTown`/`IsHideout`. (`Game.IsEscapeState`
  *is* present — use `GameController.Game.IsEscapeState`.)
- `Base.Info.BaseItemTypeDat.ClassName`, `Tincture` component, `LocalStats` (`ReAgent.FlaskInfo`)
  → not present on this fork.
- `HotkeyNodeV2` / `HotkeyNodeValue` (`ReAgent`) → this fork has `HotkeyNode` only ([settings.md](../settings.md)).
- `WindowsInput.InputSimulator` (`BuffUtil`) and `SendMessage` P/Invoke (`BasicFlaskRoutine`)
  → use the engine's `Input` API instead ([input.md](../input.md), [input-automation.md](input-automation.md)).

## Source repos

- `exApiTools/BasicFlaskRoutine` — `TreeRoutine/DefaultBehaviors/Helpers/{PlayerHelper,FlaskHelper,TreeHelper,KeyboardHelper}.cs`,
  `TreeRoutine/DefaultBehaviors/Actions/UseHotkeyAction.cs`, `TreeRoutine/BaseTreeRoutinePlugin.cs`,
  `TreeRoutine/TreeSharp/*`, `BasicFlaskRoutine/BasicFlaskRoutine.cs` (behaviour-tree flask routine).
- `exApiTools/BuffUtil` — `BuffUtil.cs` (`OnPreExecute`/`OnExecute`/`HasBuff` + per-skill `Handle*`),
  `C.cs` (buff/skill name constants).
- `exApiTools/ReAgent` — `Rule.cs` (Dynamic LINQ / Roslyn compile + evaluate), `State/RuleState.cs`,
  `State/{VitalsInfo,BuffDictionary,FlaskInfo,FlasksInfo}.cs`, `SideEffects/PressKeySideEffect.cs`,
  `ReAgent.cs` (snapshot → evaluate → apply one key per tick).
- Verified against this fork: `Core/PoEMemory/Components/{Life,Buff,Charges,Flask,Actor,Stats,Mods,Base}.cs`,
  `Core/PoEMemory/MemoryObjects/{Entity,IngameData,ServerData,ServerInventory}.cs`,
  `Core/{GameController,Input,GameWIndow,AreaInstance,AreaController}.cs`.
