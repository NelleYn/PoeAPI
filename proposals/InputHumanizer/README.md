# InputHumanizer bridge consumer (proposal)

> **EXPERIMENTAL — not compiled in this environment.** These files live under `proposals/` and are
> **not part of the build**. They target Windows + a running Path of Exile session and the
> ExileApi/ExileCore fork API; they cannot be compiled or run here. Treat this as a ready-to-move-in
> candidate that a maintainer can review, wire into a plugin project, and verify in-game. This is
> authorized plugin-development tooling.

## What InputHumanizer is

[InputHumanizer](https://github.com/exApiTools/InputHumanizer) is a community ExileCore plugin that
provides *humanized* keyboard and mouse input: randomized (Gaussian) press/release delays, interpolated
mouse movement, and an **exclusive input lock** so only one consumer drives input at a time. Plugins do
not reference it directly; instead InputHumanizer publishes an accessor through ExileCore's
`PluginBridge`, and a consumer fetches it by name and asks for an input-controller "session" (an
`IDisposable` — dispose to release the lock).

This proposal adds **`InputHumanizerBridge.cs`**, a small helper that:

- Fetches the service through `GameController.PluginBridge.GetMethod<...>(...)` and acquires the
  exclusive input-lock controller.
- Exposes the controller through a small `IDisposable` `IHumanizedInput` session: the synchronous
  `KeyDown`, an `IsHumanized` flag, and `RawController` for awaiting the humanized async actions.
- Provides static synchronous convenience (`TapKey`, `ClickAt`) and **falls back to the fork's
  synchronous `ExileCore.Input` API** when InputHumanizer is not loaded.
- Deliberately does **not** try to block-complete the humanized async actions — see the reflection
  section for why that is impossible (and was removed) in this fork.

## The bridge contract (confirmed shape)

### What InputHumanizer publishes

Confirmed from the cloned InputHumanizer source
(`InputHumanizer.cs`, `Input/IInputController.cs`, `Input/InputLockManager.cs`, `Input/InputController.cs`):

```csharp
// InputHumanizer.cs:14  — the public accessor that returns a locked controller session
public async SyncTask<IInputController> GetInputController(string requestingPlugin, TimeSpan waitTime);

// InputHumanizer.cs:26  — non-blocking variant
public bool TryGetInputController(string requestingPlugin, out IInputController controller);
```

```csharp
// Input/IInputController.cs  — the session handed back (IDisposable: dispose releases the lock)
public interface IInputController : IDisposable
{
    bool KeyDown(Keys key);
    SyncTask<bool> KeyUp(Keys key, bool releaseImmediately = false, CancellationToken cancellationToken = default);
    SyncTask<bool> Click(CancellationToken cancellationToken = default);
    SyncTask<bool> Click(Vector2 coordinate, CancellationToken cancellationToken = default);
    SyncTask<bool> MoveMouse(Vector2 coordinate, CancellationToken cancellationToken = default);
    SyncTask<bool> MoveMouse(Vector2 coordinate, int maxInterpolationDistance, int minInterpolationDelay,
                             int maxInterpolationDelay, CancellationToken cancellationToken = default);
}
```

The lock itself is a `SemaphoreSlim(1)` in `Input/InputLockManager.cs`; `InputController.Dispose()`
(and its finalizer) release it via `Manager.ReleaseController()`. So the consumer contract is: **acquire
a controller, use it, dispose it** — exactly an input-lock that returns an `IDisposable` with humanized
`KeyDown`/`KeyUp`/`Click`/move + delay, as expected.

### How consumers fetch it — and the one assumption flagged

Consumers read named delegates with `GameController.PluginBridge.GetMethod<T>(string)`
(confirmed in PickItV2 `PickIt.cs:487`, e.g. `GetMethod<Action<Entity, uint>>("MagicInput.CastSkillWithTarget")`).
The corresponding `SaveMethod(name, delegate)` is how a provider registers (PickItV2 `PickIt.cs:64-66`).

> ⚠️ **Assumption (flagged, not silently guessed).** The **cloned InputHumanizer HEAD does *not* contain
> the `SaveMethod("InputHumanizer.GetInputController", …)` registration call** — `GetInputController` /
> `TryGetInputController` appear only as public plugin methods (`InputHumanizer.cs:14,26`), and a
> repo-wide search for `SaveMethod`/`PluginBridge` in the InputHumanizer clone returned nothing. The
> bridge **key** `"InputHumanizer.GetInputController"` used here is the documented community convention,
> not a line we could cite from this specific clone (the clone is shallow and could not be unshallowed
> offline; the registration may live in a newer commit or be auto-registered by the host). The key is a
> single `const` (`InputHumanizerBridge.GetInputControllerKey`) — **verify/adjust it against the
> InputHumanizer build the user actually runs** before relying on it.
>
> Also note: PickItV2 is a *consumer* example of the `PluginBridge` pattern, but in this clone PickItV2
> does **not** itself call InputHumanizer, so it confirms the `GetMethod`/`SaveMethod` mechanics, not the
> InputHumanizer key specifically.

### Why this helper is loosely typed — and what it deliberately does *not* do

The accessor's return type is `SyncTask<IInputController>`, and `IInputController` lives inside the
InputHumanizer plugin assembly. **None of `SyncTask`, `IInputController`, or the `TaskUtils` coroutine
pump exist in this fork** — repo-wide searches for `SyncTask` and `TaskUtils` returned no results, and
`IInputController` is InputHumanizer-internal. So a consumer in this fork **cannot statically reference
the delegate type** `Func<string, TimeSpan, SyncTask<IInputController>>`, and — critically — **cannot
drive the humanized async methods to completion.**

`ExileCore.Shared.SyncTask<T>` is a *frame-pumped coroutine*: the proper consumer keeps the `SyncTask`
and advances it one frame at a time via `TaskUtils.RunOrRestart` (exactly how PickItV2 drives its
`_pickUpTask`, see `PickIt.cs:33,145` and the `await TaskUtils.NextFrame()` calls in `PickIt.cs:472,506,517`).
The humanized controller methods `Click`/`KeyUp`/`MoveMouse` are `async SyncTask<bool>` that
`await Task.Delay(...)` internally (`InputController.cs`), so a single blocking
`GetAwaiter().GetResult()` would either return a premature/default result or throw — it does **not**
synchronously complete them. An earlier draft of this helper tried exactly that; it was removed because
it is incorrect against `SyncTask` semantics.

Given those constraints, `InputHumanizerBridge.cs` does only what is provably correct here:

- **Acquires the controller** by fetching the registration as a bare `Delegate` (`GetMethod<Delegate>(key)`)
  and invoking it with `DynamicInvoke`. Lock acquisition usually completes synchronously when the lock is
  free; the helper checks the awaiter's `IsCompleted` and returns `null` (so the caller retries next tick)
  rather than forcing a premature `GetResult` when the lock is contended.
- **Forwards `KeyDown`**, the one genuinely synchronous controller method (the interface returns a plain
  `bool`, not a `SyncTask`).
- **Exposes the raw controller** via `IHumanizedInput.RawController` so advanced consumers that *do* have
  a `SyncTask` pump (e.g. a fork that adds `ExileCore.Shared`) can `await` the humanized async methods
  from inside their own coroutine.
- **Provides static synchronous convenience** (`TapKey`, `ClickAt`) and a synchronous fallback session,
  all backed by the fork's immediate `Input` API — always safe, never humanized.

If a future fork adds `SyncTask`/`TaskUtils`/`IInputController`, the reflection can be swapped for a
directly-typed delegate and the async methods exposed first-class.

## Fork members this builds on (confirmed in `master`)

All verified by reading the files on `master`:

| Member | Location |
| --- | --- |
| `PluginBridge.GetMethod<T>(string) where T : class` | `Core/GameController.cs:24` |
| `PluginBridge.SaveMethod(string, object)` | `Core/GameController.cs:32` |
| `GameController.PluginBridge` field | `Core/GameController.cs:52` |
| `ExileCore.Input` (static input class) | `Core/Input.cs:19` |
| `Input.Click(MouseButtons)` | `Core/Input.cs:156` |
| `Input.SetCursorPos(Vector2)` (SharpDX `Vector2`) | `Core/Input.cs:149` |
| `Input.KeyDown(Keys)` | `Core/Input.cs:198` |
| `Input.KeyUp(Keys)` | `Core/Input.cs:204` |
| `Input.ForceMousePosition` | `Core/Input.cs:52` |
| `Input.MousePositionNum` | `Core/Input.cs:58` |

> **Type note for integrators.** `ExileCore.Input.SetCursorPos`/`Click` take **SharpDX** `Vector2`
> (`Core/Input.cs` uses `using SharpDX;`), whereas the InputHumanizer controller and this helper's public
> surface use **`System.Numerics.Vector2`**. There is **no implicit conversion** between the two, and the
> fork only ships a one-way `ToVector2Num()` (SharpDX → Numerics). The helper therefore includes a tiny
> `VectorInterop.ToSharpDx()` extension and calls `coordinate.ToSharpDx()` before handing a position to
> `Input.SetCursorPos`. If your target project already has a Numerics→SharpDX converter, prefer it and
> delete `VectorInterop`.

## Integration steps

1. Copy `proposals/InputHumanizer/InputHumanizerBridge.cs` into your plugin project (namespace
   `ExileCore.InputHumanizing`, or rename to your own).
2. Construct once with your plugin's `GameController` and a stable name:

   ```csharp
   var humanizer = new InputHumanizerBridge(GameController, "MyPlugin");
   ```

3. **Simple, always-safe path** — synchronous (non-humanized) taps/clicks that work with or without the
   service loaded:

   ```csharp
   InputHumanizerBridge.ClickAt(targetScreenPos); // System.Numerics.Vector2
   InputHumanizerBridge.TapKey(Keys.Q);
   ```

4. **Humanized path** — acquire a session, hold the lock, and `Dispose` to release it. The synchronous
   `KeyDown` works directly. The humanized async actions (`Click`/`KeyUp`/`MoveMouse`) must be `await`-ed
   from inside your own `SyncTask` coroutine, via `RawController`, because this fork has no coroutine pump
   to complete them for you (see the reflection section above):

   ```csharp
   using var input = humanizer.TryAcquire(TimeSpan.FromSeconds(1));
   if (input == null)
       return; // service present but lock busy / not granted — try again next tick

   input.KeyDown(Keys.Q); // synchronous on both paths

   if (input.IsHumanized && input.RawController is var ctrl and not null)
   {
       // In a fork that has ExileCore.Shared, cast `ctrl` to InputHumanizer's IInputController
       // (or reflect over it) and `await ctrl.Click(targetScreenPos, ct)` inside your coroutine.
   }
   // input.IsHumanized == false means InputHumanizer wasn't loaded (use TapKey/ClickAt instead).
   ```

   `TryAcquire` returns:
   - a **humanized** session when the service granted the lock,
   - a **fallback** (`IsHumanized == false`) session when the service is not loaded, or
   - `null` when the service is present but the lock could not be granted (busy/contended) — retry later.

   For a non-blocking attempt use `TryAcquireImmediately(out var input)`.

## Dependency note

For humanized behavior the user **must install and enable the InputHumanizer plugin**
(<https://github.com/exApiTools/InputHumanizer>) alongside the consuming plugin. Without it, this helper
silently degrades to the immediate, non-humanized `ExileCore.Input` fallback (no exception, no lock).
Verify the InputHumanizer build's bridge **key** against `InputHumanizerBridge.GetInputControllerKey`
before relying on the humanized path (see the flagged assumption above).

## Verification performed (no build/run)

- Cloned `exApiTools/InputHumanizer` and `exApiTools/PickItV2` — **no 404**.
- Bridge contract read directly from the InputHumanizer clone (`InputHumanizer.cs`,
  `Input/IInputController.cs`, `Input/InputLockManager.cs`, `Input/InputController.cs`).
- `GetMethod`/`SaveMethod` consumer mechanics confirmed in PickItV2 (`PickIt.cs:64-66`, `:487`).
- Every fork member used was confirmed by reading `master` (`Core/GameController.cs`, `Core/Input.cs`) —
  see the table above with file:line.
- Confirmed **absence** of `SyncTask`, `TaskUtils`, and `IInputController` in this fork (this drives the
  loosely-typed design and the decision *not* to block-complete the humanized async actions).
- Confirmed `SyncTask` is frame-pumped by the consumer in PickItV2 (`PickIt.cs:33,145`, plus
  `await TaskUtils.NextFrame()` at `PickIt.cs:472,506,517`), which is why a single
  `GetAwaiter().GetResult()` cannot complete the humanized async methods.
- No `dotnet build`/`test` was run (Windows + game required; not possible here).
