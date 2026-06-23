# Recipe: pathfinding, movement & route sharing

How walking-bot style plugins read the walkable terrain grid, compute a route over it, share that route with other plugins through the `PluginBridge`, and turn it into mouse-clicks or WASD movement. Distilled from real ExileApi plugins and adapted to this fork.

> [API reference index](../README.md) · [cookbook index](README.md)

The pieces, and where each lives in this fork:

| Concern | Fork API | Reference |
| --- | --- | --- |
| Walkable grid | `GameController.IngameState.Data.Terrain` (`TerrainData`) | [../../offsets.md](../../offsets.md), [../memory.md](../memory.md) |
| Reading the grid bytes | `Memory.ReadBytes(addr, size)` over `NativePtrArray` | [../memory.md](../memory.md) |
| Sharing a route | `GameController.PluginBridge` (`SaveMethod` / `GetMethod<T>`) | [../plugins.md](../plugins.md) |
| Grid ↔ world ↔ screen | `WorldToGrid` / `GridToWorld`, `Camera.WorldToScreen` | [../coordinates.md](../coordinates.md) |
| Player move-state | `Pathfinding` component (`WantMoveToPosition`, `IsMoving`) | [../components-world.md](../components-world.md) |
| Sending input | `ExileCore.Input` (`KeyDown`/`KeyUp`/`Click`/`SetCursorPos`) | [../input.md](../input.md) |

## 1. Reading the walkable terrain grid

The current area's walkability is a packed byte grid embedded in `IngameData`. The fork exposes it
as `GameController.IngameState.Data.Terrain`, a `TerrainData` struct (`GameOffsets/TerrainData.cs`):

```csharp
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TerrainData
{
    [FieldOffset(0x08)] public NativePtrArray LayerMelee;   // std::vector<byte>
    [FieldOffset(0x20)] public NativePtrArray LayerRanged;  // std::vector<byte>
    [FieldOffset(0x38)] public int NumCols;
    [FieldOffset(0x3C)] public int NumRows;
    [FieldOffset(0x40)] public int BytesPerRow;  // row stride; each byte packs two 4-bit cells
}
```

There are two layers: `LayerMelee` (where a walking character can stand) and `LayerRanged` (line
of sight / projectile reachability). Each is a native `std::vector<byte>` modeled by
`GameOffsets.Native.NativePtrArray` (`First` / `Last` / `End`, plus `Size => Last - First`), so the
whole buffer is one `ReadBytes` away. Every byte holds **two** horizontally-adjacent cells in its
two nibbles, hence the `x >> 1` indexing and `BytesPerRow` stride. See
[../../offsets.md](../../offsets.md) for the layout and the build-specific caveat on where
`TerrainData` sits inside `IngameDataOffsets`, and [../memory.md](../memory.md) for the reader.

Decode it once per area (in `AreaChange`) into an `int[,]` you can index by grid cell. This is the
copilot decode, adapted to this fork — `LayerMelee.First`/`.Size` and `BytesPerRow` are all present
here, so it ports verbatim:

```csharp
private byte[,] _tiles;   // [x, y]: 0 = blocked, 1 = walkable, 2 = dash-only

public override void AreaChange(AreaInstance area)
{
    var terrain = GameController.IngameState.Data.Terrain;
    var numCols = (terrain.NumCols - 1) * 23;   // 23 grid cells per terrain tile
    var numRows = (terrain.NumRows - 1) * 23;
    if ((numCols & 1) > 0) numCols++;           // keep the column count even (two cells per byte)
    _tiles = new byte[numCols, numRows];

    // LayerMelee: walkable where the nibble is non-zero.
    var bytes = GameController.Memory.ReadBytes(terrain.LayerMelee.First, terrain.LayerMelee.Size);
    var dataIndex = 0;
    for (var y = 0; y < numRows; y++)
    {
        for (var x = 0; x < numCols; x += 2)
        {
            var b = bytes[dataIndex + (x >> 1)];
            _tiles[x,     y] = (byte)((b & 0xf) > 0 ? 1 : 255);
            _tiles[x + 1, y] = (byte)((b >> 4)  > 0 ? 1 : 255);
        }
        dataIndex += terrain.BytesPerRow;       // step one row using the real stride
    }

    // LayerRanged: cells only reachable by dashing/teleporting (nibble > 3) get marked 2.
    bytes = GameController.Memory.ReadBytes(terrain.LayerRanged.First, terrain.LayerRanged.Size);
    dataIndex = 0;
    for (var y = 0; y < numRows; y++)
    {
        for (var x = 0; x < numCols; x += 2)
        {
            var b = bytes[dataIndex + (x >> 1)];
            if (_tiles[x,     y] == 255) _tiles[x,     y] = (byte)((b & 0xf) > 3 ? 2 : 255);
            if (_tiles[x + 1, y] == 255) _tiles[x + 1, y] = (byte)((b >> 4)  > 3 ? 2 : 255);
        }
        dataIndex += terrain.BytesPerRow;
    }
}
```

