# Entities & the entity list

> One page on the ECS-style entity model: what an `Entity` exposes, how the live entity list is maintained, and how to read game state from plugins. See the [API reference index](README.md).

## The ECS model

ExileCore mirrors the game's own object model: every in-game object (the player,
monsters, chests, ground items, portals, …) is an **`Entity`**, and an entity is
little more than a bag of **components**. The entity itself carries identity and a
handful of convenience properties; everything else (life, position, rarity,
inventory, render data) lives in components you ask for by type.

You never construct entities. The framework reads them from the game process'
memory, caches them, and hands them to you through the
[`GameController`](game-controller.md) / `EntityListWrapper`. To read data you call
a component accessor such as `GetComponent<Life>()`.

`Entity` lives in `ExileCore.PoEMemory.MemoryObjects`. Components live in
`ExileCore.PoEMemory.Components` and derive from `Component` (in
`ExileCore.PoEMemory`). Both ultimately extend `RemoteMemoryObject`, which holds
the backing `Address`.

## Entity

`public class Entity : RemoteMemoryObject` — namespace `ExileCore.PoEMemory.MemoryObjects`.

### Identity & validity

| Member | Type | Notes |
| --- | --- | --- |
| `Address` | `long` | Backing memory address (from `RemoteMemoryObject`). `0` means unbacked. |
| `Id` | `uint` | Stable entity id within the current area. Server-side entities have ids `>= int.MaxValue`. |
| `InventoryId` | `uint` | Inventory slot id, where applicable. |
| `Path` | `string` | Full metadata path, e.g. `Metadata/Monsters/...@N`. `null` / sets `IsValid = false` if it cannot be read. |
| `Metadata` | `string` | `Path` with the trailing `@...` instance suffix stripped. |
| `Type` | `EntityType` | Classification computed from path + components (see [EntityType](#entitytype)). |
| `League` | `LeagueType` | League the entity belongs to (e.g. `Delve`, `Legion`); defaults to `General`. |
| `Rarity` | `MonsterRarity` | `White`..`Unique`; defaults to `White`. |
| `Version` | `uint` | Collection-pass counter used internally to age entities. |
| `IsValid` | `bool` | True while the entity was seen in the latest memory scan and parsed cleanly. Always re-check before reading. |
| `IsAlive` / `IsDead` | `bool` | Backed by `Life.CurHP > 0`. |
| `IsHostile` | `bool` | Cached; from `Positioned.Reaction` (`(Reaction & 0x7f) != 1`). |
| `IsTargetable` | `bool` | From the `Targetable` component. |
| `IsOpened` | `bool` | For chests: open or no longer targetable. |
| `IsHidden` | `bool` | Cached; true when a monster has the `hidden_monster` buff. |

> **Which of these stand on measured offsets (2026-09-16 build).** `Path`, `Metadata` and `Id` do:
> the path pair passed a length criterion on 141 entities out of 141, and `Id` sits at `entity+0x88`.
> So do the component reads behind `GridPos`, `Pos`, `RenderName` and `IsAlive`/`IsDead`.
> **`InventoryId` does not.** The offset the fork reads it from (`entity+0x70`) was *refuted* on the
> live client - on one chest that qword is a pointer into the game module, and reading it as a
> `uint32` returns the pointer's low half - and no replacement has been found, so the value this
> property returns is not to be trusted on this build. `IsHostile` reads `Positioned.Reaction`,
> whose offset has not been measured either: a later pass narrowed it to three candidate bytes in
> one qword (`+0x1E0`, `+0x1E2`, `+0x1E3`) but could not choose between them, because telling a
> hostility flag from a faction number needs an **allied monster** and the measured zone had none.
> Until that is settled, `IsHostile` is reading an inherited offset. Counts, criteria and everything
> still open are in [entities-measured.md](entities-measured.md).

### Position, distance & display

| Member | Type | Source component | Notes |
| --- | --- | --- | --- |
| `GridPos` | `Vector2` | `Positioned` | Tile-grid coordinates; the basis for distance. |
| `Pos` | `Vector3` | `Render` | World position (`Z` includes render bounds). |
| `BoundsCenterPos` | `Vector3` | `Render` | Center of the render bounds (`Render.InteractCenter`). |
| `RenderName` | `string` | `Render` | On-screen display name; `"Empty"` until read. |
| `DistancePlayer` | `float` | — | Grid distance from `Entity.Player`; `float.MaxValue` if no player yet. |
| `Distance(Entity other)` | `float` | — | Grid distance between two entities. |
| `Stats` | `Dictionary<GameStat,int>` | `Stats` | Parsed stat dictionary. |
| `Buffs` | `List<Buff>` | `Life` | Cached buff list. |

`Pos`/`GridPos`/`RenderName`/`BoundsCenterPos` are thin wrappers that read the
relevant component each call and fall back to the last cached value when the
entity is not valid. Vector types here are SharpDX (`SharpDX.Vector2/Vector3`).

> Note: there are no `PosNum`/`GridPosNum`/`BoundsNum` (System.Numerics)
> properties or a `Bounds` property on `Entity` in this codebase. Render
> bounds are exposed by the `Render` component as `Render.Bounds`
> (`Vector3`) — see [components-world.md](components-world.md). Some downstream
> plugins written against newer upstream ExileCore use those `*Num` aliases;
> in this fork use the SharpDX members above (and convert if needed).

### Component accessors

| Member | Signature | Notes |
| --- | --- | --- |
| `GetComponent<T>()` | `T GetComponent<T>() where T : Component, new()` | Returns the component or `null`. Cached per entity. |
| `HasComponent<T>()` | `bool HasComponent<T>() where T : Component, new()` | True if the entity owns component `T`. |
| `GetComponentFromMemory<T>()` | `T GetComponentFromMemory<T>() where T : Component, new()` | Re-reads the component from memory (bypasses some staleness); `null` if absent. |
| `CheckComponentForValid<T>()` | `bool CheckComponentForValid<T>() where T : Component, new()` | Verifies the component's `OwnerAddress` matches this entity. |
| `GetHudComponent<T>()` | `T GetHudComponent<T>() where T : class` | Plugin-attached data (see below); `null` if none. |
| `SetHudComponent<T>(T data)` | `void` | Attaches arbitrary plugin data keyed by type `T`. |

`GetHudComponent<T>()` / `SetHudComponent<T>()` are *not* memory components: they
are a per-entity scratch dictionary that lets a plugin stash its own computed
object on an entity and read it back later (e.g. a cached render label).

> **How a component is found on this build - not by name.** There are no component name strings in
> the client's memory to compare `typeof(T).Name` against, and the doubly linked list of
> `{Next, Prev, String, ComponentList}` nodes this fork used to walk is not there either (survey,
> 2026-09-16). What is there is a table of fixed 8-byte slots hanging off the shared `EntityDetails`
> object, and a slot only yields an **index** into the entity's own component pointer array. The
> numeric id in the slot is **not** a component type id - it was measured ambiguous in both
> directions - so a component's type has to be recognised by the component's **own vtable**, checked
> against a measured `type -> RVA` table. That model was cross-checked against the direct
> `Positioned` pointer the entity carries and agreed 50 times out of 50. The numeric id was a
> **proposal** in the survey of the same day, never a table in this repository - there is no
> `GameOffsets/ComponentNameIds.cs` in the tree or anywhere in its history - and the measurement
> refuted it before it could become one. What the measurement did displace *from the code* is the
> `NativeListNodeComponent` walk, whose file it deleted. See
> [entities-measured.md](entities-measured.md); a component type absent from the measured table
> cannot be recognised at all, which is the honest meaning of `GetComponent<T>()` returning `null`
> for it.
>
> **The measurement behind that table stands at 40 `type -> RVA` pairs** - 30 on zone entities,
> 9 item-side components, and `Projectile`, which is held on weaker evidence than the rest - joined
> **by component address** across four passes of one day (the measured zone, dropped items, a fight,
> a town). Every name has exactly one RVA. `GameOffsets/ComponentVtables.cs` is where a pair takes
> effect, so check its `MeasuredPairs` for what this build actually recognises. Types the fork asks
> for that are **not** in it - among them `Shrine`, `Monolith`, `Charges`, `Flask`, `Map`,
> `SkillGem`, `TimerComponent` - return `null` from `GetComponent<T>()` **whatever the entity
> actually carries**. They are unmeasured, not absent: no entity of the right kind was present in
> the passes that were taken.
>
> Two cautions on the newly measured half. The item-side components (`Base`, `Mods`, `Sockets`,
> `Quality`, `Stack`, `RenderItem`, `Weapon`, `Armour`, `LocalStats`) do not live on the ground
> entity at all: they sit on the **item entity**, which has its own entity vtable (`0x35E0358`, not
> the ground `0x3456508`) and is reached through `WorldItem.ItemEntity` - a path now measured at
> `WorldItem + 0x28`, 14 items out of 14. And one vtable was deliberately **left out** of the table:
> `0x35DFB80`, which the oracle names `AttributeRequirements` on armour and weapons but `Usable` on
> currency. One vtable is one type, so one of those names is wrong; entering it would produce a
> false-positive `HasComponent`, and this fork has no `Usable` class to lose.

> Note: this fork does **not** define a `TryGetComponent<T>(out T)` accessor on
> `Entity` (some upstream-targeting plugins call it). The idiomatic pattern here
> is `var c = entity.GetComponent<T>(); if (c != null) { ... }`.

Components are documented separately — combat/vitals in
[components-combat.md](components-combat.md), items in
[components-items.md](components-items.md), and world/positioning in
[components-world.md](components-world.md). Refer there for `Life`, `Positioned`,
`Render`, `Targetable`, `Monster`, etc.

### Statics & events

| Member | Type | Notes |
| --- | --- | --- |
| `Entity.Player` | `static Entity` | The local player; kept in sync by `EntityListWrapper`. |
| `OnUpdate` | `event EventHandler<Entity>` | Fires when the entity's backing address changes. |

## EntityList (memory object)

`EntityList` (`ExileCore.PoEMemory.MemoryObjects`) is the low-level memory object
that walks the game's entity tree and parses raw entity addresses. Plugin authors
rarely touch it directly — its `CollectEntities(...)` coroutine is driven by
`EntityListWrapper`. Its only generally useful surface is `EntitiesProcessed`
(count parsed in the last pass). Reach for `EntityListWrapper` instead.

## EntityListWrapper

`public class EntityListWrapper` — namespace `ExileCore`. Obtain it via
`GameController.EntityListWrapper`. It owns the live entity set, refreshes the
typed/valid lookups each tick, and raises add/remove events.

| Member | Type | Notes |
| --- | --- | --- |
| `Entities` | `ICollection<Entity>` | All currently cached entities (valid and not). Also surfaced as `GameController.Entities`. |
| `OnlyValidEntities` | `List<Entity>` | Entities that passed validity in the last refresh. |
| `NotOnlyValidEntities` | `List<Entity>` | Entities that failed validity in the last refresh. |
| `NotValidDict` | `Dictionary<uint,Entity>` | Invalid entities keyed by `Id`. |
| `ValidEntitiesByType` | `Dictionary<EntityType,List<Entity>>` | Valid entities bucketed by `Type`. Every enum value has a (possibly empty) list — safe to index. |
| `Player` | `Entity` | The local player entity (also `GameController.Player`). |
| `EntitiesVersion` | `uint` | Current collection version counter. |
| `GetEntityById(uint id)` | `static Entity` | Looks up a cached entity by id; `null` if absent. |
| `GetLabelForEntity(Entity e)` | `string` | Resolves the on-screen label text for an entity. |

> Note: there is no `NotOnlyPlayer` member in this codebase. To iterate
> everything except the player, filter `Entities` (e.g.
> `Entities.Where(e => e != GameController.Player)`).

### Add / remove events and the plugin hooks

`EntityListWrapper` exposes four `event Action<Entity>`:

| Event | Fires when |
| --- | --- |
| `EntityAddedAny` | Any entity is added to the cache. |
| `EntityAdded` | A *gameplay-relevant* entity is added — specifically one whose `(int)Type >= 100` (monsters, chests, items, …; see the `EntityType` numbering below). |
| `EntityRemoved` | An entity leaves the cache. |
| `EntityIgnored` | An entity is filtered out / ignored. |

The flow each refresh (`RefreshState`): newly collected entities are popped off
an internal stack, filtered (errors and, when disabled, server entities are
dropped), then `EntityAddedAny` fires for each; `EntityAdded` fires additionally
only when `(int)entity.Type >= 100`. Removals are drained from a delete queue and
raise `EntityRemoved`. The `PluginManager` subscribes to these and fans them out
to every loaded plugin, so in a plugin you simply **override the matching
method** on `BaseSettingsPlugin` rather than subscribing yourself:

```csharp
public override void EntityAdded(Entity entity) { /* gameplay entity appeared */ }
public override void EntityAddedAny(Entity entity) { /* any entity appeared */ }
public override void EntityRemoved(Entity entity) { /* entity gone */ }
public override void EntityIgnored(Entity entity) { /* entity filtered out */ }
```

(The corresponding contract is `IPlugin.EntityAdded/EntityRemoved/EntityAddedAny/EntityIgnored`.)

## EntityType

`enum EntityType` — namespace `ExileCore.Shared.Enums`. The numbering matters: the
first few values (`Error`, `None`, `ServerObject`, `Effect`, `Light`) are
`< 100`, while gameplay objects start at `Monster = 100` and count up
(`Chest`, `SmallChest`, `Npc`, `Shrine`, `AreaTransition`, `Portal`,
`QuestObject`, `Stash`, `Waypoint`, `Player`, `Pet`, `WorldItem`, …, `Item`,
`Terrain`, `MiscellaneousObjects`). This `>= 100` boundary is exactly what gates
the `EntityAdded` hook above. For the full list and every other framework enum,
see [enums.md](enums.md).

> **Not every `EntityType` value is reachable on this build, and that is a measurement gap, not a
> rule of the game.** `Entity.ParseType` decides most types by asking for a component, so a type
> whose gating component has no measured vtable can never be returned. As of the 2026-09-16 pass,
> `MinimapIcon`, `AreaTransition` and `HideoutDoodad` **are** measured, which makes
> `EntityType.AreaTransition`, `Waypoint`, `IngameIcon` and `HideoutDecoration` reachable - the whole
> `MinimapIcon` branch was dead before, since every case inside it sits behind
> `HasComponent<MinimapIcon>()`. The town pass added `Portal` and `NPC`, which makes
> `EntityType.Portal` / `TownPortal` and `Npc` reachable too. Still unreachable for the same reason:
> `Shrine` (needs `Shrine`) and `Monolith` (needs `Monolith`) - no zone with a shrine has been
> measured yet. See [entities-measured.md](entities-measured.md).

## Examples

### Enumerate hostile monsters and read their life

```csharp
foreach (var monster in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
{
    if (!monster.IsValid || !monster.IsAlive || !monster.IsHostile)
        continue;

    var life = monster.GetComponent<Life>();
    if (life == null)
        continue;

    DebugWindow.LogMsg($"{monster.RenderName}: {life.CurHP}/{life.MaxHP} @ {monster.DistancePlayer:F0}");
}
```

This mirrors PickItV2's "enemy nearby" check, which iterates
`GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]` and
filters with `x.IsValid && x.IsHostile && x.IsAlive` before reading components
(`PickIt.cs`).

### Filter by EntityType / require a component

```csharp
// Only chests, and only ones we can still open.
var openableChests = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Chest]
    .Where(e => e.HasComponent<Chest>() && !e.IsOpened);
```

Adapted from PickItV2's chest predicate `entity.HasComponent<Chest>()`
(`PickIt.cs`) and WhereAreYouGoing's per-type iteration over
`ValidEntitiesByType[EntityType.Monster]` (`WhereAreYouGoing.cs`).

### React in EntityAdded

```csharp
public override void EntityAdded(Entity entity)
{
    // Only gameplay entities (Type >= 100) reach this hook.
    if (entity.Type != EntityType.WorldItem)
        return;

    var item = entity.GetComponent<WorldItem>();
    if (item != null)
        DebugWindow.LogMsg($"Dropped: {entity.RenderName}");
}
```

## Source

- `Core/PoEMemory/MemoryObjects/Entity.cs`
- `Core/PoEMemory/MemoryObjects/EntityList.cs`
- `Core/EntityListWrapper.cs`
- `Core/PoEMemory/Component.cs`
- `Core/PoEMemory/RemoteMemoryObject.cs`
- `Core/Shared/Enums/EntityType.cs`
- `Core/BaseSettingsPlugin.cs`, `Core/Shared/Interfaces/IPlugin.cs`, `Core/Shared/PluginManager.cs` (entity hooks)
