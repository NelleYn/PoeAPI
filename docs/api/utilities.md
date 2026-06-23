# Utilities, logging & threading

> Logging, audio alerts, off-thread work and the small helper library a plugin author actually reaches for. For the plugin lifecycle (`Tick`, `Render`, the `LogMessage`/`LogError` helpers) see [plugins.md](plugins.md); for the cache primitives behind `ValidCache` see [caching.md](caching.md).

[API reference index](README.md)

All types below live in `ExileCore`, `ExileCore.Shared` or `ExileCore.Shared.Helpers`. `Color` is `SharpDX.Color` throughout.

---

## DebugWindow — on-screen logging

`ExileCore.DebugWindow` is the real logging API behind `BaseSettingsPlugin`'s helpers. Its `Log*` methods are **static**, so any code (not just a plugin instance) can call them. Each message is a transient, time-limited overlay line, **deduplicated by exact text**: logging the same string again resets its timer and bumps a repeat counter shown as `(N)text`.

| Method | Effect |
| --- | --- |
| `static void LogMsg(string msg)` | White message, shown for **1 second**. |
| `static void LogMsg(string msg, float time)` | White message, shown for `time` seconds. |
| `static void LogMsg(string msg, float time, Color color)` | Message with an explicit color and lifetime. |
| `static void LogError(string msg, float time = 2f)` | Red message (`Color.Red`), shown for `time` seconds (default 2). |

How messages reach the screen:

1. `LogMsg` queues / refreshes a `DebugMsgDescription` (text, expiry `DateTime`, color, repeat count).
2. `DebugWindow.Render()` (called by the engine each frame) draws every live message in the top-left corner over a `menu-background.png` strip, and lists the rolling history in the **Debug log** ImGui window when `CoreSettings.ShowDebugLog` is enabled.
3. When a message expires it is archived to history **and** forwarded to the file logger: red messages go to `Core.Logger.Error`, everything else to `Core.Logger.Information`. So an on-screen error also lands in `Logs\Error-{Date}.log`.

From inside a plugin you normally use the `BaseSettingsPlugin` wrappers, which just forward to `DebugWindow`:

| Plugin helper | Forwards to |
| --- | --- |
| `LogMsg(string)` | `DebugWindow.LogMsg(msg)` |
| `LogMessage(string, float time = 1f)` | `DebugWindow.LogMsg(msg, time, Color.GreenYellow)` |
| `LogMessage(string, float time, Color clr)` | `DebugWindow.LogMsg(msg, time, clr)` |
| `LogError(string, float time = 1f)` | `DebugWindow.LogError(msg, time)` |

Both forms are used in real plugins — `LogMessage(e.ToString(), 5)` inside a plugin (PickItV2 `Misc.cs`) and `DebugWindow.LogError("[Ground Items] Custom config folder does not exist.", 10)` from a static helper (PickItV2 `RulesDisplay.cs`).

### Logger (file logging)

