# Components: combat & character

Combat- and character-related components read from game memory. All live in the `ExileCore.PoEMemory.Components` namespace and are retrieved from an entity via `Entity.GetComponent<T>()` / `Entity.TryGetComponent<T>(out var c)` (see [entities.md](entities.md)). For items/world components see [components-items.md](components-items.md) and [components-world.md](components-world.md).

[API reference index](README.md)

## How to access

Every component derives from `Component` (`Core/PoEMemory/Component.cs`), which adds two members on top of `RemoteMemoryObject`:

| Member | Type | Note |
| --- | --- | --- |
| `OwnerAddress` | `long` | Address of the owning entity. |
| `Owner` | `Entity` | The entity that owns this component. |

```csharp
// Resistances of a monster (adapted from Guardians-R-Us)
if (entity.TryGetComponent<Stats>(out var stats) &&
    entity.TryGetComponent<Life>(out var life))
{
    var fireRes = stats.StatDictionary.GetValueOrDefault(GameStat.FireDamageResistancePct);
    var hp = life.MaxHP;
}
```

`GameStat`, `ActionFlags`, and `AnimationE` are enums — see [enums.md](enums.md).

---

### Life

File: `Core/PoEMemory/Components/Life.cs`. Health, mana, energy shield, reservation, and the buff list.

| Property | Type | Note |
| --- | --- | --- |
| `CurHP` / `MaxHP` | `int` | Current / maximum life (`MaxHP` defaults to `1` when unread). |
| `ReservedFlatHP` / `ReservedPercentHP` | `int` | Flat / percent life reserved. |
| `CurMana` / `MaxMana` | `int` | Current / maximum mana. |
| `ReservedFlatMana` / `ReservedPercentMana` | `int` | Flat / percent mana reserved. |
| `CurES` / `MaxES` | `int` | Current / maximum energy shield. |
| `HPPercentage` | `float` | Current life as a fraction of *unreserved* max life. |
| `MPPercentage` | `float` | Current mana as a fraction of *unreserved* max mana. |
| `ESPercentage` | `float` | `CurES / MaxES`, or `0` when `MaxES == 0`. |
| `Buffs` | `List<Buff>` | Buffs/debuffs currently on the entity (per-frame cached). |
| `OwnerAddress` | `long` | Owning entity address. |

Methods: `bool HasBuff(string name)` (case-sensitive name match), `List<Buff> ParseBuffs()` (raw re-read).

There is no `Health`/`EnergyShield`/`Mana` aggregate sub-object; use the flat `CurHP`/`MaxHP`, `CurES`/`MaxES`, `CurMana`/`MaxMana` pairs and the `*Percentage` helpers.

```csharp
// adapted from Guardians-R-Us: header showing a monster's pools
entity.TryGetComponent<Life>(out var life);
var text = $"Life:{life?.MaxHP:#,##0} / ES:{life?.MaxES:#,##0} / Mana:{life?.MaxMana:#,##0}";
```

### Buff

File: `Core/PoEMemory/Components/Buff.cs`. A single buff/debuff. Not fetched as a component directly — obtain instances from `Life.Buffs` (or test presence with `Life.HasBuff`). `Buff` is a `RemoteMemoryObject`, not a `Component`.

| Property | Type | Note |
| --- | --- | --- |
| `Name` | `string` | Buff/aura name (lazy, retried on empty reads). |
| `Charges` | `byte` | Stacks currently on the buff. |
| `Timer` | `float` | Time remaining. |
| `MaxTime` | `float` | Total duration; `infinity` for auras / always-on buffs. |
| `BuffOffsets` | `BuffOffsets` | Raw offsets struct (cached after first read). |

A list of buffs is exposed only through `Life.Buffs` (there is no `Buffs` component). To enumerate:

```csharp
if (entity.TryGetComponent<Life>(out var life))
    foreach (var b in life.Buffs ?? Enumerable.Empty<Buff>())
        if (b.Name == "frozen") { /* ... */ }
```

