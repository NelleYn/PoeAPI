# Recipe: world-awareness overlays (the DetectiveSquirrel family)

Patterns for overlays that read what monsters, traps, towers and minions are *about to do* and draw it in the world / on the map — distilled from DetectiveSquirrel's plugins and adapted to this fork.

[API reference index](../README.md) · [cookbook index](README.md)

DetectiveSquirrel's overlays (WhereAreYouGoing, WhatAreYouDoing, Blight, Abyss, Guardians-R-Us, AreaStatVisual, Wheres-My-Cursor) all reuse a small set of techniques: read an entity's intended destination, classify hazards by metadata, draw circles/lines in world or map space, and pull per-instance stats. The techniques port cleanly, but several helper methods they call are **upstream-only** in our fork — those are flagged inline and you draw the primitives yourself.

> **Fork API differences (read first).** DetectiveSquirrel's code is written against the compiled upstream ExileApi and leans on members our fork does not have: `Entity.PosNum`/`GridPosNum`, `Render.BoundsNum`/`RotationNum`, `Entity.TryGetComponent<T>(out …)`, and a whole family of `Graphics.DrawCircleInWorld` / `DrawLineInWorld` / `DrawLineOnLargeMap` / `DrawFilledCircleOnLargeMap` helpers, plus `IngameData.GetTerrainHeightAt` / `GetGridScreenPosition` and `Pathfinding.PathingNodes` / `StateMachine.States`. None of those exist here. See [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) for the full SharpDX↔Numerics and member-name table. Every snippet below uses only members verified against `Core/`.

---

## 1. Read where a monster is going (Pathfinding + Actor)

Upstream WhereAreYouGoing reads `Pathfinding.PathingNodes` (the full path the server queued) and `Actor.CurrentAction.Destination` (the cast/move target), then draws a poly-line along the path. **Our fork's `Pathfinding` has no `PathingNodes`** — it exposes the *endpoints* instead, which is enough for a "where is it heading" arrow:

| Fork member (`Core/PoEMemory/Components/Pathfinding.cs`) | Meaning |
| --- | --- |
| `TargetMovePos` : `Vector2i` | Next grid cell it steps to (`ClickToNextPosition`). |
| `WantMoveToPosition` : `Vector2i` | Final grid cell it wants to reach. |
| `PreviousMovePos` : `Vector2i` | Grid cell it came from. |
| `IsMoving` : `bool` | True while moving (`IsMoving == 2` internally). |
| `StayTime` : `float` | Seconds stationary at the current cell. |

The `Actor` component (`Core/PoEMemory/Components/Actor.cs`) carries the *action intent* — this matches upstream closely:

| Member | Meaning |
| --- | --- |
| `Action` : [`ActionFlags`](../enums.md) | Bit-field: `None`, `UsingAbility` (2), `Dead` (64), `Moving` (128), `HasMines` (2048). |
| `isMoving` / `isAttacking` : `bool` | Convenience flag checks. |
| `CurrentAction` : `ActionWrapper` | The in-flight action. **May relocate in memory — wrap accesses in try/catch.** |
| `CurrentAction.Destination` : `Vector2` (grid) | Where the action is aimed. |
| `CurrentAction.Target` : `Entity` | The targeted entity, if any. |
| `CurrentAction.Skill` : `ActorSkill` | The skill being used. |

