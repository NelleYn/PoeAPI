# Recipe: icons, minimap & large-map rendering

Reusable techniques for drawing icons on the corner minimap and the open large map, drawing
world-space sprites / health bars, building an entity→icon mapping over a shared sprite sheet,
and exposing per-entity icons to other plugins. See the [API reference index](../README.md) and
the [cookbook index](README.md).

These recipes are distilled from `IconsBuilder`, `MinimapIcons`, `HeistIcons`,
`ExpeditionIcons`, `HealthBars`, `Radar` and `WhereTheCirclesAt`. Every fork API named below was
checked against `Core/` on `master`. Where a plugin relies on members this fork lacks, that is
called out and linked to [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

> Coordinate math (grid ↔ world, `Camera.WorldToScreen`, `GetLeftCornerMap`, the large-map
> isometric transform) is documented once in [coordinates.md](../coordinates.md) — this recipe
> reuses it. Drawing primitives (`DrawImage`, `DrawImageGui`, `InitImage`, `GetAtlasTexture`,
> `SpriteHelper`) are documented in [graphics.md](../graphics.md).

---

## 1. Two ways to address an icon sprite

A "map icon" is a sub-rectangle (UV) of a loaded PNG, drawn with `Graphics.DrawImage`. There are
two distinct sprite-addressing styles in the reference plugins; both work on this fork.

### (a) Linear index into a fixed grid sheet (`Icons.png` / `sprites.png`)

The engine ships an `Icons.png` map-icon sheet and exposes `SpriteHelper.GetUV` to turn an icon
*index* into a UV rectangle. `MapIconsIndex` / `MyMapIconsIndex` are the index enums; the in-game
`MinimapIcon.Name` can be mapped to a `MapIconsIndex` with `Extensions.IconIndexByName`.

```csharp
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;

// In Initialise(): load the sheet once.
Graphics.InitImage("Icons.png");

// A UV for a named built-in icon:
RectangleF uv = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowCircle);

// A UV for an arbitrary cell in a 7x8 sprite sheet (1-based column/row):
RectangleF modUv = SpriteHelper.GetUV(new Size2(col, row), new Size2F(7, 8));

Graphics.DrawImage("Icons.png", drawRect, uv, Color.White);
```

`SpriteHelper.GetUV` overloads on this fork: `GetUV(MapIconsIndex)`, `GetUV(MyMapIconsIndex)`,
`GetUV(int index, Size2F size)`, `GetUV(Size2 index, Size2F size)`,
`GetUV(int x, int y, float width, float height)` — all in `Core/Shared/Helpers/SpriteHelper.cs`.

### (b) Named texture from a packed atlas (`GetAtlasTexture`)

For custom art, pack a PNG + JSON (the "Free texture packer" format) into the plugin's `textures`
folder and look textures up *by name*. `GetAtlasTexture` (on `BaseSettingsPlugin`) loads the
atlas on first use and returns an `AtlasTexture` carrying its own file name and UV. `HeistIcons`
uses this for every chest type:

```csharp
using ExileCore.Shared.AtlasHelper;

// textures/HeistIconAtlas.png + HeistIconAtlas.json shipped with the plugin.
AtlasTexture tex = GetAtlasTexture("ChestUnopenedCurrency"); // null if atlas missing
if (tex != null)
    Graphics.DrawImage(tex, drawRect, Color.White); // uses tex.AtlasFileName + tex.TextureUV
```

`AtlasTexture` (`Core/Shared/AtlasHelper/AtlasTexture.cs`) exposes `TextureName`,
`AtlasFileName`, `AtlasFilePath`, `TextureUV`. The `DrawImage(AtlasTexture, …)` overloads
(`Core/Graphics.cs`) forward `AtlasFileName` + `TextureUV` to the sprite renderer.

---

## 2. The IconsBuilder pattern: an entity→icon mapping with a shared sheet

**What it does.** Instead of recomputing an icon every frame, `IconsBuilder` builds one icon
object per entity *when the entity is added*, attaches it to the entity, and lets a renderer
plugin (`MinimapIcons`) draw all attached icons. The icon object holds its sprite (`HudTexture`),
size, color, priority, draw rect and a `Show()` predicate.

**Key API.**
- `Entity.SetHudComponent<T>(T)` / `Entity.GetHudComponent<T>()` — a per-entity, per-type
  property bag (`Core/PoEMemory/MemoryObjects/Entity.cs`). This is *the* cross-plugin handoff:
  the builder stores, the renderer (and any other plugin) reads.
- `HudTexture` (`Core/Shared/HudTexture.cs`) — `{ FileName, UV, Size, Color }`.
- `BaseIcon` (`Core/Shared/Abstract/BaseIcon.cs`) — **this fork already ships the base icon
  type**: it derives `Priority`/`Rarity` from the entity, reads `MinimapIcon.Name` →
  `MapIconsIndex` → `HudTexture("Icons.png")`, carries the `strongboxesUV` map and a `Show`
  predicate. You can subclass it instead of starting from scratch.

Builder (attach on entity add, off-thread for big packs):

```csharp
public override void EntityAdded(Entity entity)
{
    if (entity.Type is EntityType.WorldItem or EntityType.Effect) return;
    _addedIcon.Enqueue(entity);
}

public override Job Tick()
{
    // Optional: offload to the engine thread pool when the queue is large.
    if (Settings.MultiThreading && _addedIcon.Count >= Threshold)
        return GameController.MultiThreadManager.AddJob(TickLogic, nameof(MyIcons));
    TickLogic();
    return null;
}

private void TickLogic()
{
    while (_addedIcon.Count > 0)
    {
        var entity = _addedIcon.Dequeue();
        var icon = BuildIcon(entity);            // returns a BaseIcon subclass or null
        if (icon != null) entity.SetHudComponent(icon);
    }
}
```

A per-rarity sprite mapping (inside a `BaseIcon` subclass such as `MonsterIcon` —
`BaseIcon.MainTexture` has a `protected` setter, so this runs in subclass code):

```csharp
MainTexture = new HudTexture("Icons.png");
MainTexture.UV = entity.Rarity switch
{
    MonsterRarity.White  => SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeRedCircle),
    MonsterRarity.Magic  => SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeBlueCircle),
    MonsterRarity.Rare   => SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowCircle),
    MonsterRarity.Unique => SpriteHelper.GetUV(MapIconsIndex.LootFilterLargePurpleHexagon),
    _ => SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeGreenHexagon),
};
```

> **IconsBuilder is itself a reusable library.** A candidate C# port of the standalone
> `IconsBuilder` lives under this repo's `proposals/IconsBuilder/` (produced by a sibling
> worker). Prefer reusing the engine's `ExileCore.Shared.Abstract.BaseIcon` + `HudTexture` where
> they cover your need, and pull in the proposal only for the entity-classification logic
> (monster/NPC/chest/shrine/mission-marker routing) it adds on top.