### Stats

File: `Core/PoEMemory/Components/Stats.cs`. Game stats keyed by `GameStat`.

| Member | Type | Note |
| --- | --- | --- |
| `StatDictionary` | `Dictionary<GameStat, int>` | Parsed stat-id → value map (per-frame cached). |
| `StatsCount` | `long` | Number of stats on the entity. |
| `StatsComponent` | `StatsComponentOffsets` | Raw offsets struct (cached per frame). |
| `OwnerAddress` | `long` | Owning entity address. |

Methods: `Dictionary<GameStat, int> ParseStats()` (raw re-read), `Dictionary<string, int> HumanStats()` (resolves ids to human-readable stat names via game files).

```csharp
// adapted from Guardians-R-Us
entity.TryGetComponent<Stats>(out var stats);
int fireRes = stats.StatDictionary.GetValueOrDefault(GameStat.FireDamageResistancePct);
```

### Actor

File: `Core/PoEMemory/Components/Actor.cs`. The entity's current action, animation, deployed objects, and skill list. Present on the local player and on monsters.

| Member | Type | Note |
| --- | --- | --- |
| `ActionId` | `short` | Raw action bit-field. |
| `Action` | `ActionFlags` | Current action flags (`[Flags]` enum). |
| `isMoving` | `bool` | `(Action & ActionFlags.Moving) > 0`. |
| `isAttacking` | `bool` | `(Action & ActionFlags.UsingAbility) > 0`. |
| `AnimationId` | `int` | Raw animation id. |
| `Animation` | `AnimationE` | Current animation enum. |
| `CurrentAction` | `Actor.ActionWrapper` | Live action info — see note. Can be `null`. |
| `DeployedObjects` | `List<DeployedObject>` | Minions / mines / totems this actor deployed. |
| `DeployedObjectsCount` | `long` | Count of deployed objects. |
| `ActorSkills` | `List<ActorSkill>` | Skills available to this actor. |