`ExileCore.Logger` exposes `static ILogger Log`, a lazily-configured [Serilog](https://serilog.net/) logger that writes a separate rolling file per level under `Logs\` (`Info-{Date}.log`, `Debug-…`, `Warning-…`, `Error-…`, `Fatal-…`, `Verbose-…`). The engine also surfaces the active logger as `Core.Logger` (`static ILogger`), which is what `DebugWindow` archives expired messages through. Prefer `LogError`/`LogMessage` for things a user should see; reach for the logger directly only for high-volume diagnostics that should stay off the overlay.

---

## SoundController — audio alerts

`ExileCore.SoundController` plays WAV files through XAudio2. You do not construct it — grab the shared instance from `GameController.SoundController`.

| Member | Description |
| --- | --- |
| `void PlaySound(string name)` | Plays the named sound, loading & caching it on first use. No-op if the controller never initialised. |
| `void PreloadSound(string name)` | Loads and caches a sound ahead of time so the first `PlaySound` has no decode hitch. |
| `void SetVolume(float volume)` | Sets master output volume, `0..1`. |

Notes verified against source (`Core/SoundController.cs`):

- The engine constructs the controller with a sounds directory (relative to the app base directory). If that directory is **missing**, the controller logs a warning and silently becomes a no-op — every `PlaySound` returns without playing. Plan for sound being unavailable.
- `name` may be a bare key (`"alert"`) or include `.wav`; sounds are keyed both ways. Only `*.wav` files are supported.
- `PlaySound` plays an unknown name as a no-op and logs `Sound file: {name}.wav not found.` via `DebugWindow.LogError`.

A plugin triggers an alert by caching the controller in `Initialise` and calling `PlaySound` when its condition fires — exactly the pattern in the ProximityAlert plugin:

```csharp
// from Initialise()
_soundController = GameController.SoundController;

// when an alert condition is met (ProximityAlert/Proximity.cs, adapted)
private void PlaySound(string path)
{
    if (!Settings.PlaySounds) return;
    if (path != string.Empty)
        _soundController.PlaySound(Path.Combine(_soundDir, path).Replace('\\', '/'));
}
```

---

## Threading — running work off the main thread

ExileCore runs every plugin's `Tick`/`Render` on the main render thread. To do heavier work without dropping frames you have two complementary mechanisms.

### Returning a `Job` from `Tick()`

`IPlugin.Tick()` returns a `Job` (or `null`). `ExileCore.Job` is a unit of work scheduled on a worker thread:

```csharp
public Job(string name, Action work)   // wrap a delegate
public Action Work  { get; set; }
public string Name  { get; set; }      // diagnostics
public double ElapsedMs { get; set; }  // how long Work ran
public volatile bool IsStarted;        // handed to a worker
public volatile bool IsCompleted;      // finished (success or fail)
public volatile bool IsFailed;         // Work threw
```

What the engine does with your returned job each frame (`Core.cs`):

- If the **threads** core setting is `> 0`, the job is handed to `MultiThreadManager.AddJob(job)` and the frame waits (bounded by a job timeout) until it completes via `SpinUntil`. Your `Work` runs on a pool worker thread.
- If threads are `0` (multithreading off), the engine just runs `job.Work()` inline on the main thread.

So a returned `Job` is **resolved within the same frame** — it is "offload this frame's heavy work to a worker", not "fire and forget". Return `null` (the `BaseSettingsPlugin` default) when there is nothing to offload.

```csharp
public override Job Tick()
{
    if (!ShouldDoExpensiveWork())
        return null;

    // Build a snapshot on the main thread, crunch it on a worker.
    var snapshot = CaptureEntities();
    return new Job($"{Name}_Analyze", () =>
    {
        _results = AnalyzeExpensive(snapshot); // runs off the main thread
    });
}
```

Cautions: `Work` runs on another thread, so don't touch ImGui or do rendering inside it — build results there and draw them in `Render`. Exceptions thrown by `Work` are caught by the worker, logged via `DebugWindow.LogError`, and flip `IsFailed` (they don't crash the host). Jobs whose work exceeds `MultiThreadManager`'s critical budget (750 ms) get their worker thread repaired, with a warning on the overlay — keep job work short.

In practice most reference plugins return `null` from `Tick` and use the coroutine runner (below) instead — PickItV2 and Stashie both have `public override Job Tick()` that returns `null`.

### `MultiThreadManager`

`ExileCore.MultiThreadManager` owns the worker-thread pool. You rarely touch it directly (the engine drives it), but it is reachable as `GameController.MultiThreadManager`.

| Member | Description |
| --- | --- |
| `Job AddJob(Job job)` | Schedule a job; assigns it to a free worker immediately or queues it. |
| `Job AddJob(Action action, string name)` | Convenience: wrap an action in a named job and schedule it. |
| `int ThreadsCount { get; }` | Current worker count (driven by the **Threads** core setting). |
| `int FailedThreadsCount { get; }` | Workers repaired after exceeding the 750 ms critical budget. |
| `void ChangeNumberThreads(int count)` | Resize the pool. |

### Coroutines (`IYieldBase` / `WaitTime` / `WaitFunction`)

For multi-step work that spans many frames (walk to a stash, click each item, wait for a panel), use a coroutine instead of a single `Job`. A coroutine is an `IEnumerator` whose `yield return` values are **yield conditions** that suspend it until satisfied. Register one with `Core.ParallelRunner.Run(...)` and the engine resumes it each frame.

`ExileCore.Shared.Interfaces.IYieldBase` is the marker interface (`IEnumerable, IEnumerator`) for those conditions. The abstract `ExileCore.Shared.YieldBase` implements it, and the concrete conditions are:

| Yield condition | Resumes when |
| --- | --- |
| `WaitTime(int milliseconds)` | the given number of milliseconds has elapsed. |
| `WaitRandom(int minWait, int maxWait)` | a random delay in `[minWait, maxWait)` ms has elapsed. |
| `WaitRender(long count = 1)` | `count` frames have rendered. |
| `WaitFunction(Func<bool> fn)` | `fn()` returns `false` (it waits *while* the predicate is true). |