---

## 3. Drawing attached icons on the minimap and large map

**What it does.** The renderer iterates entities, reads each attached icon, computes a screen
position from the icon's *grid* position relative to the player, and draws the sprite. The same
loop handles both the corner minimap and the open large map — only the anchor, scale and
visibility test change.

**Key API (all present on this fork):**

| Need | Fork member | Source |
| --- | --- | --- |
| Map element | `GameController.IngameState.IngameUi.Map` (`Map`) | `Core/PoEMemory/Elements/Map.cs` |
| Large map element | `Map.LargeMap` (`Element`) | same |
| Large-map pan / zoom | `Map.LargeMapShiftX`, `LargeMapShiftY`, `LargeMapZoom` | same |
| Corner minimap | `Map.SmallMiniMap` (`Element`), `SmallMinMapZoom` | same |
| On-screen rect | `Element.GetClientRect()` / `GetClientRectCache` | `Core/PoEMemory/Element.cs` |
| Player grid pos | `GameController.Player.GetComponent<Positioned>().GridPos` (`Vector2`) | `Core/.../Positioned.cs` |
| Entity grid pos | `Positioned.GridPos`, or `Entity.GridPos` | same |
| Entity height (Z) | `Entity.GetComponent<Render>().Pos.Z` | `Core/.../Render.cs` |