> The actor type exposes **no `ActorVaalSkills` property**. Vaal-skill data is read through the [`ActorVaalSkill`](#skill-memory-objects) memory object reached from an `ActorSkill`. `CurrentAction` points at a memory location that "changes a lot" — wrap accesses to it (and its fields) in try/catch.

`Actor.ActionWrapper` (nested `RemoteMemoryObject`) exposes the in-flight action:

| Property | Type | Note |
| --- | --- | --- |
| `Destination` / `DestinationX` / `DestinationY` | `Vector2` / `float` / `float` | Action destination. |
| `CastDestination` | `Vector2` | Cast target position. |
| `Target` | `Entity` | The action's target entity. |
| `Skill` | `ActorSkill` | Skill being used. |

```csharp
// adapted from Where-Are-You-Going: react to a monster's current action
entity.TryGetComponent<Actor>(out var actor);
if (actor != null && (actor.Action & ActionFlags.Moving) != 0)
{
    // entity is moving
}
```

### Player

File: `Core/PoEMemory/Components/Player.cs`. Player-specific character data (on the local player entity).

| Property | Type | Note |
| --- | --- | --- |
| `PlayerName` | `string` | Character name. |
| `XP` | `uint` | Total experience. |
| `Level` | `int` | Character level (defaults to `1`). |
| `Strength` / `Dexterity` / `Intelligence` | `int` | Core attributes. |
| `HideoutLevel` | `int` | Hideout level. |
| `PantheonMajor` / `PantheonMinor` | `PantheonGod` | Chosen pantheon gods. |
| `PropheciesCount` | `byte` | Number of sealed prophecies. |
| `Prophecies` | `IList<ProphecyDat>` | Sealed prophecies. |
| `Hideout` | `HideoutWrapper` | Hideout wrapper. |
| `AllocatedLootId` | `int` | Allocated loot id. |

Methods: `bool IsTrialCompleted(...)` overloads (by trial id `string`, `LabyrinthTrial`, or `WorldArea`); debug helper `TrialStates`.

> There is **no `PartyStatus` on the `Player` component** — party state lives on `ServerData` (`PartyStatusType`, of enum type `PartyStatus`), not here.

### Monster

File: `Core/PoEMemory/Components/Monster.cs`. Empty marker component — its mere presence identifies the entity as a monster. No public members.

```csharp
bool isMonster = entity.HasComponent<Monster>();
```

### StateMachine

File: `Core/PoEMemory/Components/StateMachine.cs`. Targeting state from the entity's state machine.

| Property | Type | Note |
| --- | --- | --- |
| `CanBeTarget` | `bool` | Entity can be targeted (`byte == 1`). |
| `InTarget` | `bool` | Entity is currently targeted. |

> This component does **not** expose a `States` / `StateMachine` list — only the two targeting flags above.

### Charges

File: `Core/PoEMemory/Components/Charges.cs`. Charge state of a flask or other charge-using item.

| Property | Type | Note |
| --- | --- | --- |
| `NumCharges` | `int` | Current charges. |
| `ChargesPerUse` | `int` | Charges consumed per use. |
| `ChargesMax` | `int` | Maximum charges that can be stored. |

### Flask

File: `Core/PoEMemory/Components/Flask.cs`. Empty marker component — present on flask items. No public members; pair with `Charges` for charge data.

### Targetable

File: `Core/PoEMemory/Components/Targetable.cs`. Whether the entity is targetable / targeted.

| Property | Type | Note |
| --- | --- | --- |
| `isTargetable` | `bool` | Entity can be targeted. |
| `isTargeted` | `bool` | Entity is currently targeted. |
| `TargetableComponent` | `TargetableComponentOffsets` | Raw offsets struct (cached per frame). |

> There is **no `isHighlightable`** member on this component.

### DiesAfterTime

File: `Core/PoEMemory/Components/DiesAfterTime.cs`. Empty marker component — present on entities that are destroyed after a period of time (e.g. temporary summons). No public members. Pair with `TimerComponent` for the remaining time.

### Magnetic

File: `Core/PoEMemory/Components/Magnetic.cs`. Magnetic force of an in-game object.

| Property | Type | Note |
| --- | --- | --- |
| `Force` | `int` | Magnetic force value. |

### Beam

File: `Core/PoEMemory/Components/Beam.cs`. Start/end world positions of a beam effect.

| Property | Type | Note |
| --- | --- | --- |
| `BeamStart` | `Vector3` | Beam start (actually the casting entity's world position). |
| `BeamEnd` | `Vector3` | Beam end / target world position. |
| `Unknown1` / `Unknown2` | `int` | Unidentified values (`Unknown1` looks like two bools). |

### TimerComponent

File: `Core/PoEMemory/Components/TimerComponent.cs`. Remaining time on an entity timer.

| Property | Type | Note |
| --- | --- | --- |
| `TimeLeft` | `float` | Time remaining, in seconds. |

### Pathfinding

File: `Core/PoEMemory/Components/Pathfinding.cs`. Movement / pathfinding state.

| Property | Type | Note |
| --- | --- | --- |
| `TargetMovePos` | `Vector2i` | Next grid position being moved toward. |
| `PreviousMovePos` | `Vector2i` | Previous grid position occupied. |
| `WantMoveToPosition` | `Vector2i` | Grid position the entity ultimately wants to reach. |
| `IsMoving` | `bool` | Entity is currently moving (`IsMoving == 2` internally). |
| `StayTime` | `float` | Time spent stationary at the current position. |

> Members are `TargetMovePos` / `PreviousMovePos` / `WantMoveToPosition` / `IsMoving` / `StayTime` — there is no `StayPositions` or `WasInArea` member. Note the capitalized `IsMoving` here vs. the lowercase `isMoving` on `Actor`.

```csharp
// adapted from Where-Are-You-Going
entity.TryGetComponent<Pathfinding>(out var path);
if (path != null && path.IsMoving)
{
    var target = path.WantMoveToPosition; // grid cell the unit is heading to
}
```

---

## Skill memory objects

The skills exposed by `Actor.ActorSkills` (and `Actor.ActionWrapper.Skill`) are `RemoteMemoryObject`s under `ExileCore.PoEMemory.MemoryObjects`, not components.

### ActorSkill

File: `Core/PoEMemory/MemoryObjects/ActorSkill.cs`. A single skill on the actor's skill set.

| Member | Type | Note |
| --- | --- | --- |
| `Id` | `ushort` | Internal skill id. |
| `Name` / `InternalName` | `string` | Display / internal skill name. |
| `EffectsPerLevel` | `GrantedEffectsPerLevel` | Granted-effect record (leads to the skill gem). |
| `CanBeUsed` / `CanBeUsedWithWeapon` / `AllowedToCast` | `bool` | Usability flags. |
| `IsOnSkillBar` / `SkillSlotIndex` | `bool` / `int` | Skill-bar membership and slot. |
| `IsUsing` / `PrepareForUsage` / `SkillUseStage` | `bool` / `bool` / `byte` | Cast-stage flags. |
| `Cooldown` / `CastTime` | `float` / `TimeSpan` | Cooldown (seconds) and cast time. |
| `Cost` / `TotalUses` | `int` / `int` | Mana cost / charge-like uses. |
| `SoulsPerUse` / `TotalVaalUses` / `IsVaalSkill` | `int` / `int` / `bool` | Vaal-soul data. |
| `IsTotem` / `IsTrap` | `bool` | Skill behaviour flags. |
| `Dps` / `HundredTimesAttacksPerSecond` | `float` / `int` | Derived from stats. |
| `Stats` | `Dictionary<GameStat, int>` | Per-skill stats; `int GetStat(GameStat)` reads one. |

### ActorVaalSkill

File: `Core/PoEMemory/MemoryObjects/ActorVaalSkill.cs`. Vaal-skill record (names, icon, soul counts): `VaalSkillInternalName`, `VaalSkillDisplayName`, `VaalSkillDescription`, `VaalSkillSkillName`, `VaalSkillIcon` (`string`), and `VaalMaxSouls`, `VaalSoulsPerUse`, `CurrVaalSouls` (`int`).

### ActiveSkillWrapper

File: `Core/PoEMemory/MemoryObjects/ActiveSkillWrapper.cs`. The "active skill" definition behind a gem: `InternalName`, `DisplayName`, `Description`, `SkillName`, `Icon`, `LongDescription`, `AmazonLink` (`string`), plus `CastTypes` and `SkillTypes` (`List<int>`). Reached via `SkillGemWrapper.ActiveSkill`.

### SkillGemWrapper

File: `Core/PoEMemory/MemoryObjects/SkillGemWrapper.cs`. The skill gem record: `Name` (`string`) and `ActiveSkill` (`ActiveSkillWrapper`). Reached from `ActorSkill.EffectsPerLevel.SkillGemWrapper`.

## Source

- Components: `Core/PoEMemory/Components/{Life,Buff,Stats,Actor,Player,Monster,StateMachine,Charges,Flask,Targetable,DiesAfterTime,Magnetic,Beam,TimerComponent,Pathfinding}.cs`
- Component base: `Core/PoEMemory/Component.cs`
- Skill memory objects: `Core/PoEMemory/MemoryObjects/{ActorSkill,ActorVaalSkill,ActiveSkillWrapper,SkillGemWrapper}.cs`
- Enums: `Core/Shared/Enums/{GameStat,ActionFlags,AnimationE,PantheonGod,PartyStatus}.cs`
- Cross-checked against plugins: DetectiveSquirrel/Guardians-R-Us (`Stats`/`Life`/`Actor.DeployedObjects`), DetectiveSquirrel/ExileAPI-WhereAreYouGoing (`Actor.Action`/`Pathfinding`).