> Adapted from [exApiTools/copilot](https://github.com/exApiTools/copilot) (`AutoPilot.AreaChange`).
> The fork's `Memory.ReadBytes`, `NativePtrArray.First`/`.Size` and `TerrainData` are all present —
> see [../memory.md](../memory.md).

**Richer terrain helpers are upstream-only.** The exApiTools/Radar fork reads its grid through
extra `IngameData` members (`RawPathfindingData`, `RawTerrainHeightData`, `AreaDimensions`,
`GetClearedPathfindingData`, `RawTerrainTargetingData`) and a `Memory.ReadStdVector<T>` helper for
the `.tgt` tile metadata. **None of those exist in this fork** — see
[../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) (`ReadStdVector` row).
Decode the raw `Terrain` layers yourself as above instead of porting `RawPathfindingData` directly.

## 2. Pathfinding over the grid (Dijkstra direction field)

Radar's pathfinder is a clean, reusable design: a Dijkstra flood-fill **from the target**, which
yields a *direction field* over the whole map — every reachable cell stores the index of its best
neighbour toward the target. Walking any start cell to the target is then a constant-memory pointer
chase, and the expensive flood-fill is amortised across every start position. Coordinates are
`GameOffsets.Native.Vector2i`, whose `+`/`-`/`*` operators and `DistanceF` are all in the fork.

The shape (condensed from `Radar/PathFinder.cs`):

```csharp
private static readonly Vector2i[] NeighborOffsets =
{
    new(0, 1), new(1, 1), new(1, 0), new(1, -1),
    new(0, -1), new(-1, -1), new(-1, 0), new(-1, 1),
};

// FindPath walks the precomputed direction field from start to target.
public List<Vector2i> FindPath(Vector2i start, Vector2i target)
{
    var directionField = _directionField[target];   // byte[][] built by the flood-fill
    if (directionField[start.Y][start.X] == 0) return null;   // 0 = unreachable

    var path = new List<Vector2i>();
    var current = start;
    while (current != target)
    {
        var directionIndex = directionField[current.Y][current.X];
        if (directionIndex == 0) return null;
        var next = NeighborOffsets[directionIndex - 1] + current;   // Vector2i + operator
        path.Add(next);
        current = next;
    }
    return path;
}
```

The flood-fill (`RunFirstScan`) uses a binary-heap priority queue keyed by exact distance, adds
neighbours via `coord.DistanceF(previous)` for the diagonal cost, and `yield return`s an
intermediate path roughly every 100 ms so a long scan can report progress and be cancelled. The
direction field is only materialised once the distance dictionary would exceed the grid's own
memory footprint — until then `FindPath` falls back to greedy descent of the exact-distance field.

Build it from your decoded grid by treating chosen cell values as pathable:

```csharp
// Radar treats values {1,2,3,4,5} as walkable for path cost; your decode may use {1,2}.
var pathFinder = new PathFinder(grid, pathableValues: new[] { 1, 2 });
```

> Adapted from [exApiTools/Radar](https://github.com/exApiTools/Radar) (`PathFinder.cs`,
> `BinaryHeap.cs`). `Vector2i` arithmetic and `DistanceF` are verified in
> `GameOffsets/Native/Vector2i.cs`.

### Cheap inline reachability (no full path)

If you only need "can I walk/dash straight from here to there", march the grid along the direction
vector and inspect each cell — copilot's dash check does exactly this, blocking on a `255` (wall)
cell and dashing across a run of `2` (dash-only) cells:

```csharp
var dir = (targetGrid - GameController.Player.GridPos);
dir.Normalize();
for (var i = 0; i < 500; i++)
{
    var p = GameController.Player.GridPos + i * dir;
    var tile = _tiles[(int)p.X, (int)p.Y];
    if (tile == 255) break;          // wall: cannot dash through
    // ... count walkable vs. dash-only cells to decide ...
}
```

`Player.GridPos` is the SharpDX `Vector2` from the `Positioned` component (see
[../components-world.md](../components-world.md)).

## 3. Sharing a route through the PluginBridge

This is the headline pattern: **Radar computes routes and exposes them so other plugins do not
re-implement pathfinding.** The mechanism is `GameController.PluginBridge` — a name→delegate
registry (`SaveMethod` / `GetMethod<T>`, `ExileCore.PluginBridge`). See
[../plugins.md](../plugins.md#pluginbridge-named-methods).

### Provider side (Radar, registered in `Initialise()`)

```csharp
public override bool Initialise()
{
    // target grid cell, a callback that receives the path, and a cancellation token.
    GameController.PluginBridge.SaveMethod("Radar.LookForRoute",
        (Vector2 target, Action<List<Vector2i>> callback, CancellationToken cancellationToken) =>
            AddRoute(target, null, callback, cancellationToken));

    GameController.PluginBridge.SaveMethod("Radar.ClusterTarget",
        (string targetName, int expectedCount) => ClusterTarget(targetName, expectedCount));
    return true;
}
```

`AddRoute` runs the pathfinder on a background `Task` and invokes `callback` with an updated path
each time the player moves — so the consumer keeps receiving fresh paths, not a one-shot answer.
`Vector2` here is `System.Numerics.Vector2`; the path is a `List<Vector2i>` of grid cells.

### Consumer side

A consumer fetches the delegate by name and invokes it. Always null-check the result —
`GetMethod<T>` returns `null` when Radar is not loaded/enabled, and **plugin load order is not
guaranteed**, so fetch lazily (e.g. in `Tick`/`Render`) rather than caching it from `Initialise`:

```csharp
private volatile List<Vector2i> _currentPath;

private void RequestRoute(System.Numerics.Vector2 targetGridCell, CancellationToken ct)
{
    var lookForRoute = GameController.PluginBridge
        .GetMethod<Action<System.Numerics.Vector2, Action<List<Vector2i>>, CancellationToken>>(
            "Radar.LookForRoute");

    if (lookForRoute == null) return;   // Radar not present/enabled this frame

    lookForRoute(targetGridCell, path => _currentPath = path, ct);
}
```

The delegate type passed to `GetMethod<T>` **must match what Radar registered**. The arity differs
between Radar versions (older builds registered a `(Vector2 target, Action<List<Vector2i>>)`
two-argument form). If `GetMethod<T>` returns non-null but the cast yields the wrong shape you will
get a runtime cast failure, so confirm the signature against the Radar build you target.

> Provider verified in [exApiTools/Radar](https://github.com/exApiTools/Radar) (`Radar.cs:52`,
> `Radar.Pathfinding.cs`). For the inverse direction — a plugin *consuming* another's bridge method
> — Wasdeg calls `GetMethod<Action<Vector2i, uint>>("MagicInput.CastSkillWithPosition")`; see below.

## 4. From a path to movement

A grid path is just cells. Two ways to actually move along it.

### (a) Click-to-move / cast-at-position

Convert the next grid waypoint to world, then to screen, position the cursor and send the move
input. The grid→world→screen chain uses fork-verified members
(`WorldToGrid`/`GridToWorld` in [../coordinates.md](../coordinates.md),
`Camera.WorldToScreen`, `Window.GetWindowRectangle`):

```csharp
private System.Numerics.Vector2 GridToScreen(Vector2 gridCell, float z)
{
    var world  = gridCell.GridToWorld(z);                          // Vector2 -> Vector3 world
    var screen = GameController.IngameState.Camera.WorldToScreen(world);
    return screen + GameController.Window.GetWindowRectangle().Location; // window-relative -> absolute
}
```

```csharp
// Move toward the next path waypoint (move bound to a key, e.g. 'T').
var next = _currentPath[0];
var z = GameController.Player.GetComponent<Render>().Pos.Z;
Input.SetCursorPos(GridToScreen(new Vector2(next.X, next.Y), z));   // ExileCore.Input
Input.KeyDown(Settings.MoveKey);   // press the in-game "move only" key
// ... short delay ...
Input.KeyUp(Settings.MoveKey);
```

`WorldToScreen` returns `Vector2.Zero` when the target is off-screen/not ready — copilot clamps
the result into the window rectangle (with a ~50 px margin) so the cursor still moves *toward* an
off-screen waypoint rather than snapping to the top-left corner. `Camera.WorldToScreen` and its
zero-return caveat are documented in [../coordinates.md](../coordinates.md); the `Input` members
(`SetCursorPos`, `KeyDown`/`KeyUp`, `Click`, `LeftDown`/`LeftUp`) in [../input.md](../input.md).

> Adapted from [exApiTools/copilot](https://github.com/exApiTools/copilot)
> (`AutoPilot.AutoPilotLogic`, `Helper.WorldToValidScreenPosition`).

### (b) WASD movement from held direction keys

Wasdeg maps the four WASD keys to an isometric grid direction and re-issues a move toward that
direction. It reads the player's own move-state from the **`Pathfinding` component** to decide
whether a new click is even needed (`WantMoveToPosition`, `IsMoving` — both present in the fork,
`Core/PoEMemory/Components/Pathfinding.cs`):

```csharp
var keys = new
{
    up    = Input.IsKeyDown(Settings.MoveUpHotkey.Value.Key),
    down  = Input.IsKeyDown(Settings.MoveDownHotkey.Value.Key),
    left  = Input.IsKeyDown(Settings.MoveLeftHotkey.Value.Key),
    right = Input.IsKeyDown(Settings.MoveRightHotkey.Value.Key),
};

// Screen WASD -> isometric grid offset. The grid axes are rotated ~45deg from the
// screen, so a single screen direction maps to a diagonal grid step (verbatim from Wasdeg).
var dir = keys switch
{
    { down: true, up: false, left: true,  right: false } => new Vector2i(-1, 0),
    { down: true, up: false, left: false, right: true  } => new Vector2i(0, -1),
    { down: false, up: true, left: true,  right: false } => new Vector2i(0, 1),
    { down: false, up: true, left: false, right: true  } => new Vector2i(1, 0),
    { down: true,  up: false } => new Vector2i(-1, -1),
    { down: false, up: true  } => new Vector2i(1, 1),
    { left: true,  right: false } => new Vector2i(-1, 1),
    { left: false, right: true  } => new Vector2i(1, -1),
    _ => (Vector2i?)null,
};

// Re-click only if the player isn't already heading roughly that way:
var pathfinding = GameController.Player.GetComponent<Pathfinding>();
if (pathfinding != null && pathfinding.IsMoving)
{
    var heading = pathfinding.WantMoveToPosition; // Vector2i grid cell the game is steering toward
    // compare heading-vs-desired angle and skip the click if close enough
}
```

`Input.IsKeyDown(Keys)` is verified in the fork's `Core/Input.cs`. Wasdeg then issues the move by
calling **another** plugin's bridge method
(`GetMethod<Action<Vector2i, uint>>("MagicInput.CastSkillWithPosition")`), which is the same
`PluginBridge` consumer pattern as §3 in reverse — proving the bridge is the standard channel for
both *sharing routes* and *delegating input* between plugins.

> Movement state and key-direction mapping adapted from
> [exApiTools/Wasdeg](https://github.com/exApiTools/Wasdeg) (`Wasdeg.cs`).
> **Upstream-only members it relies on:** `Entity.GridPosNum`, `Vector2.ToVector2Num()`,
> `Vector2.Normalized()`, `AbsoluteAngleTo()`, `TruncateToVector2I()` are not in this fork — use
> SharpDX `Player.GridPos` and `System.Numerics`/`MathHepler` helpers
> ([../coordinates.md](../coordinates.md)) instead, and see
> [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) (`GridPosNum` row).

## 5. Room-graph pathing (Sanctum)

Sanctum is a different shape of pathfinding: not a tile grid but a small **layered DAG of rooms**,
where you want the *highest-weighted* route (best rewards / least danger) rather than the shortest.
PathfindSanctum runs a max-cost variant of Dijkstra over it:

1. Score every room into a `double[layer, room]` weight map (`WeightCalculator`).
2. Run Dijkstra that **maximises** accumulated weight from the entrance node, expanding to each
   room's connected children in the next layer (`PathFinder.FindBestPath`, `SortedSet` ordered by
   descending cost).
3. Draw the winning path by framing each room's on-screen rectangle.

```csharp
// Neighbours are the rooms in the previous layer whose connection list contains this room's index.
private static IEnumerable<(int, int)> GetNeighbors((int, int) room, byte[][][] connections)
{
    var (layer, index) = room;
    if (layer == 0) yield break;
    var previous = connections[layer - 1];
    for (var i = 0; i < previous.Length; i++)
        if (previous[i].Contains((byte)index))
            yield return (layer - 1, i);
}
```

The room layout, per-room connections and on-screen rectangles come from the Sanctum UI element
graph: `GameController.IngameState.IngameUi.SanctumFloorWindow` → `RoomsByLayer`,
`FloorData.RoomLayout`, and each room's `GetClientRect()`.

> **The entire Sanctum element tree is upstream-only in this fork.** `SanctumFloorWindow`,
> `RoomsByLayer`, `SanctumRoomElement`, `SanctumRoomData` are **not present** in `Core/` here —
> they are listed among the missing types in
> [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). The *algorithm*
> (weighted max-cost Dijkstra over a layered DAG, with `Graphics.DrawFrame` to highlight the path)
> is fully reusable; the *data source* is not, until those elements are added.
> Adapted from [ChandlerFerry/PathfindSanctum](https://github.com/ChandlerFerry/PathfindSanctum)
> (`PathFinder.cs`, `WeightCalculator.cs`) and
> [exApiTools/BetterSanctum](https://github.com/exApiTools/BetterSanctum) (`BetterSanctumPlugin.cs`).

## Pitfalls

- **Off-thread reads.** Path scans run on `Task`s; `Memory` reads and the cached `Terrain`/grid are
  fine to read from a worker, but treat results as snapshots and re-validate the player position
  each step (Radar re-reads `GetPlayerPosition` and recomputes the path whenever the player moves).
- **`WorldToScreen` returning `(0,0)`** means off-screen/not-ready, not the top-left corner — guard
  or clamp ([../coordinates.md](../coordinates.md)).
- **`GetMethod<T>` null & signature drift** — null-check every call and match the exact delegate
  shape Radar registered (§3).
- **Grid stride** — always step rows by `BytesPerRow`, never by `NumCols`; the buffer is nibble-packed
  and may be padded.
- **Offset volatility** — `TerrainData`'s position inside `IngameDataOffsets` is build-specific; verify
  against your ExileApi-Compiled reference after a patch ([../../offsets.md](../../offsets.md)).

## Source repos

- [exApiTools/Radar](https://github.com/exApiTools/Radar) — `Radar.cs` (`PluginBridge.SaveMethod("Radar.LookForRoute", ...)`), `PathFinder.cs`, `BinaryHeap.cs`, `Radar.Pathfinding.cs`, `Radar.MemoryInteraction.cs`.
- [exApiTools/copilot](https://github.com/exApiTools/copilot) — `AutoPilot.cs` (terrain decode, click-move, dash check), `Helper.cs` (`WorldToValidScreenPosition`).
- [exApiTools/Wasdeg](https://github.com/exApiTools/Wasdeg) — `Wasdeg.cs` (WASD→grid direction, `Pathfinding` component, bridge consumption).
- [exApiTools/BetterSanctum](https://github.com/exApiTools/BetterSanctum) — `BetterSanctumPlugin.cs` (room graph, connections, room rects).
- [ChandlerFerry/PathfindSanctum](https://github.com/ChandlerFerry/PathfindSanctum) — `PathFinder.cs`, `WeightCalculator.cs` (weighted max-cost room Dijkstra).
- [exApiTools/ProjectileTracker](https://github.com/exApiTools/ProjectileTracker) — reviewed; no pathfinding/route-sharing applicable to this recipe.

Cross-links: [../coordinates.md](../coordinates.md) · [../memory.md](../memory.md) · [../plugins.md](../plugins.md) · [../input.md](../input.md) · [../../offsets.md](../../offsets.md) · [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md)