The minimap is an isometric projection rotated by the camera angle (~38.7°). Take the target's
grid delta from the player, rotate by `cos`/`sin` of the camera angle, scale, and add to the map
center. This is the same transform documented in
[coordinates.md → Minimap & large-map drawing](../coordinates.md#minimap--large-map-drawing);
here it is wired through attached icons, adapted from `HeistIcons` (the closest fork-API match):

```csharp
private const float CameraAngle = 38.7f * MathF.PI / 180;
private static readonly float Cos = MathF.Cos(CameraAngle);
private static readonly float Sin = MathF.Sin(CameraAngle);

// delta = (targetGrid - playerGrid); deltaZ = height term (see below)
private static Vector2 GridDeltaToMapDelta(Vector2 delta, float scale, float deltaZ) =>
    scale * new Vector2((delta.X - delta.Y) * Cos, deltaZ - (delta.X + delta.Y) * Sin);

public override void Render()
{
    var ui   = GameController.IngameState.IngameUi;
    var map  = ui.Map;
    var large = map.LargeMap.IsVisibleLocal;
    var small = map.SmallMiniMap.IsVisibleLocal;
    if (!large && !small) return;
    // Fork-compatible panel guard (WhereTheCirclesAt uses exactly these on this fork):
    if (ui.OpenLeftPanel.IsVisible || ui.OpenRightPanel.IsVisible) return;

    // 1. Anchor + scale per active map.
    Vector2 mapCenter;
    float scale;
    if (large)
    {
        var rect = map.LargeMap.GetClientRectCache;
        mapCenter = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f)
                    + new Vector2(map.LargeMapShiftX, map.LargeMapShiftY);
        // HeistIcons-style scale; tune the constants to taste.
        var cam = GameController.IngameState.Camera;
        var k = cam.Width < 1024f ? 1120f : 1024f;
        scale = k / cam.Height * cam.Width * 3.06f / 4f / map.LargeMapZoom;
    }
    else
    {
        var rect = map.SmallMiniMap.GetClientRectCache;
        mapCenter = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        scale = /* your minimap scale */ 1f;
    }

    var playerGrid = GameController.Player.GetComponent<Positioned>().GridPos;
    var playerZ    = GameController.Player.GetComponent<Render>().Pos.Z;

    foreach (var entity in GameController.EntityListWrapper.OnlyValidEntities)
    {
        var icon = entity.GetHudComponent<BaseIcon>();
        if (icon == null || !icon.Show()) continue;

        var grid = entity.GetComponent<Positioned>().GridPos;
        var z    = entity.GetComponent<Render>().Pos.Z;
        // height term: world Z difference scaled into grid space (see coordinates.md note)
        var deltaZ = (z - playerZ) / (9f / map.LargeMapZoom);

        var pos  = mapCenter + GridDeltaToMapDelta(grid - playerGrid, scale, large ? deltaZ : 0);
        var size = icon.MainTexture.Size;
        var rect = new RectangleF(pos.X - size / 2f, pos.Y - size / 2f, size, size);

        // On the corner minimap, clip to the minimap rectangle:
        if (small && !map.SmallMiniMap.GetClientRectCache.Contains(rect)) continue;

        Graphics.DrawImage(icon.MainTexture.FileName, rect, icon.MainTexture.UV,
                           icon.MainTexture.Color);
        if (!string.IsNullOrEmpty(icon.Text))
            Graphics.DrawText(icon.Text, pos, FontAlign.Center);
    }
}
```

> **Upstream-only shortcut (not in this fork).** `MinimapIcons` reads `Map.LargeMap` *cast to a
> `SubMap`* and uses `SubMap.MapCenter` / `SubMap.MapScale` directly, plus
> `Render.UnclampedHeight`, `Entity.GridPosNum`/`PosNum` (System.Numerics) and
> `IngameState.Data.GetTerrainHeightAt(grid)` for an exact terrain-height term. **None of these
> exist on this fork** — there is no `SubMap` type, `Map.LargeMap` is a plain `Element`, and
> `Render`/`Entity`/`Positioned` expose only the SharpDX `Pos`/`Bounds`/`GridPos`. Compute the
> center from `GetClientRectCache` + `LargeMapShiftX/Y` and the scale from `LargeMapZoom` as
> above, and use `Render.Pos.Z` for the height term. `MinimapIcons`/`WhereTheCirclesAt` also test
> `IngameUi.FullscreenPanels` / `LargePanels` to suppress drawing under open windows — **this fork
> exposes neither**; use `OpenLeftPanel.IsVisible` / `OpenRightPanel.IsVisible` (shown above), the
> guard `WhereTheCirclesAt` actually uses on this fork. See
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md) (`SubMap`,
> `Entity.PosNum`, `Positioned.WorldPosNum`).

