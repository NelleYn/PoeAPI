# Graphics & rendering

> The `Graphics` object passed to every plugin draws text, lines, boxes, frames and images onto the overlay. [API reference index](README.md)

Every plugin (`BaseSettingsPlugin<T>`) exposes a `Graphics` property. Call its methods from
inside `Render()` to draw on top of the game. `Graphics` (`namespace ExileCore`) is a thin
facade over the DirectX 11 renderer: text and primitives go through the ImGui background
draw list, images go through the sprite renderer.

All coordinates are **screen-space pixels** (top-left origin). To turn an entity's world or
grid position into a screen position first, see [coordinates.md](coordinates.md). For the
underlying backend (DX11 + ImGui), see [../architecture.md](../architecture.md).

## Types used in signatures

`Graphics.cs` mixes two vector types and uses SharpDX for colors and rectangles:

| Name | Actual type | Notes |
| --- | --- | --- |
| `Vector2` | `SharpDX.Vector2` | SharpDX 2D vector |
| `Vector2N` | `System.Numerics.Vector2` | `using Vector2N = System.Numerics.Vector2;` alias inside `Graphics.cs` |
| `Color` | `SharpDX.Color` | RGBA, e.g. `Color.White`, `new Color(0, 0, 0, 200)` |
| `RectangleF` | `SharpDX.RectangleF` | `new RectangleF(x, y, width, height)`; exposes `TopLeft`/`BottomRight` |

Most drawing methods are overloaded so you can pass either `Vector2` (SharpDX) or `Vector2N`
(numerics) — they are converted internally.

## Text

### DrawText

Draws text and **returns the rendered text size** as `Vector2N`. When no font name is given,
the active font from core settings is used; when `height` is omitted (or `-1`), the active
font's pixel size is used. `align` shifts `position` horizontally (`Center` subtracts half the
width, `Right` subtracts the full width).

| Overload | Notes |
| --- | --- |
| `DrawText(string text, Vector2N position)` | white, settings font/size |
| `DrawText(string text, Vector2 position, FontAlign align = Left)` | white, settings font/size |
| `DrawText(string text, Vector2N position, FontAlign align = Left)` | white, settings font/size |
| `DrawText(string text, Vector2 position, Color color)` | settings font/size |
| `DrawText(string text, Vector2N position, Color color)` | settings font/size |
| `DrawText(string text, Vector2 position, Color color, int height)` | settings font, given height |
| `DrawText(string text, Vector2 position, Color color, FontAlign align = Left)` | settings font/size |
| `DrawText(string text, Vector2 position, Color color, int height, FontAlign align = Left)` | settings font |
| `DrawText(string text, Vector2N position, Color color, int height, FontAlign align = Left)` | settings font |
| `DrawText(string text, Vector2 position, Color color, string fontName = null, FontAlign align = Left)` | named font, font's own size |
| `DrawText(string text, Vector2N position, Color color, string fontName = null, FontAlign align = Left)` | named font, font's own size |
| `DrawText(string text, Vector2N position, Color color, int height, string fontName, FontAlign align = Left)` | named font and height |

Returns: `Vector2N` — the measured size of the drawn text.

```csharp
// Returns the size, useful for laying out the next line
var size = Graphics.DrawText("Hello", new Vector2(x, y), Color.White);
Graphics.DrawText("World", new Vector2(x, y + size.Y), Color.Yellow);
```

### MeasureText

Measures text without drawing it (delegates to `ImGui.CalcTextSize`).

| Overload | Notes |
| --- | --- |
| `Vector2N MeasureText(string text)` | measures with the current ImGui font |
| `Vector2N MeasureText(string text, int height)` | `height` is currently ignored; same result |

## Lines, boxes and frames

### DrawLine

Straight line between two points.

| Overload |
| --- |
| `void DrawLine(Vector2N p1, Vector2N p2, float borderWidth, Color color)` |
| `void DrawLine(Vector2 p1, Vector2 p2, float borderWidth, Color color)` |

### DrawBox (filled rectangle)

| Overload | Notes |
| --- | --- |
| `void DrawBox(Vector2N p1, Vector2N p2, Color color, float rounding = 0)` | corner points |
| `void DrawBox(Vector2 p1, Vector2 p2, Color color, float rounding = 0)` | corner points |
| `void DrawBox(RectangleF rect, Color color)` | rectangle |
| `void DrawBox(RectangleF rect, Color color, float rounding)` | rectangle, rounded corners |

