# Coordinates, camera & maps

How to turn an entity's position in the game world into a pixel on screen — for both world-space overlays and minimap/large-map drawing. See the [API reference index](README.md).

## Coordinate systems

ExileApi works with three coordinate spaces. Knowing which one a value is in (and which type carries it) is half the battle.

| Space | Typical source | Type | Notes |
| --- | --- | --- | --- |
| **Grid units** | `Positioned.GridPos`, `Positioned.GridPosI`, `Entity.GridPos` | `Vector2` / `Vector2i` | The game's tile grid. Integer per-tile; used for pathfinding and distance. |
| **World units** | `Render.Pos`, `Positioned.WorldPos`, `Entity.Pos` | `Vector3` (SharpDX) / `Vector2` | Engine world coordinates. `Render.Pos` is 3D (X, Y, Z height); `Positioned.WorldPos` is 2D. |
| **Screen pixels** | `Camera.WorldToScreen(...)`, `Element.GetClientRect()` | `Vector2` (System.Numerics) | The DirectX viewport. What you pass to `Graphics`. |

These component members are documented in [components-world.md](components-world.md). The `Camera` is reached through `IngameState` — see [ingame-state.md](ingame-state.md).

> Types matter: `Render.Pos` and `Camera.Position`/`WorldToScreen` use **SharpDX** `Vector3`/`Vector2` (the project still references SharpDX for memory structs). `Graphics` draw calls and most newer plugin code use **System.Numerics** vectors. Convert at the boundary.

### Grid ↔ world conversion

The conversion lives in two equivalent helper classes (`ExileCore.WorldPositionExtensions` and `ExileCore.Shared.Helpers.PoeMapExtension`). Both use the same constants:

```csharp
private const float MarsEllipticOrbit = 0.092f;   // grid-per-world multiplier (jokey name)
private const float Offset = 5.434783f;           // = 1 / MarsEllipticOrbit / 2, roughly
```

So **1 grid tile ≈ 1 / 0.092 ≈ 10.87 world units**. Note `0.092 ≈ 23/250`, the same ratio Radar-style plugins spell out as `GridToWorldMultiplier = 250 / 23 ≈ 10.87` (250 world units per 23-unit tile) — the engine just stores it as its reciprocal. The conversions are:

```csharp
// world = grid / 0.092 + 5.434783
worldXY = gridXY / MarsEllipticOrbit + Offset;
// grid = floor(world * 0.092)
gridXY  = floor(worldXY * MarsEllipticOrbit);
```

`Source`: `Core/WorldPositionExtensions.cs`, `Core/Shared/Helpers/PoeMapExtension.cs`.

## Camera

`ExileCore.PoEMemory.MemoryObjects.Camera` is a `RemoteMemoryObject` reached via `GameController.IngameState.Camera`. The camera frame (offsets, half-extents) is cached per frame.

| Member | Type | Meaning |
| --- | --- | --- |
| `Width` / `Height` | `int` | Viewport size in pixels. |
| `Size` | `Vector2` | `new Vector2(Width, Height)`. |
| `Position` | `Vector3` | Camera world position. |
| `ZFar` | `float` | Far clip plane distance. |
| `WorldToScreen(Vector3 vec)` | `Vector2` | Projects a world position to screen pixels. |

The view/projection matrix (`CameraOffsets.MatrixBytes`) and the cached `HalfWidth`/`HalfHeight` are **private**; `WorldToScreen` is the public surface. Internally it does the standard projective transform:

```csharp
public Vector2 WorldToScreen(Vector3 vec)
{
    var cord = new Vector4(vec, 1f);
    cord = Vector4.Transform(cord, Matrix);   // view-projection
    cord = Vector4.Divide(cord, cord.W);      // perspective divide
    return new Vector2(
        (cord.X + 1.0f) * HalfWidth,
        (1.0f - cord.Y) * HalfHeight);
}
```

`WorldToScreen` is wrapped in a try/catch and returns `Vector2.Zero` on failure, so a returned `(0,0)` can mean "off screen / not ready" rather than the top-left corner — guard accordingly.

`Source`: `Core/PoEMemory/MemoryObjects/Camera.cs`, `GameOffsets/CameraOffsets.cs`.

## WorldPositionExtensions

