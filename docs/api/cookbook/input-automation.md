# Recipe: input automation & humanized input

Patterns for moving the real cursor and pressing keys safely from a plugin: gate on window focus, prefer the `Input` API over hand-rolled P/Invoke, drive movement/clicks from a coroutine, and — when the **InputHumanizer** plugin is installed — borrow its serialized, randomized-delay input service through the `PluginBridge`.

[API reference index](../README.md) · [cookbook index](README.md)

This recipe is for authorized plugin development (PickItV2, Stashie, EZVendor and similar). It documents how the reference plugins drive synthetic input, adapted to this fork. The low-level surface is documented in [../input.md](../input.md); the bridge mechanism in [../plugins.md](../plugins.md); coroutines and `Job` in [../utilities.md](../utilities.md).

## The two rules of synthetic input

1. **Only act when the game window is foreground.** Synthetic mouse/keyboard events go to whatever window has focus, so moving the cursor or clicking while the user has alt-tabbed away clicks *their other app*. Every reference plugin checks `GameController.Window.IsForeground()` before doing anything.
2. **Never block the main thread.** `Input.Click`, `Input.SetCursorPos` and `Input.KeyDown`/`KeyUp` return instantly, but humans don't click instantly. The delay between move → click → next action must come from a coroutine yield (`yield return new WaitTime(...)`), not `Thread.Sleep`. `Thread.Sleep` inside `Tick`/`Render` freezes the whole overlay. (MineDetonator and TC_Pickit's older `Mouse` helper both use `Thread.Sleep`; that is the anti-pattern this recipe replaces.)

## Gating on focus + a hotkey

PickItV2's work-mode gate is the canonical shape: bail unless the window is foreground, the plugin is enabled, and the abort key (Escape) is not held — then check the work hotkey. It reads keys with `Input.GetKeyState` (live OS poll, no registration needed):

```csharp
private bool ShouldRun()
{
    if (!GameController.Window.IsForeground() ||
        !Settings.Enable ||
        Input.GetKeyState(Keys.Escape))
        return false;

    return Input.GetKeyState(Settings.PickUpKey.Value);   // HotkeyNode -> Keys
}
```

