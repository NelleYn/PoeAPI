# Input & game window

Synthetic mouse/keyboard input, per-frame key tracking, and game-window geometry for plugins. See the [API reference index](README.md).

Namespace: `ExileCore`. `Keys` and `MouseButtons` are `System.Windows.Forms`; `Vector2` / `RectangleF` are SharpDX.

> **Caveat.** This fork deliberately installs **no mouse/keyboard hooks** ("No hooks mouse and keyboard for prevent lag when debug", per the README). Key state is therefore polled each frame from the OS (`GetKeyState`), not captured by a hook. The `Input` automation methods (`Click`, `KeyPress`, `SetCursorPos`, …) drive the real cursor and keyboard through the Win32 API, so they interact with the foreground game window the same way a person would. They exist for automation plugins such as PickItV2, Stashie and EZVendor.

## `Input` (static class)

Source: `Core/Input.cs`. Tracked key states are refreshed once per frame by the engine via `Input.Update(IntPtr)` (called from `Core.cs`); plugins do not call `Update` themselves.

### Mouse position

| Member | Signature | Notes |
| --- | --- | --- |
| `MousePosition` | `static Vector2 { get; }` | Cursor position captured during the last `Update` (in window client coordinates — `Update` calls `WinApi.GetCursorPosition(windowPtr)`). |
| `ForceMousePosition` | `static Vector2 { get; }` | Live cursor position read directly from the OS, in **screen** coordinates. |
| `MousePositionNum` | `static System.Numerics.Vector2 { get; }` | Cached `MousePosition` converted to `System.Numerics.Vector2`. |

### Key state

| Member | Signature | Notes |
| --- | --- | --- |
| `IsKeyDown` | `static bool IsKeyDown(int nVirtKey)` | Overload that casts the virtual-key code to `Keys`. |
| `IsKeyDown` | `static bool IsKeyDown(Keys nVirtKey)` | Returns the **cached** state from the last `Update`. The key must be registered first; querying an unregistered key logs an error (debug build) and throws (`KeyNotFoundException`). |
| `GetKeyState` | `static bool GetKeyState(Keys key)` | Live state read straight from the OS (`WinApi.GetKeyState(key) < 0`). Works without registration. |
| `RegisterKey` | `static void RegisterKey(Keys key)` | Adds the key to the tracked set so `IsKeyDown(Keys)` returns a valid value. Call in your plugin's `Initialise`. |
| `ReleaseKey` | `static event EventHandler<Keys>` | Raised during `Update` when a tracked key transitions down → up. |

### Synthetic keyboard

| Member | Signature | Notes |
| --- | --- | --- |
| `KeyDown` | `static void KeyDown(Keys key)` | Synthetic key-down via `keybd_event`. |
| `KeyUp` | `static void KeyUp(Keys key)` | Synthetic key-up via `keybd_event`. |
| `KeyPress` | `static IEnumerator KeyPress(Keys key)` | **Coroutine.** Presses down, yields a short delay, releases. `yield return` it from a coroutine. |
| `KeyDown` / `KeyUp` | `static void (Keys key, IntPtr handle)` | Posts `WM_KEYDOWN`/`WM_KEYUP` to a specific window handle via `SendMessage`. |
| `KeyPressRelease` | `static void KeyPressRelease(Keys key)` / `(Keys key, IntPtr handle)` | Rate-limited (≈10 ms) toggle of down/up; call repeatedly to alternate. |

### Synthetic mouse

| Member | Signature | Notes |
| --- | --- | --- |
| `Click` | `static void Click(MouseButtons buttons)` | `Left` / `Right` only: moves, then sends a combined down+up via `mouse_event`. |
| `LeftDown` | `static void LeftDown()` | Presses the left button (`MOUSEEVENTF_LEFTDOWN`). |
| `LeftUp` | `static void LeftUp()` | Releases the left button (`MOUSEEVENTF_LEFTUP`). |
| `MouseMove` | `static void MouseMove()` | Sends a zero-delta `MOUSEEVENTF_MOVE` to "wake" the cursor at its current position. |
| `SetCursorPos` | `static void SetCursorPos(Vector2 vec)` | Moves the cursor to a **screen** position, then calls `MouseMove`. |
| `SetCursorPositionSmooth` | `static IEnumerator SetCursorPositionSmooth(Vector2 vec)` | **Coroutine.** Steps the cursor toward the target with `Vector2.SmoothStep`; for short distances falls back to a single `SetCursorPos`. |
| `VerticalScroll` | `static void VerticalScroll(bool forward, int clicks)` | Wheel scroll; sends `clicks * 120` wheel delta (negated when `forward` is false). |

### `MOUSEEVENTF_*` constants

Public `const int` on `Input`, the raw Win32 `mouse_event` flags:

| Constant | Value |
| --- | --- |
| `MOUSEEVENTF_MOVE` | `0x0001` |
| `MOUSEEVENTF_LEFTDOWN` | `0x02` |
| `MOUSEEVENTF_LEFTUP` | `0x04` |
| `MOUSEEVENTF_RIGHTDOWN` | `0x0008` |
| `MOUSEEVENTF_RIGHTUP` | `0x0010` |
| `MOUSEEVENTF_MIDDOWN` | `0x0020` |
| `MOUSEEVENTF_MIDUP` | `0x0040` |
| `MOUSE_EVENT_WHEEL` | `0x800` |

## `HotkeyNode` integration

A settings property of type `HotkeyNode` (see [settings.md](settings.md)) holds a single `Keys` value and is editable in the menu. It implicitly converts to/from `Keys`, so it can be passed straight to `Input`:

```csharp
// In settings:
public HotkeyNode PickUpKey { get; set; } = Keys.F;

// In Initialise: register so cached IsKeyDown works, and re-register on edit.
Input.RegisterKey(Settings.PickUpKey);
Settings.PickUpKey.OnValueChanged += () => Input.RegisterKey(Settings.PickUpKey);
```

Then poll in `Tick`/your work loop. Two common patterns, both verified in source:

- **Live state** — `Input.GetKeyState(Settings.PickUpKey.Value)` (or just `Settings.PickUpKey`, via the implicit `Keys` conversion). Needs no registration. Used by PickItV2.
- **Edge-triggered** — `HotkeyNode.PressedOnce()` returns `true` exactly once per press; it uses the *cached* `Input.IsKeyDown`, so the key **must** be registered. `UnpressedOnce()` is the release edge. Used by Stashie / Radar.

## `GameWindow`

Source: `Core/GameWIndow.cs`. Wraps the PoE process window; reach it via `GameController.Window` (see [game-controller.md](game-controller.md)).

| Member | Signature | Notes |
| --- | --- | --- |
| `Process` | `Process { get; }` | The underlying game process. |
| `GetWindowRectangle` | `RectangleF GetWindowRectangle()` | Client rectangle, served from a ~200 ms time cache. `X`/`Y` are the client area's top-left in **screen** coordinates; `Width`/`Height` its size. |
| `GetWindowRectangleTimeCache` | `RectangleF { get; }` | Same value as the property form (the cached rectangle). |
| `GetWindowRectangleReal` | `RectangleF GetWindowRectangleReal()` | Reads from the OS immediately (`GetClientRect` + `ClientToScreen`), falling back to the last valid rectangle if the window reports an invalid size. |
| `IsForeground` | `bool IsForeground()` | Whether the game window is the foreground window. |
| `ScreenToClient` | `Vector2 ScreenToClient(int x, int y)` | Converts a screen-space point to window client coordinates. |

### Coordinates

`Graphics` draws in screen space aligned to the game window. The rectangle's top-left (`GetWindowRectangle().TopLeft`) is the offset between client-relative element positions and the screen position you must move the cursor to. To click a UI element, add that offset to the element's client rectangle — this is exactly what PickItV2 and Stashie do:

```csharp
var offset = GameController.Window.GetWindowRectangleTimeCache.TopLeft.ToVector2Num();
var screenPos = label.GetClientRect().Center.ToVector2Num() + offset;
Input.SetCursorPos(screenPos);   // SetCursorPos takes a screen-space SharpDX Vector2
```

## Examples

### Trigger an action when a hotkey is pressed

```csharp
public override bool Initialise()
{
    Input.RegisterKey(Settings.DropHotkey);
    Settings.DropHotkey.OnValueChanged += () => Input.RegisterKey(Settings.DropHotkey);
    return true;
}

public override void Tick()
{
    // Fires once per key press, not every frame it is held.
    if (Settings.DropHotkey.PressedOnce())
        StartDropCoroutine();
}
```

### Move + click for an automation plugin

```csharp
// Coroutine body: move the real cursor over a UI element and left-click it.
private IEnumerator ClickElement(Element element)
{
    var offset = GameController.Window.GetWindowRectangleTimeCache.TopLeft;
    Input.SetCursorPos(element.GetClientRect().Center + offset); // screen-space SharpDX Vector2
    Input.MouseMove();
    yield return new WaitTime(50);
    Input.Click(MouseButtons.Left);
}
```

(Adapted from PickItV2 `PickIt.cs` and Stashie `ActionsHandler.cs` / `ActionCoRoutine.cs`.)

## Source

- `Core/Input.cs`
- `Core/GameWIndow.cs`
- `Core/Shared/Nodes/HotkeyNode.cs`
- `Core/Shared/WinApi.cs` (underlying Win32 calls)
