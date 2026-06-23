# Components: world objects & interactables

Components describing where an entity sits in the world and how a plugin can interact with it (chests, portals, transitions, shrines, league mechanics, minimap icons, blockages). All live in namespace `ExileCore.PoEMemory.Components` and derive from `Component`. Obtain one from an entity with `entity.GetComponent<T>()` (returns `null` if absent); `entity.HasComponent<T>()` tests for presence — see [entities.md](entities.md).

[API reference index](README.md)

> Coordinate conventions (grid vs. world units, `Positioned.GridPos` vs. `Render.Pos`, screen projection) are explained once in [coordinates.md](coordinates.md). This page documents component members only; it does not repeat the coordinate math.
>
> Related component pages: [components-combat.md](components-combat.md) (Life, Stats, Buffs, Actor, …) and [components-items.md](components-items.md) (Base, Mods, Sockets, …).

Every component also inherits two members from the `Component` base class:

| Member | Type | Note |
| --- | --- | --- |
| `OwnerAddress` | `long` | Memory address of the owning `Entity`. |
| `Owner` | `Entity` | The entity that owns this component. |

---

## Positioning & rendering

### Positioned

File: `Core/PoEMemory/Components/Positioned.cs`. Grid/world coordinates, rotation, and the reaction byte used to tell friend from foe. The most commonly fetched component for any on-map entity.

| Property | Type | Note |
| --- | --- | --- |
| `GridX` / `GridY` | `int` | Integer grid cell coordinates. |
| `GridPos` | `Vector2` | Grid position as a float vector (`new(GridX, GridY)`). |
| `GridPosI` | `Vector2i` | Grid position as an integer vector. |
| `GridPosition` | `Vector2` | Sub-grid (fractional) position. |
| `WorldX` / `WorldY` | `float` | World-space X/Y coordinates. |
| `WorldPos` | `Vector2` | World position as a vector. |
| `Rotation` | `float` | Facing rotation, in radians. |
| `RotationDeg` | `float` | Facing rotation converted to degrees. |
| `Reaction` | `byte` | Reaction value used to determine hostility. |
| `PositionedStruct` | `PositionedComponentOffsets` | Raw offsets struct (frame-cached). |

Usage: `var p = entity.GetComponent<Positioned>(); var cell = p.GridPos;` (Radar resolves entity grid cells through `GetComponent<Positioned>()`).

### Render

File: `Core/PoEMemory/Components/Render.cs`. World render position, model bounds/height, rotation, and the entity's render name. Use `Pos` (plus `Height`) when projecting an entity to the screen via the camera.

| Property | Type | Note |
| --- | --- | --- |
| `Pos` | `Vector3` | World render position. |
| `X` / `Y` / `Z` | `float` | Components of `Pos`. |
| `Bounds` | `Vector3` | Model bounding-box size. |
| `InteractCenter` | `Vector3` | `Pos + Bounds / 2` — center used for interaction. |
| `Height` | `float` | Model height (`0` when below the `0.01` threshold). |
| `TerrainHeight` | `float` | Terrain height at the entity (same source as `Height`). |
| `Name` | `string` | Render name (cached). |
| `Rotation` | `Vector3` | Model rotation. |
| `MeshRoration` | `Vector3` | Mesh rotation (same value as `Rotation`; spelling preserved from source). |
| `RenderStruct` | `RenderComponentOffsets` | Raw offsets struct (frame-cached); exposes `Height` for screen projection. |

Usage: `var r = player.GetComponent<Render>(); var world = new Vector3(r.Pos.X, r.Pos.Y, r.RenderStruct.Height); var screen = camera.WorldToScreen(world);` (adapted from Radar's route drawing, where the entity is projected from its render position using the render struct's height).

> Some downstream plugin forks rename these to `GridPosNum` / `PosNum` / `BoundsNum`. This repo exposes the names above (`GridPos`, `Pos`, `Bounds`).

### Animated

File: `Core/PoEMemory/Components/Animated.cs`. Bridges an animated in-game object to the entity backing its animation.

| Property | Type | Note |
| --- | --- | --- |
| `BaseAnimatedObjectEntity` | `Entity` | The base animated object entity. |

Usage: `entity.GetComponent<Animated>()?.BaseAnimatedObjectEntity` to reach the underlying animated entity.

---

## Interactables

### Chest

File: `Core/PoEMemory/Components/Chest.cs`. State of a chest or strongbox. Basic flags come from the chest struct; the `Strongbox*`-prefixed flags below are only meaningful for strongboxes.