`p1`/`p2` are the top-left and bottom-right corners. `rounding` is the corner radius in pixels.

### DrawFrame (outlined rectangle)

| Overload | Notes |
| --- | --- |
| `void DrawFrame(Vector2N p1, Vector2N p2, Color color, float rounding, int thickness, int flags)` | corner points |
| `void DrawFrame(Vector2 p1, Vector2 p2, Color color, int thickness)` | corner points, no rounding |
| `void DrawFrame(RectangleF rect, Color color, float rounding, int thickness, int flags)` | rectangle |
| `void DrawFrame(RectangleF rect, Color color, int thickness)` | rectangle, no rounding |

`thickness` is the border width in pixels. `flags` maps to ImGui's `ImDrawFlags` corner flags
(which corners to round); pass `0` for the default behavior.

## Images

Images are drawn either through the **sprite renderer** (`DrawImage`) or through the **ImGui
draw list** (`DrawImageGui`). UV rectangles select a sub-region of a texture in normalized
`0..1` coordinates; the default UV is the full texture `(0, 0, 1, 1)`.

### DrawImage

| Overload | Notes |
| --- | --- |
| `void DrawImage(string fileName, RectangleF rectangle)` | white, full UV |
| `void DrawImage(string fileName, RectangleF rectangle, Color color)` | full UV |
| `void DrawImage(string fileName, RectangleF rectangle, RectangleF uv)` | white tint |
| `void DrawImage(string fileName, RectangleF rectangle, RectangleF uv, Color color)` | sub-region + tint |
| `void DrawImage(AtlasTexture atlasTexture, RectangleF rectangle)` | white, atlas UV |
| `void DrawImage(AtlasTexture atlasTexture, RectangleF rectangle, Color color)` | atlas UV + tint |