```csharp
// Draw an arrow from each hostile monster toward where it's heading / aiming.
public override void Render()
{
    var camera = GameController.IngameState.Camera;
    var monsters = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster];

    foreach (var e in monsters)
    {
        if (!e.IsAlive || !e.IsHostile || e.DistancePlayer > 100) continue;

        var pos = e.GetComponent<Positioned>();   // fork: GetComponent, returns null if absent
        var actor = e.GetComponent<Actor>();
        var render = e.GetComponent<Render>();
        if (pos == null || actor == null || render == null) continue;

        // entity foot position in screen space (Render.Pos is SharpDX Vector3, world units + height)
        var from = camera.WorldToScreen(render.Pos);

        Vector2 destGrid;
        if (actor.isAttacking && actor.CurrentAction != null)
        {
            try { destGrid = actor.CurrentAction.Destination; }   // CurrentAction can move in memory
            catch { continue; }
        }
        else if (actor.isMoving)
        {
            var t = e.GetComponent<Pathfinding>()?.WantMoveToPosition ?? default;
            destGrid = new Vector2(t.X, t.Y);   // Vector2i -> Vector2
        }
        else continue;

        // grid -> world (drops to z=0; add terrain height if you have it) -> screen
        var to = camera.WorldToScreen(destGrid.GridToWorld(render.Pos.Z));
        Graphics.DrawLine(from, to, 2, Color.Yellow);
    }
}
```

Notes on the conversions used above (all verified):