```csharp
// A coroutine method: yield conditions pause it between steps.
private IEnumerator DoStashRoutine()
{
    OpenStash();
    yield return new WaitFunction(() => !StashIsOpen());   // wait until the panel opens
    foreach (var item in ItemsToStash())
    {
        ClickItem(item);
        yield return new WaitTime(60);                      // human-ish delay between clicks
    }
}

// Start it (e.g. from Tick when a hotkey fires, as Stashie does):
Core.ParallelRunner.Run(DoStashRoutine(), this, "MyPlugin_Stash");
```

`Run(IEnumerator, IPlugin owner, string name = null)` de-duplicates by `(name, owner)`, so calling it again while the same routine is live returns the existing one rather than starting a second. Stashie drives its whole stash sequence this way, checking `Core.ParallelRunner.FindByName("Stashie_DropItemsToStash")` in `Tick` to start/stop the coroutine.

---

## Time

`ExileCore.Time` exposes global timing off a single process-wide stopwatch:

| Member | Description |
| --- | --- |
| `static double DeltaTime { get; set; }` | Time elapsed since the previous frame. |
| `static double TotalMilliseconds { get; }` | Total ms since the app started (fractional). |
| `static long ElapsedMilliseconds { get; }` | Whole ms since the app started. |

Use `Time.DeltaTime` for frame-rate-independent animation/throttling and `Time.TotalMilliseconds` for cheap monotonic timestamps.

---

## Helper extensions (`ExileCore.Shared.Helpers`)

A grab-bag of static helpers. The genuinely useful ones for plugin authors:

### `Extensions` — colors, vectors, reflection

| Member | Description |
| --- | --- |
| `Color.ToImgui()` | Color → packed RGBA `uint` for ImGui. |
| `Color.ToImguiVec4()` / `.ToImguiVec4(byte alpha)` | Color → normalized `Vector4` for ImGui (the alpha overload uses raw bytes). |
| `Color.GetRandomColor()` | A random named SharpDX color. |
| `static Color GetColorByName(string)` | Named color lookup; `Color.Zero` if unknown. |
| `static MapIconsIndex IconIndexByName(string)` | Resolve a `MapIconsIndex` by name. |
| `Color.Hex()` | Cached hex RGBA string for a color. |
| `SharpDX.Vector2/4 → ToVector2Num()/ToVector4Num()` | SharpDX → `System.Numerics` vectors. |
| `Vector4.ToSharpColor()` | Numeric `Vector4` → SharpDX `Color`. |
| `Entity.ValidCache<T>(Func<T>)` | Build a `ValidCache<T>` tied to an entity's validity (see [caching.md](caching.md)). |

### `ConvertHelper` — formatting & color conversion

| Member | Description |
| --- | --- |
| `static string ToShorten(double value, string format = "0")` | Compact number formatting: `1.2K`, `3.45M`. |
| `string.ToBGRAColor()` | Parse a hex BGRA string into a `Color` (`Color.Black` on failure). |
| `Color.ToHex()` | Color → HTML hex string. |
| `static Color ColorFromHsv(double h, double s, double v)` | Build a color from HSV. |
| `SharpDX vector → TranslateToNum(...)` | Convert to `System.Numerics` with optional per-axis offsets. |

### `MiscHelpers` — strings, time, config

| Member | Description |
| --- | --- |
| `string.InsertBeforeUpperCase(string append)` | Split camel/Pascal case, e.g. for friendly labels. |
| `static string GetTimeString(TimeSpan)` | Format a span as `h:mm:ss` (or `m:ss` under an hour). |
| `string.ToEnum<T>()` | Case-insensitive enum parse. |
| `RectangleF.ClickRandom(int x = 3, int y = 3)` | A random point inside a rect, inset by margins (human-like clicks). |
| `static void PerfTimerLogMsg(Action act, string msg, …)` | Run an action under a perf timer and report to the overlay. |
| `static IEnumerable<string[]> LoadConfigBase(string path, int columns = 2)` | Load a `;`-delimited config file, skipping blanks and `#` comments. |

### `MathHepler` — geometry & RNG

(Note the spelling — `MathHepler`, in `ExileCore.Shared.Helpers`.)