| Property | Type | Note |
| --- | --- | --- |
| `IsOpened` | `bool` | Chest has been opened. |
| `IsLocked` | `bool` | Chest is locked. |
| `IsStrongbox` | `bool` | Chest is a strongbox. |
| `DestroyingAfterOpen` | `bool` | Strongbox is destroyed after opening. |
| `IsLarge` | `bool` | Strongbox is large. |
| `Stompable` | `bool` | Strongbox can be stomped. |
| `OpenOnDamage` | `bool` | Strongbox opens when damaged. |

Usage: `while (entity.IsValid && entity.GetComponent<Chest>()?.IsOpened != true) { ... }` (Radar's chest-pathfinding loop).

> There is no `Rarity` member on `Chest` in this repo; strongbox/chest rarity comes from other components (e.g. `ObjectMagicProperties`).

### Portal

File: `Core/PoEMemory/Components/Portal.cs`. Destination of a portal object.

| Property | Type | Note |
| --- | --- | --- |
| `Area` | `WorldArea` | World area this portal leads to. |

Usage: `entity.GetComponent<Portal>()?.Area?.Name` to label a portal's destination.

### AreaTransition

File: `Core/PoEMemory/Components/AreaTransition.cs`. Destination area and kind of an area-transition object (zone entrances, waypoints, etc.).

| Property | Type | Note |
| --- | --- | --- |
| `WorldArea` | `WorldArea` | Destination area resolved by memory address. |
| `WorldAreaId` | `int` | Destination area id. |
| `AreaById` | `WorldArea` | Destination area resolved from `WorldAreaId`. |
| `TransitionType` | `AreaTransitionType` | Transition kind (see enum below). |

`AreaTransitionType` (`ExileCore.Shared.Enums`): `Normal = 0`, `Local = 1`, `NormalToCorrupted = 2`, `CorruptedToNormal = 3`, `Labyrinth = 5`.

Usage: `entity.GetComponent<AreaTransition>()?.WorldArea` to read where a transition goes.

### Shrine

File: `Core/PoEMemory/Components/Shrine.cs`. Whether a shrine can currently be activated.

| Property | Type | Note |
| --- | --- | --- |
| `IsAvailable` | `bool` | Shrine is available for use. |

Usage: `entity.GetComponent<Shrine>()?.IsAvailable == true` to highlight usable shrines.

### TriggerableBlockage

File: `Core/PoEMemory/Components/TriggerableBlockage.cs`. Open/closed state and grid bounds of a triggerable blockage (e.g. arena gates, sealed passages).

| Property | Type | Note |
| --- | --- | --- |
| `IsClosed` | `bool` | Blockage is closed. |
| `IsOpened` | `bool` | Blockage is open. |
| `Min` | `Point` | Minimum (top-left) grid corner of the blocked area. |
| `Max` | `Point` | Maximum (bottom-right) grid corner of the blocked area. |
| `Data` | `byte[]` | Raw passability/blockage data spanning the grid area. |

Usage: `entity.GetComponent<TriggerableBlockage>()?.IsClosed` to test whether a passage is sealed.

### Transitionable

File: `Core/PoEMemory/Components/Transitionable.cs`. Transition state flags of objects such as doors and levers.

| Property | Type | Note |
| --- | --- | --- |
| `Flag1` | `byte` | First transition state flag. |
| `Flag2` | `byte` | Second transition state flag. |

Usage: read `Flag1` to detect a door/lever's current state (interpretation is object-specific). This repo exposes `Flag1`/`Flag2`; there is no `CurrentState` member.

---

## League mechanics

### Monolith

File: `Core/PoEMemory/Components/Monolith.cs`. Open progress of a breach/essence monolith.

| Property | Type | Note |
| --- | --- | --- |
| `OpenStage` | `int` | Current open stage of the monolith. |
| `IsOpened` | `bool` | Fully opened (`OpenStage == 4`); the object disappears afterward. |

Usage: `entity.GetComponent<Monolith>()?.IsOpened` to skip monoliths that are already cleared.

### BlightTower

File: `Core/PoEMemory/Components/BlightTower.cs`. Identity and icon of a Blight tower, read from its data record. All members are cached after first read.

| Property | Type | Note |
| --- | --- | --- |
| `Id` | `string` | Data id of the tower. |
| `Name` | `string` | Display name. |
| `Icon` | `string` | Icon path. |
| `IconFileName` | `string` | Icon file name without extension. |

Usage: `var t = entity.GetComponent<BlightTower>(); if (t != null) { var id = t.Id; }` (the Blight plugin resolves per-tower settings off the tower's `Id`).

### ClientBetrayalChoice

File: `Core/PoEMemory/Components/ClientBetrayalChoice.cs`. Marker component for a client-side Betrayal (Immortal Syndicate) choice. No public members beyond the base class; use its presence to detect Betrayal choice entities.

### DelveLight

File: `Core/PoEMemory/Components/DelveLight.cs`. Marker component indicating the entity provides Delve light radius. No public members beyond the base class.

### Sector

File: `Core/PoEMemory/Components/Sector.cs`. Marker component associated with a map sector. No public members beyond the base class.

---

## Map, minimap & NPCs

### MinimapIcon

File: `Core/PoEMemory/Components/MinimapIcon.cs`. Visibility and name of an entity's minimap icon.

| Property | Type | Note |
| --- | --- | --- |
| `IsVisible` | `bool` | Icon is visible. |
| `IsHide` | `bool` | Icon is hidden. |
| `Name` | `string` | Minimap icon name (cached). |
| `TestString` | `string` | Raw icon name string read from memory. |

Usage: `if (entity.GetComponent<MinimapIcon>() is { IsVisible: true } icon) { var n = icon.Name; }` to read an entity's minimap label.

### NPC

File: `Core/PoEMemory/Components/NPC.cs`. NPC indicator state (overhead icon, minimap label).

| Property | Type | Note |
| --- | --- | --- |
| `HasIconOverhead` | `bool` | NPC has an overhead icon. |
| `IsIgnoreHidden` | `bool` | NPC is flagged to be ignored when hidden. |
| `IsMinMapLabelVisible` | `bool` | NPC's minimap label is visible. |

Usage: `entity.GetComponent<NPC>()?.HasIconOverhead` to find NPCs flagged with an overhead marker.

### HideoutDoodad

File: `Core/PoEMemory/Components/HideoutDoodad.cs`. Marker component for a hideout decoration (doodad). No public members beyond the base class; use its presence to identify hideout decorations.

---

## Other / low-level

### DeployedObject

File: `Core/PoEMemory/Components/DeployedObject.cs`. A single object deployed by an actor (e.g. a minion or mine), resolved to its entity. Note: this type is a `RemoteMemoryObject` exposed through `Actor` (see [components-combat.md](components-combat.md)), not a `Component` fetched via `GetComponent<T>()`.

| Property | Type | Note |
| --- | --- | --- |
| `ObjectId` | `ushort` | Entity id of the deployed object. |
| `SkillKey` | `ushort` | Key of the skill that deployed it. |
| `Entity` | `Entity` | Backing entity (resolved and cached on first access). |

Usage: iterate `Actor.DeployedObjects` and read each `.Entity` to track minions/mines.

### ClientAnimationController

File: `Core/PoEMemory/Components/ClientAnimationController.cs`. Current animation key of a client-side animation controller.

| Property | Type | Note |
| --- | --- | --- |
| `AnimKey` | `int` | Active animation key. |

Usage: `entity.GetComponent<ClientAnimationController>()?.AnimKey` to detect animation-driven state changes.

### WorldDescription

File: `Core/PoEMemory/Components/WorldDescription.cs`. Marker component associated with an entity's world description. No public members beyond the base class.

### Preload

File: `Core/PoEMemory/Components/Preload.cs`. Marker component associated with preloaded entities. No public members beyond the base class.

---

## Source

- `Core/PoEMemory/Component.cs` — base `Component` (`Owner`, `OwnerAddress`).
- `Core/PoEMemory/Components/Positioned.cs`, `Render.cs`, `Animated.cs`
- `Core/PoEMemory/Components/Chest.cs`, `Portal.cs`, `AreaTransition.cs`, `Shrine.cs`, `TriggerableBlockage.cs`, `Transitionable.cs`
- `Core/PoEMemory/Components/Monolith.cs`, `BlightTower.cs`, `ClientBetrayalChoice.cs`, `DelveLight.cs`, `Sector.cs`
- `Core/PoEMemory/Components/MinimapIcon.cs`, `NPC.cs`, `HideoutDoodad.cs`
- `Core/PoEMemory/Components/DeployedObject.cs`, `ClientAnimationController.cs`, `WorldDescription.cs`, `Preload.cs`
- `Core/Shared/Enums/AreaTransitionType.cs`
- `GameOffsets/PositionedComponentOffsets.cs`, `GameOffsets/RenderComponentOffsets.cs`
- Plugin cross-check: instantsc/Radar (`Radar.cs`, `Radar.Pathfinding.cs` — `Positioned`/`Render`/`Chest`), DetectiveSquirrel/Blight (`Main.cs` — `BlightTower`).