`GetKeyState` vs the cached `IsKeyDown` / `HotkeyNode.PressedOnce()` (edge-triggered, needs `Input.RegisterKey`) is covered in [../input.md](../input.md#key-state). Register hotkeys in `Initialise` and re-register on edit:

```csharp
public override bool Initialise()
{
    Input.RegisterKey(Settings.PickUpKey);
    Input.RegisterKey(Keys.Escape);
    Settings.PickUpKey.OnValueChanged += () => Input.RegisterKey(Settings.PickUpKey);
    return true;
}
```

## Move + click with the raw `Input` API

This is the fallback path — what you do when InputHumanizer is **not** installed. Convert the target element's client rect to a screen point, move, wait, click. Drive it from a coroutine so the waits don't block:

```csharp
// Coroutine: hover a UI element, then left-click it.
private IEnumerator ClickElement(Element element)
{
    // GetWindowRectangleTimeCache.TopLeft is the client->screen offset (SharpDX Vector2).
    var offset = GameController.Window.GetWindowRectangleTimeCache.TopLeft;

    // ClickRandom jitters the point inside the rect so clicks don't always land dead-center.
    var screenPos = element.GetClientRect().ClickRandom(5, 3) + offset;

    yield return Input.SetCursorPositionSmooth(screenPos);   // smooth move (coroutine)
    yield return new WaitRandom(30, 70);                     // human-ish settle delay
    Input.Click(MouseButtons.Left);
    yield return new WaitTime(50);
}
```

Start it with `Core.ParallelRunner.Run(ClickElement(el), this, "MyPlugin_Click")` (de-duplicated by name+owner — see [../utilities.md](../utilities.md#coroutines-iyieldbase--waittime--waitfunction)).

Fork members used here, all verified against `Core/`:

| Member | Source | Notes |
| --- | --- | --- |
| `GameController.Window.IsForeground()` | `Core/GameWIndow.cs` | Focus gate. |
| `GameController.Window.GetWindowRectangleTimeCache` | `Core/GameWIndow.cs` | `.TopLeft` is the client→screen offset (SharpDX `Vector2`). |
| `Element.GetClientRect()` | UI element | Client-space rect; add the offset for a screen point. |
| `RectangleF.ClickRandom(int x = 3, int y = 3)` | `Core/Shared/Helpers/MiscHelpers.cs` | Random point inside the rect, inset by the margins. Returns **SharpDX** `Vector2`. |
| `Input.SetCursorPositionSmooth(Vector2)` | `Core/Input.cs` | Coroutine; `Vector2.SmoothStep` toward the target, falls back to a single `SetCursorPos` for short hops. `yield return` it. |
| `Input.SetCursorPos(Vector2)` | `Core/Input.cs` | Instant move (screen-space SharpDX `Vector2`), then a `MouseMove` to wake the cursor. |
| `Input.Click(MouseButtons)` | `Core/Input.cs` | `Left`/`Right` only; combined down+up via `mouse_event`. |
| `Input.KeyPress(Keys)` | `Core/Input.cs` | Coroutine: down, short delay, up. For a held key use `KeyDown`/`KeyUp` directly. |
| `WaitTime` / `WaitRandom` | `Core/Shared/Coroutine.cs` | Yield conditions; `WaitRandom(min, max)` is itself a cheap humanizer. |

> `Input.SetCursorPositionSmooth` in this fork is the descendant of TC_Pickit's `Mouse.SetCursorPosHuman` — same `Vector2.SmoothStep` interpolation with a per-step `WaitTime(1)`. Prefer the built-in over copying TC_Pickit's `Mouse` class, whose other helpers (`SetCursorPosAndLeftClick`, `SetCursorPosition`) block with `Thread.Sleep`.

### Rate-limiting clicks without a coroutine

If you must act from inside `Tick`/`Render` (e.g. PickItV2 runs its picker from `Render`), gate clicks on a `Stopwatch` instead of sleeping:

```csharp
private readonly Stopwatch _sinceLastClick = Stopwatch.StartNew();

if (_sinceLastClick.ElapsedMilliseconds > Settings.PauseBetweenClicks)
{
    Input.Click(MouseButtons.Left);
    _sinceLastClick.Restart();
}
```

### Prefer `Input` over hand-rolled P/Invoke

MineDetonator ships its own `Keyboard` class (`keybd_event` + `Thread.Sleep`) and TC_Pickit ships its own `Mouse` class (`mouse_event` + `SetCursorPos`) because they predate the `Input` API. On this fork that is redundant: `Input.KeyDown`/`KeyUp`/`KeyPress`, `Input.Click`, `Input.SetCursorPos`/`SetCursorPositionSmooth` and `Input.VerticalScroll` wrap the exact same Win32 calls (`Core/Shared/WinApi.cs`) and the engine refreshes key state for you. Use `Input`; reach for raw P/Invoke only for calls the fork does not expose (e.g. `BlockInput`, used by AutoOpen's `BlockInputWhenClicking`) — see [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md).

## The InputHumanizer pattern (PluginBridge service)

[InputHumanizer](https://github.com/exApiTools/InputHumanizer) is a standalone plugin that owns a single global lock around input and adds Gaussian-distributed delays to every key/click plus interpolated cursor movement. Consumer plugins don't depend on its assembly — they borrow an `IInputController` at runtime through `GameController.PluginBridge` (see [../plugins.md](../plugins.md#pluginbridge-named-methods)). That keeps all of a plugin's input serialized and humanized when the user has InputHumanizer enabled, and lets your plugin degrade gracefully to the raw `Input` path when they don't.

### What InputHumanizer publishes

The plugin exposes acquisition methods that hand out a controller guarded by a `SemaphoreSlim` (only one plugin holds input at a time). The controller is `IDisposable`; disposing it releases the lock:

```csharp
// InputHumanizer side (paraphrased): one controller at a time, behind a semaphore.
public bool TryGetInputController(string requestingPlugin, out IInputController controller);
public SyncTask<IInputController> GetInputController(string requestingPlugin, TimeSpan waitTime);

public interface IInputController : IDisposable
{
    bool KeyDown(Keys key);
    SyncTask<bool> KeyUp(Keys key, bool releaseImmediately = false, CancellationToken ct = default);
    SyncTask<bool> Click(CancellationToken ct = default);
    SyncTask<bool> Click(Vector2 coordinate, CancellationToken ct = default);   // System.Numerics
    SyncTask<bool> MoveMouse(Vector2 coordinate, CancellationToken ct = default);
}
```

Under the hood each method calls `ExileCore.Input.*` (the same surface in [../input.md](../input.md)) but inserts a delay drawn from a Gaussian (`mean`, `stdDev`, clamped to `[min, max]`) and moves the cursor in interpolated steps. The semaphore is what makes it safe to share between PickItV2, Stashie, etc. without two plugins fighting over the mouse.

### Consumer wiring

A consumer fetches the acquisition delegate by name. `GetMethod<T>` returns `null` if InputHumanizer isn't loaded (plugin load order is not guaranteed), so always null-check:

```csharp
// T is the published delegate's signature; the name is the published method name.
var tryGet = GameController.PluginBridge
    .GetMethod<TryGetInputControllerDelegate>("InputHumanizer.TryGetInputController");

// delegate matching InputHumanizer's TryGetInputController(string, out IInputController)
public delegate bool TryGetInputControllerDelegate(string requestingPlugin, out IInputController controller);
```

Then acquire, use, and dispose the controller per action (the `using` releases the lock):

```csharp
private async SyncTask<bool> PickUpHumanized(Element label)
{
    if (!GameController.Window.IsForeground())
        return false;

    // ClickRandom and TopLeft are both SharpDX Vector2 — keep the screen point in SharpDX.
    var offset = GameController.Window.GetWindowRectangleTimeCache.TopLeft;
    SharpDX.Vector2 target = label.GetClientRect().ClickRandom(5, 3) + offset;

    var tryGet = GameController.PluginBridge
        .GetMethod<TryGetInputControllerDelegate>("InputHumanizer.TryGetInputController");

    if (tryGet != null && tryGet(Name, out var controller))
    {
        // Humanized path: serialized + delayed by InputHumanizer.
        // IInputController takes System.Numerics.Vector2, so convert with ToVector2Num().
        using (controller)
        {
            await controller.MoveMouse(target.ToVector2Num());
            await controller.Click();
        }
        return true;
    }

    // Fallback path: InputHumanizer not installed — drive the raw Input API ourselves.
    Core.ParallelRunner.Run(ClickAt(target), this, $"{Name}_Click");
    return true;
}

private IEnumerator ClickAt(SharpDX.Vector2 pos)
{
    yield return Input.SetCursorPositionSmooth(pos);   // SharpDX Vector2
    yield return new WaitRandom(30, 70);
    Input.Click(MouseButtons.Left);
}
```

Note the coordinate type split: `IInputController` takes `System.Numerics.Vector2`, while `Input.SetCursorPositionSmooth` / `Input.SetCursorPos` take **SharpDX** `Vector2`. `ClickRandom` and `GetWindowRectangleTimeCache.TopLeft` are both SharpDX, so keep the screen point in SharpDX for the raw `Input` path and convert to numeric with `SharpDX.Vector2.ToVector2Num()` (`Core/Shared/Helpers/Extensions.cs`) only when handing it to the controller.

> **Helper.** A C# bridge helper that wraps this `GetMethod` lookup + null-fallback is being ported under `proposals/InputHumanizer/` by a sibling worker; reference it once it lands rather than re-deriving the delegate plumbing in each plugin. Until then, copy the delegate + null-check shape above.

### Acquire-once vs per-action

`TryGetInputController` is non-blocking (it returns `false` immediately if another plugin holds the lock). `GetInputController(name, waitTime)` is the async form that waits up to `waitTime` for the lock, returning `null` on timeout. Use `TryGet` for opportunistic single clicks; use `GetInputController` + a longer `waitTime` when you have a multi-step sequence (a stash dump, a vendor sell) that should not interleave with another plugin's input. Either way, dispose the controller (a `using` block, or `Dispose()` in your `finally`) as soon as the sequence ends so other plugins can take the lock.

## Compatibility notes (this fork)

These symbols appear in the upstream plugins but are **not** in this fork's `Core/`. Don't copy them blindly:

| Upstream symbol | Status on this fork | Use instead |
| --- | --- | --- |
| `SyncTask<T>` / `TaskUtils` | **Not present.** InputHumanizer's `IInputController` and PickItV2's picker are built on these async-coroutine types from upstream `ExileCore.Shared`. | This fork's coroutine model is `IEnumerator` + `WaitTime`/`WaitFunction` via `Core.ParallelRunner.Run` ([../utilities.md](../utilities.md#coroutines-iyieldbase--waittime--waitfunction)). Adapt the `await controller.X()` calls accordingly, or gate the humanized path behind a feature check. See [../compatibility-exileapi-compiled.md](../compatibility-exileapi-compiled.md). |
| `Input.ForceMousePositionNum` | **Not present** (used by InputHumanizer's `Mouse` and AutoOpen to read/restore the cursor). | `Input.ForceMousePosition` (SharpDX, screen-space) or `Input.MousePositionNum` (cached, client-space). `ToVector2Num()` converts SharpDX → numeric. |
| `RectangleF.ClickRandomNum(...)` | **Not present** (PickItV2 uses it). | `RectangleF.ClickRandom(x, y)` (returns SharpDX `Vector2`), then `.ToVector2Num()` if you need `System.Numerics`. |
| `Kalon.CursorMover.GenerateMovements(...)` | External closed DLL bundled with InputHumanizer; not part of the fork. | Use `Input.SetCursorPositionSmooth` for interpolated movement; it covers the common case. |
| `Mouse.BlockInput` (Win32 `BlockInput`) | Not exposed by `Input`. | P/Invoke `user32.BlockInput` yourself if you truly need it (AutoOpen's "block input while clicking"); the fork deliberately installs no input hooks (see [../input.md](../input.md)). |

## Source repos

- [exApiTools/InputHumanizer](https://github.com/exApiTools/InputHumanizer) — `InputHumanizer.cs`, `Input/IInputController.cs`, `Input/InputController.cs`, `Input/InputLockManager.cs` (semaphore), `Input/Mouse.cs`, `Input/Delay.cs` (Gaussian).
- [exApiTools/PickItV2](https://github.com/exApiTools/PickItV2) — `PickIt.cs` (focus/hotkey gate, `Stopwatch` rate-limit, `PluginBridge` consumer of `MagicInput.CastSkillWithTarget`, smooth cursor + click coroutine).
- [exApiTools/TC_Pickit](https://github.com/exApiTools/TC_Pickit) — `Mouse.cs` (origin of the `SmoothStep` humanized-move coroutine and the `Thread.Sleep` anti-pattern).
- [exApiTools/MineDetonator](https://github.com/exApiTools/MineDetonator) — `Keyboard.cs` (hand-rolled `keybd_event`), `Core.cs` (delay gate via `DateTime`).
- [exApiTools/AutoOpen](https://github.com/exApiTools/AutoOpen) — cursor save/restore + `BlockInput` while clicking.
- [exApiTools/AutoQuit](https://github.com/exApiTools/AutoQuit) — focus-independent action (`RegisterKey` + `IsKeyDown`, then kill the TCP connection rather than synthetic input).