- `Camera.WorldToScreen(Vector3)` takes a **SharpDX** `Vector3` and returns a **SharpDX** `Vector2`; it returns `Vector2.Zero` on failure (off-screen / not ready), so guard for `(0,0)`. See [coordinates.md](../coordinates.md#camera).
- `GridToWorld(this Vector2)` / `GridToWorld(this Vector2, float z)` live in `Core/WorldPositionExtensions.cs` and `Core/Shared/Helpers/PoeMapExtension.cs` (`grid / 0.092 + 5.43`). Upstream's `GetTerrainHeightAt(grid)` does not exist here; reuse the moving entity's own `Render.Pos.Z` as the height, or `Render.Height`/`TerrainHeight`.
- `ActionFlags` is a real bit-field, so prefer `actor.isMoving` / `actor.isAttacking` (which mask the flag) over `switch`-ing on raw values. Upstream switches on undocumented composites like `(ActionFlags)4224`; those numbers are build-specific — match on the named flags instead.

See [components-combat.md](../components-combat.md) for `Actor`/`ActorSkill` and [components-world.md](../components-world.md) for `Pathfinding`/`Positioned`/`Render`.

---

## 2. Draw a circle in the world (no built-in helper)

Upstream calls `Graphics.DrawCircleInWorld` / `DrawFilledCircleInWorld`. **Our fork's `Graphics` only has** `DrawLine`, `DrawBox`, `DrawFrame`, `DrawText`, `DrawImage` (verify in `Core/Graphics.cs`). WhatAreYouDoing already ships a hand-rolled fallback that works perfectly here — sample N points on a circle, project each with `WorldToScreen`, connect with `DrawLine`:

```csharp
// Adapted verbatim-in-spirit from WhatAreYouDoing.DrawCircleInWorldPosition.
private void DrawCircleInWorld(Vector3 worldPos, float worldRadius, int thickness, Color color)
{
    const int segments = 24;
    const float step = 2f * MathF.PI / segments;
    var camera = GameController.IngameState.Camera;

    for (var i = 0; i < segments; i++)
    {
        var a = i * step;
        var b = (i + 1) * step;
        var p1 = worldPos + new Vector3(MathF.Cos(a) * worldRadius, MathF.Sin(a) * worldRadius, 0);
        var p2 = worldPos + new Vector3(MathF.Cos(b) * worldRadius, MathF.Sin(b) * worldRadius, 0);
        Graphics.DrawLine(camera.WorldToScreen(p1), camera.WorldToScreen(p2), thickness, color);
    }
}
```

The radius is in **world units**. To draw "N grid tiles" of radius, multiply by `1 / 0.092 ≈ 10.87` (one tile ≈ 10.87 world units — see [coordinates.md](../coordinates.md#grid--world-conversion)). Cull off-screen entities first (the upstream `IsEntityWithinScreen(WorldToScreen(pos), screenRect, allowancePx)` check, reused identically across Blight/Abyss/WhereAreYouGoing) so you are not projecting hundreds of points per frame.

A "draw on the open large map" circle uses the same loop but projects each point with the **Radar large-map transform** (`mapCenter + TranslateGridDeltaToMapDelta(point - player, height)`) instead of `WorldToScreen` — that transform, and the `Map.LargeMapZoom` / `LargeMapShiftX` plumbing every DetectiveSquirrel map overlay copies, is documented in [coordinates.md](../coordinates.md#minimap--large-map-drawing). (Upstream's `Graphics.DrawCircleOnLargeMap` / `DrawLineOnLargeMap` are not in this fork.)

---

## 3. Trap & hazard timing (metadata classification + a timed cache)

WhatAreYouDoing classifies labyrinth traps and Sirus death-zones by **metadata/path string**, then renders danger. The classification pattern ports directly — entities expose `Path` and `Metadata`:

```csharp
// Hazard lookup by Path (Terrain) / Metadata (Monster). Reused verbatim from WhatAreYouDoing.
private static readonly Dictionary<string, float> HazardRadius = new()
{
    ["Metadata/Terrain/Labyrinth/Traps/LabyrinthRoomba"]      = 120f,
    ["Metadata/Terrain/Labyrinth/Traps/LabyrinthSawblade"]    = 50f,
    ["Metadata/Terrain/Labyrinth/Traps/LabyrinthSpinner"]     = 60f,
    ["Metadata/Monsters/InvisibleFire/InvisibleFireOrionDeathZoneStationary"] = 790f,
};
```

Two reusable refinements from the same plugin:

1. **Scale the radius by the entity's own size stat.** Read `Stats` and multiply by `ActorScalePct`:

   ```csharp
   var baseRadius = HazardRadius.GetValueOrDefault(e.Path, 30f);
   var stats = e.GetComponent<Stats>();
   if (stats != null && stats.StatDictionary.TryGetValue(GameStat.ActorScalePct, out var scale))
       baseRadius *= 1f + scale / 100f;
   ```

   `Stats.StatDictionary` (a `Dictionary<GameStat,int>`) is the per-entity stat bag — verified in `Core/PoEMemory/Components/Stats.cs`. See [components-combat.md](../components-combat.md).

2. **A timed cache for one-shot hazards (dart traps).** A dart trap fires, `CurrentAction` changes, then the projectile lives ~1.5 s with no entity to track. WhatAreYouDoing records `(entity.Address, action.Address, destination, duration)` on first sight and fades a line until it expires. The pattern is just a `List` you prune in `Tick()`:

   ```csharp
   record Shot(long EntityAddr, long ActionAddr, Vector2 DestGrid, DateTime Start, TimeSpan Life);
   private readonly List<Shot> _shots = new();

   public override Job Tick()
   {
       _shots.RemoveAll(s => DateTime.Now - s.Start > s.Life);   // prune expired
       return null;
   }
   // On a dart trap's CurrentAction: if no Shot matches (EntityAddr, ActionAddr), add one.
   // While alive, lerp the line color from red→transparent by elapsed/Life.
   ```

   Use addresses as identity keys — `entity.Address` and `actor.CurrentAction.Address` are stable enough within an action's lifetime to dedupe. (Note upstream keyed on `entity.Address` cast to a `long`; in this fork `Entity.Address` is already `long`.)

---

## 4. League/area pathway lines (Blight, Abyss)

Blight and Abyss both reduce to: collect a set of league entities (`EntityAdded`), sort by `Entity.Id`, then connect consecutive ones with a line if they are close enough. This is a clean, reusable shape:

```csharp
public override void EntityAdded(Entity e)
{
    if (e.Metadata.StartsWith("Metadata/MiscellaneousObjects/Abyss/") &&
        e.Metadata != "Metadata/MiscellaneousObjects/Abyss/AbyssNodeMini")
        _nodes.Add(e);
}

public override void Render()
{
    var ordered = _nodes.OrderByDescending(n => n.Id).ToList();   // crevice grows by Id
    for (var i = 0; i < ordered.Count - 1; i++)
    {
        var a = ordered[i];
        var b = ordered[i + 1];
        if (a.Distance(b) > 35) continue;                          // skip jumps between unrelated nodes
        if (a.GridPos == Vector2.Zero || b.GridPos == Vector2.Zero) continue;

        // World line: our fork has no Graphics.DrawLineInWorld — project both endpoints yourself.
        var camera = GameController.IngameState.Camera;
        var pa = camera.WorldToScreen(a.GetComponent<Render>().Pos);
        var pb = camera.WorldToScreen(b.GetComponent<Render>().Pos);
        Graphics.DrawLine(pa, pb, 3, Color.Cyan);
    }
}
```

`Entity.Distance(Entity)` and `Entity.GridPos` (via `Positioned.GridPos`) are verified fork members. Upstream's `Entity.GridPosNum` is the System.Numerics twin of `GridPos` — use `GridPos` (SharpDX) here and convert at the `WorldToScreen` boundary as needed.

**Blight tower radius — partly upstream-only.** Blight's `BlightTower` component exists in our fork (`Core/PoEMemory/Components/BlightTower.cs`, exposing `Id` / `Name` / `Icon` / `IconFileName`), so you can classify towers by `Id` (`"FlameTower1"`, `"MinionTower2"`, …) exactly as the plugin does. But it pulls each tower's **radius** from `GameController.Game.Files.BlightTowers` — and that dat-file accessor is **not in this fork's `FilesContainer`** (verify: it has `Stats`, `Mods`, `ItemClasses`, `QuestStates`, but no `BlightTowers`). Hard-code radii per `Id`, or read the dat yourself via `Core/PoEMemory/FilesFromMemory.cs`. See [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

**State detection — upstream-only.** Blight checks `entity.GetComponent<StateMachine>().States` to know when the pump succeeded or a pathway closed. **Our fork's `StateMachine` only exposes `CanBeTarget` and `InTarget`** (`Core/PoEMemory/Components/StateMachine.cs`) — there is no `States` list. Detect completion another way (e.g. the pump entity disappearing, a buff on the player, or a UI element) until the component gains `States`.

---

## 5. Minion / guardian tracking (Actor.DeployedObjects)

Guardians-R-Us lists your animated guardians by walking `Actor.DeployedObjects` on the local player and filtering by metadata — this maps 1:1 to fork members:

```csharp
var player = GameController.IngameState.Data.LocalPlayer;
var actor = player.GetComponent<Actor>();
if (actor != null)
{
    var guardians = actor.DeployedObjects
        .Where(d => d.Entity is { IsValid: true,
                                  Metadata: "Metadata/Monsters/AnimatedItem/AnimatedArmour" })
        .Select(d => d.Entity)
        .ToList();

    foreach (var g in guardians)
    {
        var life = g.GetComponent<Life>();
        var stats = g.GetComponent<Stats>();
        // life?.MaxHP, life?.MaxES, stats?.StatDictionary[GameStat.FireDamageResistancePct] ...
    }
}
```

Verified: `Actor.DeployedObjects` is `List<DeployedObject>`, and `DeployedObject.Entity` resolves the backing entity by id (`Core/PoEMemory/Components/DeployedObject.cs`). `DeployedObjectsCount` is available if you only need the count. This is the same list used to track mines, totems, and spectres — filter by `Metadata` for whatever you summon. See [components-combat.md](../components-combat.md) for `Actor`/`Life`/`Stats`.

> Guardians-R-Us also calls `GameController.IngameState.Data.ServerData.GetPlayerInventoryByType(InventoryTypeE.AnimatedArmour)` to read the equipped guardian items, and `GameController.InspectObject(entity, …)` for a debug tree. `GetPlayerInventoryByType` is verified in `Core/PoEMemory/MemoryObjects/ServerData.cs`; **`InspectObject` is not present on this fork's `GameController`** — use the built-in debug window / `DebugWindow.LogMsg` instead.

---

## 6. Per-instance area stats (MapStats)

AreaStatVisual reads `IngameState.Data.MapStats` — the `Dictionary<GameStat,int>` describing the *current map instance's* modifiers (pack size, monster damage %, etc.) — and matches each entry against user regexes. The data source is verified (`Core/PoEMemory/MemoryObjects/IngameData.cs`):

```csharp
public override void Render()
{
    var stats = GameController.IngameState.Data.MapStats;   // Dictionary<GameStat,int>, or null out of map
    if (stats == null) return;

    foreach (var (stat, value) in stats)
        Graphics.DrawText($"{stat}: {value}", /* position */ default, Color.White);
}
```

Clear any cache in `AreaChange(AreaInstance)` (the plugin override fired on map change) so stale lines don't survive a portal — AreaStatVisual does exactly this.

> **Translation is upstream-only.** AreaStatVisual's nicest feature is turning a raw `GameStat`+value into readable text via `GameController.Files.StatDescriptions.TranslateMod(values)` (with `MapStatDescriptions` / `HeistEquipmentStatDescriptions` fallbacks). **None of those `*StatDescriptions` files exist on this fork's `FilesContainer`** and there is no `TranslateMod` anywhere in `Core/`. You can still show the `GameStat` enum name (camel-case-split it for readability, as the plugin does as its own fallback) and the value. See [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). For `MapStats` and the rest of `IngameState.Data`, see [ingame-state.md](../ingame-state.md).

`Graphics.MeasureText` (verified) lets you size a background box behind the lines, as AreaStatVisual does with `DrawBox` + `DrawFrame`. See [graphics.md](../graphics.md).

---

## 7. Reading preloads (PreloadsRevised)

PreloadsRevised scans the files the game streamed for the current area and alerts on matches (Exiles, league bosses, unique strongboxes). The mechanism is fully supported in this fork:

```csharp
var memory = GameController.Memory;
var files = new FilesFromMemory(memory).GetAllFiles();           // Dictionary<string, FileInformation>
var areaChangeCount = GameController.Game.AreaChangeCount;       // bumps on every area load

foreach (var (path, info) in files)
{
    if (info.ChangeCount != areaChangeCount) continue;          // only files loaded for THIS area
    var name = path.Contains('@') ? path.Split('@')[0] : path;  // strip the @-suffix variants
    // regex-match `name` against your watch-list
}
```

Verified members: `FilesFromMemory.GetAllFiles()` (`Core/PoEMemory/FilesFromMemory.cs`), `FileInformation.ChangeCount`, and `GameStateController.AreaChangeCount`. The `info.ChangeCount == areaChangeCount` filter is the whole trick — it isolates *this instance's* preloads from everything cached earlier. PreloadsRevised does this on a background `Task` (`Parallel.ForEach` over the file dictionary) and only re-parses when the area changes; mirror that to avoid scanning tens of thousands of paths every frame. See [files-in-memory.md](../files-in-memory.md).

---

## 8. Cursor & screen-space lines (Wheres-My-Cursor)

The simplest overlay in the family: a line from screen-center (the player) to the cursor, gated on no panel being open. Pure screen-space — no world projection:

```csharp
public override void Render()
{
    var ui = GameController.Game.IngameState.IngameUi;
    if (ui.OpenLeftPanel.IsVisible || ui.OpenRightPanel.IsVisible ||
        ui.TreePanel.IsVisible || ui.Atlas.IsVisible) return;

    var rect   = GameController.Window.GetWindowRectangleReal();
    var center = new Vector2(rect.Width / 2, rect.Height / 2);

    var cursor = Input.MousePositionNum;                          // screen-space cursor
    var offset = GameController.Window.GetWindowRectangle().TopLeft;
    cursor -= new Vector2(offset.X, offset.Y);                    // window-relative

    Graphics.DrawLine(center, cursor, 2, Color.White);
}
```

`Input.MousePositionNum`, `GameController.Window.GetWindowRectangle()` / `GetWindowRectangleReal()` / `GetWindowRectangleTimeCache` are verified. The "is a panel open?" guard (`IngameUi.OpenLeftPanel` / `OpenRightPanel` / `FullscreenPanels.Any(x => x.IsVisible)` / `LargePanels`) recurs in **every** overlay here — factor it into one `ShouldDraw()` helper, as AreaStatVisual and Blight do. See [input.md](../input.md), [ui-elements.md](../ui-elements.md), and [graphics.md](../graphics.md).

---

## Cheat-sheet: upstream member → fork equivalent

| DetectiveSquirrel uses | In this fork |
| --- | --- |
| `entity.PosNum` / `entity.GridPosNum` (Numerics) | `Render.Pos` (SharpDX `Vector3`) / `Positioned.GridPos` (SharpDX `Vector2`); convert at boundary |
| `render.BoundsNum` / `render.RotationNum` | `Render.Bounds` / `Render.Rotation` (SharpDX `Vector3`) |
| `e.TryGetComponent<T>(out var c)` | `var c = e.GetComponent<T>(); if (c != null)` |
| `Graphics.DrawCircleInWorld` / `DrawFilledCircleInWorld` | hand-roll with `WorldToScreen` + `DrawLine` (§2) |
| `Graphics.DrawLineInWorld` / `DrawLineOnLargeMap` / `DrawCircleOnLargeMap` | project endpoints yourself + `DrawLine` ([coordinates.md](../coordinates.md)) |
| `IngameData.GetTerrainHeightAt(grid)` / `GetGridScreenPosition` | use `Render.Pos.Z` / `Render.Height`; project via `Camera.WorldToScreen` |
| `Pathfinding.PathingNodes` (full path) | `Pathfinding.TargetMovePos` / `WantMoveToPosition` (endpoints only) |
| `StateMachine.States` | not available — only `CanBeTarget` / `InTarget` |
| `Files.StatDescriptions.TranslateMod(...)` / `Files.BlightTowers` | not available — show `GameStat` name / hard-code radii |
| `GameController.InspectObject(...)` | not available — use `DebugWindow` |

All "not available" rows are documented in [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). Everything else is verified against `Core/` as of this fork.

---

## Source repos

- [DetectiveSquirrel/ExileAPI-WhereAreYouGoing](https://github.com/DetectiveSquirrel/ExileAPI-WhereAreYouGoing) — monster pathing & cast-destination arrows; map+world drawing.
- [DetectiveSquirrel/WhatAreYouDoing](https://github.com/DetectiveSquirrel/WhatAreYouDoing) — trap/hazard classification by metadata, `ActorScalePct` sizing, timed-shot cache, hand-rolled world circle.
- [DetectiveSquirrel/Blight](https://github.com/DetectiveSquirrel/Blight) — tower classification by `BlightTower.Id`, pathway lines, `StateMachine.States` completion detection (upstream-only).
- [DetectiveSquirrel/Abyss](https://github.com/DetectiveSquirrel/Abyss) — id-ordered crevice pathway lines.
- [DetectiveSquirrel/Guardians-R-Us](https://github.com/DetectiveSquirrel/Guardians-R-Us) — minion/guardian tracking via `Actor.DeployedObjects`, per-entity `Stats`.
- [DetectiveSquirrel/AreaStatVisual](https://github.com/DetectiveSquirrel/AreaStatVisual) — per-instance `IngameState.Data.MapStats`, stat-description translation (upstream-only).
- [DetectiveSquirrel/PreloadsRevised-poe1](https://github.com/DetectiveSquirrel/PreloadsRevised-poe1) — area preloads via `FilesFromMemory.GetAllFiles()` filtered by `AreaChangeCount`.
- [DetectiveSquirrel/Wheres-My-Cursor](https://github.com/DetectiveSquirrel/Wheres-My-Cursor) — screen-space player→cursor line with panel gating.
- [DetectiveSquirrel/Character-Data](https://github.com/DetectiveSquirrel/Character-Data), [DetectiveSquirrel/SkillGems](https://github.com/DetectiveSquirrel/SkillGems) — cross-checked for the panel-gating and `GetComponent` patterns reused above.