For a **corner minimap** anchor you can also use `GameController.GetLeftCornerMap()` (it adjusts
for the diagnostic UI and the sulphite bar). See
[coordinates.md](../coordinates.md#minimap--large-map-drawing).

---

## 4. Drawing world-space sprites and health bars

**What it does.** Project an entity's world position to a screen pixel with
`Camera.WorldToScreen`, then draw a sprite/box/text there. `HealthBars` draws a filled bar split
into HP/ES; `HeistIcons` and `ExpeditionIcons` also draw their atlas icons in the world when the
map is closed.

**Key API.**
- `GameController.IngameState.Camera.WorldToScreen(Vector3)` → screen `Vector2`
  (`Core/PoEMemory/MemoryObjects/Camera.cs`). Returns `(0,0)` on failure — guard for it.
- `Entity.GetComponent<Render>().Pos` (SharpDX `Vector3`); raise/lower with the SharpDX
  `Translate(dx, dy, dz)` extension (`Core/Shared/Helpers/MathHepler.cs`).
- `Render.Bounds` (SharpDX `Vector3`) for the model's size, to lift the bar above the model.
- `Entity.GetComponent<Life>()` → `HPPercentage`, `ESPercentage`, `CurHP`/`MaxHP`,
  `CurES`/`MaxES`.

```csharp
using SharpDX;

var camera = GameController.IngameState.Camera;
var render = entity.GetComponent<Render>();
var life   = entity.GetComponent<Life>();
if (render == null || life == null) return;

// Lift the bar to roughly the top of the model (Bounds.Z is model height in world units).
var head   = render.Pos.Translate(0, 0, -2 * render.Bounds.Z);
var screen = camera.WorldToScreen(head);
if (screen == Vector2.Zero) return; // off-screen / not ready

float w = 100f, h = 12f;
var bar = new RectangleF(screen.X - w / 2f, screen.Y - h / 2f, w, h);

Graphics.DrawBox(bar, Color.Black);                                   // background
Graphics.DrawImage("healthbar.png", bar with { Width = w * life.HPPercentage }, Color.Red);
var esW = w * life.ESPercentage;
Graphics.DrawImage("healthbar.png", new RectangleF(bar.X, bar.Y, esW, h * 0.3f), Color.Cyan);
Graphics.DrawFrame(bar, Color.White, 1);
```

> **Upstream-only members in `HealthBars`.** The original uses `Entity.PosNum`
> (System.Numerics), `Render.BoundsNum` and a `MultiplyAlpha` color helper — **none are in this
> fork**. Use the SharpDX `Entity.GetComponent<Render>().Pos` and `Render.Bounds` instead (shown
> above); apply alpha by constructing a `SharpDX.Color` with the desired `A`. See
> [compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

For the **ImGui draw-list** alternative (used by `WhereTheCirclesAt` to draw smooth world
circles via `ImDrawListPtr.AddPolyline` with per-point `Camera.WorldToScreen`), see
[graphics.md → ImGui overlay](../graphics.md) and the world-circle snippet in
[coordinates.md](../coordinates.md). Note `WhereTheCirclesAt` calls a `Graphics.DrawCircleOnLargeMap`
and `Entity.GridPosNum` helper that **do not exist on this fork** — draw circles yourself with
`AddPolyline` over `WorldToScreen`, or with the section-3 grid→map transform.

---

## 5. Exposing icons to other plugins (the MinimapIcons contract)

**What it does.** Because icons are stored with `Entity.SetHudComponent<T>(icon)`, *any* plugin
can read another plugin's icon for the same entity with `Entity.GetHudComponent<T>()`. This is
how a builder plugin (`IconsBuilder`) feeds a renderer plugin (`MinimapIcons`) without a direct
reference, and how a third plugin can reuse those icons.

```csharp
// Reader side (in another plugin): reuse whatever icon a builder attached.
var icon = entity.GetHudComponent<ExileCore.Shared.Abstract.BaseIcon>();
if (icon is { } i && i.Show())
{
    // i.MainTexture (HudTexture), i.Priority, i.Text, i.DrawRect ...
}
```

The HUD-component bag is keyed by `typeof(T)` (`Entity.cs`), so the producer and consumer must
agree on the exact type. Reusing the engine's `ExileCore.Shared.Abstract.BaseIcon` as the shared
key is the most portable contract; a producer using its own private icon class is only readable
by plugins that reference that class.

> The richer cross-plugin event bus (`PluginBridge` / `PublishEvent` / `ReceiveEvent`) is the
> alternative for non-per-entity data; see [plugins.md](../plugins.md). For per-entity icons the
> HUD-component bag is simpler and is what these plugins actually use.

---

## Source repos

- **exApiTools/IconsBuilder** — `IconsBuilder.cs` (entity→icon routing, `SetHudComponent`,
  multithreaded `Tick`), `Icons/MonsterIcon.cs`, `Icons/BaseIcon.cs` (the atlas/`HudTexture` +
  `SpriteHelper.GetUV` mapping). The pattern is also reflected in this repo's
  `Core/Shared/Abstract/BaseIcon.cs` and `proposals/IconsBuilder/`.
- **exApiTools/MinimapIcons** — `MinimapIcons.cs` (the minimap + large-map render loop, icon
  list cache, `GetHudComponent<BaseIcon>` consumption; note its `SubMap.MapCenter/MapScale`,
  `Entity.GridPosNum/PosNum`, `Render.UnclampedHeight`, `GetTerrainHeightAt` are upstream-only).
- **exApiTools/HeistIcons** — `Main/Core.cs` (best fork-API-matching minimap/large-map transform:
  `Map.SmallMiniMap`/`LargeMap`, `LargeMapShiftX/Y`, `LargeMapZoom`, `Camera`, `Positioned.GridPos`,
  `Render.Pos.Z`), `ChestIcon.cs` (`DeltaInWorldToMinimapDelta`), `HeistChest.cs`
  (`GetAtlasTexture` by name), `textures/HeistIconAtlas.{png,json}`.
- **exApiTools/ExpeditionIcons** — atlas icons drawn in-world and on the map; entity routing.
- **exApiTools/HealthBars** — `HealthBars.cs`, `HealthBar.cs` (`Camera.WorldToScreen` + `Life`
  HP/ES bar; `PosNum`/`BoundsNum`/`MultiplyAlpha` are upstream-only — adapt to `Render.Pos`/
  `Render.Bounds`).
- **exApiTools/Radar** — the canonical grid→map transform (see [coordinates.md](../coordinates.md));
  pathfinding code is out of scope here.
- **DetectiveSquirrel/WhereTheCirclesAt** — `WhereTheCirclesAt.cs` (ImGui `AddPolyline` world
  circles via `Camera.WorldToScreen`; its `Graphics.DrawCircleOnLargeMap` and `Entity.GridPosNum`
  are upstream-only).
</content>