`ExileCore.WorldPositionExtensions` — extension methods on vectors for grid/world conversion. (The near-identical `PoeMapExtension` adds a `Vector2 WorldToGrid(this Vector2)` overload.)

| Method | Signature | Returns |
| --- | --- | --- |
| `GridToWorld` | `Vector2 GridToWorld(this Vector2 grid)` | 2D world position. |
| `GridToWorld` | `Vector3 GridToWorld(this Vector2 grid, float z)` | 3D world position with supplied height `z`. |
| `WorldToGrid` | `Vector2 WorldToGrid(this Vector3 world)` | 2D grid position (floored; Z ignored). |

`PoeMapExtension` additionally offers `Vector2 WorldToGrid(this Vector2 world)`.

There are **no distance helpers here** — distance lives in `MathHepler` and on the vector types (`Vector2i.Distance`, `Entity.DistancePlayer`, etc.).

`Source`: `Core/WorldPositionExtensions.cs`, `Core/Shared/Helpers/PoeMapExtension.cs`.

## MathHepler

`ExileCore.Shared.Helpers.MathHepler` (note the spelling) collects vector/angle utilities. Members worth knowing:

| Member | Signature | Use |
| --- | --- | --- |
| `RotateVector2` | `Vector2 RotateVector2(Vector2 v, float angleDeg)` | Rotate a 2D vector about the origin (degrees). |
| `ConvertToRadians` | `double ConvertToRadians(double deg)` | Degrees → radians. |
| `NormalizeVector` | `Vector2 NormalizeVector(Vector2 v)` | Unit-length copy. |
| `VectorLength` | `float VectorLength(Vector2 v)` | Magnitude. |
| `GetPolarCoordinates` | `double GetPolarCoordinates(this Vector2 v, out double phi)` | Radius + angle. |
| `Translate` | `Vector2 Translate(this Vector2 v, float dx, float dy)` (+ Numerics & `Vector3` overloads) | Offset a point. |
| `Mult` | `System.Numerics.Vector2 Mult(this Vector2 v, float dx, float dy)` | Per-component scale. |
| `Distance` / `DistanceSquared` | `float (this Vector2 a, Vector2 b)` | Euclidean distance helpers. |
| `PointInRectangle` | `bool (this Vector2 p, RectangleF r)` | Hit test (treats `r.Width`/`r.Height` as right/bottom edges). |
| `GetDirectionsUV` | `RectangleF (double phi, double distance)` | UV rect for directional arrow sprites. |
| `Max` | `float Max(params float[] values)` | Vararg max. |
| `Randomizer` / `GetRandomWord` | — | Shared `Random` and random string. |

These are SharpDX-vector based. `Translate`/`Mult` also have System.Numerics overloads.

`Source`: `Core/Shared/Helpers/MathHepler.cs`.

## Minimap & large-map drawing

To draw on the in-game map (Radar-style overlays) you combine three pieces:

1. **The map's screen anchor.** `GameController.GetLeftCornerMap()` returns the `Vector2` screen position of the minimap's left corner, adjusting for the diagnostic UI (`DiagnosticInfoType.Off/Short/Full`) and the sulphite bar. The engine also exposes a 500 ms-cached version internally (`LeftCornerMap`) used to position the left plugin panel. `Source`: `Core/GameController.cs`.

2. **The large-map element.** `GameController.IngameState.IngameUi.Map` (`ExileCore.PoEMemory.Elements.Map`) exposes:

   | Member | Meaning |
   | --- | --- |
   | `LargeMap` (`Element`) | The full-screen map element. |
   | `LargeMapShiftX` / `LargeMapShiftY` (`float`) | Map pan/shift. |
   | `LargeMapZoom` (`float`) | Map scale/zoom. |
   | `SmallMiniMap` (`Element`) | The corner minimap element. |
   | `SmallMinMapX` / `SmallMinMapY` / `SmallMinMapZoom` | Minimap shift & zoom. |

   `largeMap.GetClientRectCache` gives its on-screen rectangle; its center is the projection origin. (Radar reads center/scale by casting `LargeMap` to its own `SubMap` view — see the cross-check note below.)

3. **The grid-delta → map-delta transform.** The map is an isometric projection rotated by the camera angle (~38.7°). You take a target's grid position **relative to the player**, project it through `cos`/`sin` of the camera angle, scale by the map zoom, and add it to the map center. This is the canonical Radar transform:

```csharp
const int   TileToGridConversion  = 23;
const int   TileToWorldConversion = 250;
const float GridToWorldMultiplier = TileToWorldConversion / 23f; // ≈ 10.87
const double CameraAngle = 38.7 * Math.PI / 180;
static readonly float Cos = (float)Math.Cos(CameraAngle);
static readonly float Sin = (float)Math.Sin(CameraAngle);

// grid delta (target - player) and a Z (world height / GridToWorldMultiplier)
Vector2 TranslateGridDeltaToMapDelta(Vector2 delta, float deltaZ)
{
    deltaZ /= GridToWorldMultiplier; // z is world units -> translate to grid
    return (float)mapScale * new Vector2(
        (delta.X - delta.Y) * Cos,
        (deltaZ - (delta.X + delta.Y)) * Sin);
}
```

Adapted from [instantsc/Radar](https://github.com/instantsc/Radar) (`Radar.cs`).

## Examples

### (a) World marker via Camera.WorldToScreen

Draw text/box at an entity's feet, in world space:

```csharp
public override void Render()
{
    var camera = GameController.IngameState.Camera;
    foreach (var e in GameController.Entities) // ICollection<Entity>
    {
        if (!e.IsValid || !e.HasComponent<Render>()) continue;

        // Render.Pos is a SharpDX Vector3 (world units, includes height)
        var screen = camera.WorldToScreen(e.GetComponent<Render>().Pos);
        if (screen == SharpDX.Vector2.Zero) continue; // off screen / not ready

        Graphics.DrawText(e.RenderName, screen);
    }
}
```

This mirrors `ShowGroundEffects` (`Camera.WorldToScreen(e.PosNum)` then `Graphics.DrawText`). Plugins often expose a System.Numerics `PosNum` helper alongside the SharpDX `Pos`; this repo's `Render.Pos` is SharpDX, so feed that directly to `WorldToScreen`.

### (b) Minimap marker via the large-map transform

Place a marker for a target grid position on the open large map:

```csharp
public override void Render()
{
    var map = GameController.IngameState.IngameUi.Map;
    var largeMap = map.LargeMap;
    if (!largeMap.IsVisible) return;

    var player    = GameController.Player.GetComponent<Positioned>();
    var playerPos = player.GridPos;
    var playerHeight = GameController.Player.GetComponent<Render>().Pos.Z;

    var mapCenter = largeMap.GetClientRectCache.Center; // projection origin
    var mapScale  = map.LargeMapZoom;                   // * any custom zoom

    foreach (var target in targetGridPositions) // Vector2 in grid units
    {
        var delta = target - playerPos;
        var mapDelta = mapScale * new Vector2(
            (delta.X - delta.Y) * Cos,
            (-(delta.X + delta.Y)) * Sin); // deltaZ omitted for flat markers

        Graphics.DrawText("X", mapCenter + mapDelta);
    }
}
```

For a corner-minimap marker, anchor at `GameController.GetLeftCornerMap()` (plus the minimap's own size/shift) and use `map.SmallMinMapZoom` instead. The rotation math is identical — `Radar` and `WhereAreYouGoing` both build on this `mapCenter + TranslateGridDeltaToMapDelta(target - player, height)` pattern.

See [graphics.md](graphics.md) for the `Graphics` draw API used above.

## Source

- `Core/PoEMemory/MemoryObjects/Camera.cs`
- `GameOffsets/CameraOffsets.cs`
- `Core/WorldPositionExtensions.cs`
- `Core/Shared/Helpers/PoeMapExtension.cs`
- `Core/Shared/Helpers/MathHepler.cs`
- `Core/GameController.cs` (`GetLeftCornerMap`)
- `Core/PoEMemory/Elements/Map.cs`
- `GameOffsets/Native/Vector2i.cs`
- Cross-checked against plugins: [instantsc/Radar](https://github.com/instantsc/Radar) (large-map transform), [DetectiveSquirrel/ExileAPI-WhereAreYouGoing](https://github.com/DetectiveSquirrel/ExileAPI-WhereAreYouGoing) and [arturino009/ShowGroundEffects](https://github.com/arturino009/ShowGroundEffects) (`Camera.WorldToScreen`).