The `AtlasTexture` overloads use the texture's own `AtlasFileName` and `TextureUV` (see
[Textures](#textures-and-atlases) below). `fileName` must have been loaded first via
[`InitImage`](#initimage) (or by `GetAtlasTexture`).

### DrawImageGui

Draws through the ImGui background draw list instead of the sprite renderer.

| Overload |
| --- |
| `void DrawImageGui(string fileName, RectangleF rectangle, RectangleF uv)` |
| `void DrawImageGui(string fileName, Vector2N TopLeft, Vector2N BottomRight, Vector2N TopLeft_UV, Vector2N BottomRight_UV)` |

### InitImage

Loads a PNG so it can be drawn later. Returns `true` on success.

```csharp
bool InitImage(string name, bool textures = true)
```

When `textures` is `true` the path is resolved under the plugin's `textures/` folder
(`textures/{name}`). Pass `false` to use `name` as-is (e.g. an absolute path). Call it once,
typically from `Initialise()`.

```csharp
// From the plugin's textures/ folder
Graphics.InitImage("Direction-Arrow.png");
// Absolute path, resolved as-is
Graphics.InitImage(Path.Combine(DirectoryFullName, "textures\\back.png").Replace('\\', '/'), false);
```

### DisposeTexture

```csharp
void DisposeTexture(string name)
```

Releases a previously loaded texture from the renderer's cache.

## Fonts

A loaded font is a `FontContainer` (`namespace ExileCore.RenderQ`), an immutable struct
wrapping the native ImGui atlas plus its `Name` and pixel `Size`:

```csharp
public unsafe struct FontContainer
{
    public ImFont* Atlas { get; }
    public string Name { get; }
    public int Size { get; }
}
```

`Graphics` exposes two of them:

| Member | Meaning |
| --- | --- |
| `FontContainer Font` | the font currently selected in core settings |
| `FontContainer LastFont` | the most recently used font |

When `DrawText` receives a `fontName`, that font is selected; if it isn't loaded, the engine
falls back to the first available font and logs an error. When `height` is `-1`, the font's own
`Size` is used. Fonts are keyed by name (e.g. `"FrizQuadrataITC:13"`, where the suffix is the
size).

### FontAlign

```csharp
namespace ExileCore.Shared.Enums;

public enum FontAlign
{
    Left,    // position is the left edge (default)
    Center,  // position is the horizontal center
    Right    // position is the right edge
}
```

Alignment only shifts the X coordinate; the Y coordinate is unaffected.

## Textures and atlases

For multiple icons packed into one image, use an **atlas**. Provide a `textures/` folder in your
plugin with one PNG and a matching `*.json` config (from the *Free Texture Packer* tool), then
look up named sub-textures with `GetAtlasTexture` (declared on `BaseSettingsPlugin`, see
[plugins.md](plugins.md)):

```csharp
AtlasTexture GetAtlasTexture(string textureName)
```

On first call it loads the atlas (config + PNG) from the plugin's `textures/` folder and calls
`InitImage` for the PNG; it returns `null` if the atlas is missing. The returned `AtlasTexture`
(`namespace ExileCore.Shared.AtlasHelper`) carries the data the `DrawImage(AtlasTexture, ...)`
overloads need:

```csharp
public class AtlasTexture
{
    public string TextureName { get; }
    public string AtlasFilePath { get; }
    public string AtlasFileName { get; }
    public RectangleF TextureUV { get; }   // normalized sub-region within the atlas
}
```

```csharp
var icon = GetAtlasTexture("my-icon");
if (icon != null)
    Graphics.DrawImage(icon, new RectangleF(x, y, 32, 32), Color.White);
```

### SpriteHelper

`SpriteHelper` (`namespace ExileCore.Shared.Helpers`) is a small static helper that computes
normalized UV rectangles for sprites laid out on a fixed grid — primarily the built-in map-icon
sheets. Overloads accept a `MapIconsIndex` / `MyMapIconsIndex`, a linear index, or a
column/row pair plus the sheet dimensions, and return a `RectangleF` UV you can pass to
`DrawImage`.

## Drawing your own ImGui windows

ImGui.NET is available; you can call `ImGui.*` directly inside `Render()` to build custom
windows, controls and draw lists. A common overlay pattern is to open a borderless, input-less,
background-less window sized to a region and draw into its draw list:

```csharp
ImGui.SetNextWindowSize(new Vector2N(_rect.Width, _rect.Height));
ImGui.SetNextWindowPos(new Vector2N(_rect.Left, _rect.Top));
ImGui.Begin("my_plugin_drawregion",
    ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove |
    ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoSavedSettings |
    ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus |
    ImGuiWindowFlags.NoBackground);

var drawList = ImGui.GetWindowDrawList();
// ... drawList.AddLine(...), drawList.AddCircle(...), etc.

ImGui.End();
```

`Graphics.TransparentState` reports whether the overlay is currently in its transparent
(click-through) state; check it when you need to know if the overlay is accepting input. For
standard menu/settings controls, see [ui-elements.md](ui-elements.md).

## Example: label an entity at its screen position

Convert an entity's world position to screen space, then draw a translucent box behind a
centered label (adapted from
[vadash/ProximityAlert](https://github.com/vadash/ProximityAlert) `Proximity.cs`):

```csharp
public override void Render()
{
    foreach (var entity in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
    {
        // World -> screen (see coordinates.md)
        var screenPos = GameController.IngameState.Camera.WorldToScreen(entity.Pos);

        var label = entity.RenderName;
        var textSize = Graphics.MeasureText(label, 10);

        // Background box behind the label
        Graphics.DrawBox(
            new RectangleF(screenPos.X - textSize.X / 2, screenPos.Y - 7, textSize.X, 13),
            new Color(0, 0, 0, 200));

        // Centered label on top
        Graphics.DrawText(label, new Vector2(screenPos.X, screenPos.Y), Color.White, 10, FontAlign.Center);
    }
}
```

See also: [coordinates.md](coordinates.md) (world/grid → screen), [ui-elements.md](ui-elements.md)
(menu controls), [plugins.md](plugins.md) (`GetAtlasTexture`, plugin lifecycle),
[../architecture.md](../architecture.md) (DX11 + ImGui backend).

## Source

- `Core/Graphics.cs` — the `Graphics` facade and all overloads
- `Core/RenderQ/ImGuiRender.cs` — `DrawText`, `MeasureText`, `DrawImage`, `TransparentState`, `CurrentFont`
- `Core/RenderQ/SpritesRender.cs` — `DrawImage`, `LoadPng`
- `Core/RenderQ/DX11.cs` — DirectX 11 backend and texture cache
- `Core/RenderQ/FontContainer.cs` — `FontContainer`
- `Core/Shared/Enums/FontAlign.cs` — `FontAlign`
- `Core/Shared/AtlasHelper/AtlasTexture.cs` — `AtlasTexture`
- `Core/Shared/Helpers/SpriteHelper.cs` — sprite-sheet UV helpers
- `Core/BaseSettingsPlugin.cs` — `GetAtlasTexture`