| Member | Description |
| --- | --- |
| `static Random Randomizer { get; }` | Shared RNG used across the helpers. |
| `Vector2.Distance(Vector2)` / `.DistanceSquared(Vector2)` | Distance between points. |
| `Vector2.Translate(float dx, float dy)` / `Mult(...)` | Vector translate / scale. |
| `Vector2.PointInRectangle(RectangleF)` | Point-in-rect test. |
| `static Vector2 NormalizeVector(Vector2)` / `RotateVector2(...)` | Normalize / rotate. |
| `static string GetRandomWord(int length)` | Random identifier (used internally for coroutine names). |

### `IntPtrExtensions` — pointer arithmetic & validation

Useful when working directly with native addresses (offsets, raw memory): `Add`/`Subtract`/`Multiply`/`Divide`, `GetValue()` (→ `ulong`), `IsZero()`/`IsNotZero()`, `IsAligned()`, and `IsValid()` (Windows user-space address range check).

### `ActionExtensions` — exception-safe invocation

`action.SafeTryInvoke(...)` invokes a delegate (`Action`, `Action<T>`, `Action<T1,T2>`, `Action<T1,T2,T3>`) and, instead of letting it throw, logs any exception via `DebugWindow.LogError`. Handy for invoking user callbacks or event handlers that shouldn't be allowed to crash a frame.

### `PerformanceTimer` — scoped timing

`ExileCore.Shared.Helpers.PerformanceTimer` is a disposable struct (`new PerformanceTimer(label, triggerMs, callback, log)`) that logs the elapsed time on `Dispose`, but only if it exceeds `triggerMs`. Wrap a `using` block around code you want to profile:

```csharp
using (new PerformanceTimer("BuildItemList", triggerMs: 5))
{
    BuildItemList(); // reported only if it took >= 5 ms
}
```

It reports through its static `PerformanceTimer.Logger`; set `PerformanceTimer.IgnoreTimer = true` to mute all timers globally.

### `DictionaryExtensions`

This repo's `DictionaryExtensions` provides `MergeLeft<T, TK, TV>(this T me, params IDictionary<TK,TV>[] others)` — returns a new dictionary with `others` merged over the receiver (later dictionaries win on key collisions). (There is no `GetValueOrDefault` here; use the BCL `Dictionary.GetValueOrDefault` for that.)

---

## Examples

**Log an error to the overlay** (it also lands in `Logs\Error-{Date}.log`):

```csharp
public override Job Tick()
{
    try
    {
        DoRiskyThing();
    }
    catch (Exception e)
    {
        LogError(e.ToString(), 5);          // BaseSettingsPlugin helper → DebugWindow.LogError
    }
    return null;
}
```

**Play an alert sound** when a condition fires:

```csharp
private SoundController _sound;

public override bool Initialise()
{
    _sound = GameController.SoundController;
    _sound.PreloadSound("alert");           // avoid first-play hitch (optional)
    return true;
}

public override void Render()
{
    if (DangerNearby() && Settings.PlaySounds)
        _sound.PlaySound("alert");          // no-op if the sounds dir is missing
}
```

**Offload work via a Job from `Tick()`** (runs on a worker when the Threads setting > 0, else inline):

```csharp
private IReadOnlyList<MyResult> _results = [];

public override Job Tick()
{
    if (!NeedsRecompute()) return null;

    var snapshot = SnapshotEntities();      // capture on the main thread
    return new Job($"{Name}_Compute", () =>
    {
        _results = ExpensiveAnalysis(snapshot); // worker thread; no rendering here
    });
}

public override void Render()
{
    foreach (var r in _results)
        Graphics.DrawText(r.Label, r.Position, Color.White); // draw on the main thread
}
```

---

## Source

- `Core/DebugWindow.cs` — `DebugWindow`, `DebugMsgDescription`
- `Core/Logger.cs` — `Logger`
- `Core/SoundController.cs` — `SoundController`, `MyWave`
- `Core/MultiThreadManager.cs` — `Job`, `MultiThreadManager`, `ThreadUnit`
- `Core/Time.cs` — `Time`
- `Core/Shared/Coroutine.cs` — `Coroutine`, `YieldBase`, `WaitTime`, `WaitRandom`, `WaitRender`, `WaitFunction`
- `Core/Shared/Runner.cs` — `Runner` (`Core.ParallelRunner`)
- `Core/Shared/Interfaces/IYieldBase.cs` — `IYieldBase`
- `Core/Shared/Helpers/` — `Extensions`, `ConvertHelper`, `MiscHelpers`, `MathHepler`, `IntPtrExtensions`, `ActionExtensions`, `PerformanceTimer`, `DictionaryExtensions`
- `Core/BaseSettingsPlugin.cs`, `Core/Core.cs` — logging helpers and the `Tick` → `Job` pipeline
