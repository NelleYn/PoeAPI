// EXPERIMENTAL candidate — InputHumanizer PluginBridge consumer. See proposals/InputHumanizer/README.md. Not part of the build.

using System;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;

namespace ExileCore.InputHumanizing;

/// <summary>
/// A small, robust helper for consuming the community <c>InputHumanizer</c> plugin through the
/// fork's <see cref="PluginBridge"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why so loosely typed?</b> The InputHumanizer service hands back an
/// <c>InputHumanizer.Input.IInputController</c> whose interface lives inside the InputHumanizer plugin
/// assembly, and its mouse/keyboard methods return <c>ExileCore.Shared.SyncTask&lt;T&gt;</c> — a
/// frame-pumped coroutine. <b>Neither <c>IInputController</c> nor <c>SyncTask</c> nor the
/// <c>TaskUtils</c> pump exists in this fork</b>, so a consumer here cannot statically reference the
/// contract and cannot drive the async (humanized-delay) methods to completion. This helper therefore
/// (1) acquires the controller dynamically through the bridge, (2) forwards the one genuinely
/// synchronous controller method (<see cref="IHumanizedInput.KeyDown"/>), (3) exposes the raw controller
/// for advanced consumers that <i>do</i> have a SyncTask pump, and (4) provides a fully synchronous
/// <see cref="Input"/> fallback for the common case.
/// </para>
/// <para>
/// <b>Important — humanized async actions are not blocked synchronously.</b> The humanized
/// <c>Click</c>/<c>KeyUp</c>/<c>MoveMouse</c> methods <c>await Task.Delay(...)</c> internally; they
/// complete over several frames. Synchronously blocking on them from the render thread would either
/// deadlock the frame pump or return a premature/incorrect result, so this helper does <b>not</b> do
/// that. Use <see cref="IHumanizedInput.RawController"/> from inside your own coroutine to await them, or
/// rely on the synchronous (non-humanized) convenience methods, which are always safe.
/// </para>
/// <para>
/// <b>EXPERIMENTAL.</b> This file is not part of the build and has not been compiled in this environment
/// (Windows + a running game are required). See the README for the confirmed contract and the flagged
/// assumption about the bridge key.
/// </para>
/// </remarks>
public sealed class InputHumanizerBridge
{
    /// <summary>
    /// The bridge key under which InputHumanizer is assumed to register its
    /// <c>SyncTask&lt;IInputController&gt; GetInputController(string, TimeSpan)</c> method.
    /// </summary>
    /// <remarks>
    /// The cloned InputHumanizer HEAD exposes <c>GetInputController</c> as a public plugin method but does
    /// not itself contain the <c>SaveMethod(...)</c> call, so this key is the documented community
    /// convention rather than something confirmed line-for-line in the clone. Verify it against the
    /// InputHumanizer build you actually run. See the README.
    /// </remarks>
    public const string GetInputControllerKey = "InputHumanizer.GetInputController";

    private readonly GameController _gameController;
    private readonly string _requestingPlugin;

    /// <summary>
    /// Creates a bridge bound to a <see cref="GameController"/> and an identifying plugin name. The plugin
    /// name is forwarded to InputHumanizer's input-lock manager so its UI can report who holds the lock.
    /// </summary>
    /// <param name="gameController">The active game controller (exposes <see cref="GameController.PluginBridge"/>).</param>
    /// <param name="requestingPlugin">A stable identifier for the calling plugin (e.g. its display name).</param>
    public InputHumanizerBridge(GameController gameController, string requestingPlugin)
    {
        _gameController = gameController ?? throw new ArgumentNullException(nameof(gameController));
        _requestingPlugin = string.IsNullOrWhiteSpace(requestingPlugin)
            ? throw new ArgumentException("A non-empty requesting plugin name is required.", nameof(requestingPlugin))
            : requestingPlugin;
    }

    /// <summary>
    /// Returns <c>true</c> when the InputHumanizer service is currently registered on the bridge.
    /// </summary>
    public bool IsServiceAvailable => ResolveGetControllerDelegate() != null;

    /// <summary>
    /// Acquires an input session, exclusively held until disposed. Wrap the result in a <c>using</c> block
    /// so the lock is released promptly.
    /// </summary>
    /// <param name="waitTime">
    /// How long the underlying service waits for the input lock before giving up. Only meaningful when the
    /// service is present; ignored by the fallback (which is always immediately available).
    /// </param>
    /// <returns>
    /// A humanized session when the service granted the lock, a synchronous <see cref="Input"/> fallback
    /// session when the service is absent, or <c>null</c> when the service is present but the lock could
    /// not be obtained (or the request otherwise failed).
    /// </returns>
    public IHumanizedInput TryAcquire(TimeSpan waitTime)
    {
        var getController = ResolveGetControllerDelegate();
        if (getController == null)
        {
            // Service not loaded: hand back the immediate, non-humanized fallback.
            return new RawInputSession();
        }

        object syncTask;
        try
        {
            syncTask = getController.DynamicInvoke(_requestingPlugin, waitTime);
        }
        catch (Exception ex)
        {
            // DynamicInvoke wraps callee exceptions in TargetInvocationException; this catch covers both
            // that and any binding failure (wrong delegate shape). Degrade to the fallback.
            DebugWindow.LogError($"InputHumanizerBridge: failed to invoke '{GetInputControllerKey}': {ex.Message}");
            return new RawInputSession();
        }

        // GetInputController returns SyncTask<IInputController>. Acquiring the lock either completes
        // synchronously (lock free) or, when contended, needs frame pumping we cannot do here. We make a
        // single best-effort completion attempt; if the controller is not available, return null so the
        // caller can retry next tick rather than silently using un-humanized input.
        if (!TryCompleteSyncTask(syncTask, out var controller) || controller == null)
        {
            return null;
        }

        return new HumanizedInputSession(controller);
    }

    /// <summary>
    /// Non-blocking acquire: asks the service for the lock with a zero wait, which matches InputHumanizer's
    /// own "try" semantics (<c>SemaphoreSlim.Wait(TimeSpan.Zero)</c>).
    /// </summary>
    /// <param name="session">The acquired session, or <c>null</c> when the lock was busy.</param>
    /// <returns><c>true</c> if a session (humanized or fallback) was produced; otherwise <c>false</c>.</returns>
    public bool TryAcquireImmediately(out IHumanizedInput session)
    {
        session = TryAcquire(TimeSpan.Zero);
        return session != null;
    }

    /// <summary>
    /// Resolves the registered <c>GetInputController</c> delegate from the bridge, or <c>null</c> if absent.
    /// </summary>
    private Delegate ResolveGetControllerDelegate()
    {
        var bridge = _gameController.PluginBridge;
        if (bridge == null)
        {
            return null;
        }

        // The exact generic argument (Func<string, TimeSpan, SyncTask<IInputController>>) is not
        // referenceable here, so fetch as a bare Delegate and invoke dynamically.
        return bridge.GetMethod<Delegate>(GetInputControllerKey);
    }

    /// <summary>
    /// Best-effort, single-step completion of a <c>SyncTask&lt;IInputController&gt;</c> via its awaiter,
    /// used only for the lock acquisition (which usually completes synchronously when the lock is free).
    /// </summary>
    /// <param name="syncTask">The awaitable returned by <c>GetInputController</c>.</param>
    /// <param name="result">The unwrapped controller, or <c>null</c> if not yet available.</param>
    /// <returns>
    /// <c>true</c> if the task completed and produced a result; <c>false</c> if it was incomplete or threw.
    /// </returns>
    private static bool TryCompleteSyncTask(object syncTask, out object result)
    {
        result = null;
        if (syncTask == null)
        {
            return false;
        }

        var type = syncTask.GetType();

        try
        {
            // Prefer the awaiter pattern (SyncTask exposes IsCompleted + GetResult on its awaiter).
            var getAwaiter = type.GetMethod("GetAwaiter", Type.EmptyTypes);
            if (getAwaiter != null)
            {
                var awaiter = getAwaiter.Invoke(syncTask, null);
                if (awaiter != null)
                {
                    var awaiterType = awaiter.GetType();

                    // If the awaiter advertises IsCompleted == false, the lock was contended and we cannot
                    // pump it here — report "not ready" instead of forcing a premature/blocking GetResult.
                    var isCompleted = awaiterType.GetProperty("IsCompleted");
                    if (isCompleted != null && isCompleted.GetValue(awaiter) is bool completed && !completed)
                    {
                        return false;
                    }

                    var getResult = awaiterType.GetMethod("GetResult", Type.EmptyTypes);
                    if (getResult != null)
                    {
                        // GetResult is invoked on the awaiter, which is non-null here.
                        result = getResult.Invoke(awaiter, null);
                        return result != null;
                    }
                }
            }

            // Fallback: a Result property (Task-like). Only read it if the task reports completion.
            var resultProp = type.GetProperty("Result");
            if (resultProp != null)
            {
                var isCompletedProp = type.GetProperty("IsCompleted");
                if (isCompletedProp != null && isCompletedProp.GetValue(syncTask) is bool done && !done)
                {
                    return false;
                }

                result = resultProp.GetValue(syncTask);
                return result != null;
            }
        }
        catch (Exception ex)
        {
            // Reflection or awaited-callee failure: treat as "not acquired".
            DebugWindow.LogError($"InputHumanizerBridge: error completing controller task: {ex.Message}");
            return false;
        }

        return false;
    }

    /// <summary>
    /// A humanized session backed by an InputHumanizer <c>IInputController</c> reached through reflection.
    /// The synchronous <see cref="KeyDown"/> is forwarded directly; the humanized async actions are exposed
    /// only via <see cref="RawController"/> (callers drive them with their own SyncTask pump). Disposing
    /// releases the underlying input lock.
    /// </summary>
    private sealed class HumanizedInputSession : IHumanizedInput
    {
        private readonly object _controller;
        private readonly Type _type;
        private bool _disposed;

        internal HumanizedInputSession(object controller)
        {
            _controller = controller;
            _type = controller.GetType();
        }

        public bool IsHumanized => true;

        public object RawController => _controller;

        public bool KeyDown(Keys key)
        {
            // IInputController.KeyDown returns a plain bool (synchronous); no SyncTask to pump.
            var m = _type.GetMethod("KeyDown", new[] { typeof(Keys) });
            if (m == null)
            {
                DebugWindow.LogError("InputHumanizerBridge: controller has no KeyDown(Keys) method.");
                return false;
            }

            return m.Invoke(_controller, new object[] { key }) is true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            (_controller as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// The non-humanized fallback session used when InputHumanizer is not loaded. It forwards to the
    /// fork's synchronous <see cref="Input"/> API. There is no lock to release, so <see cref="Dispose"/>
    /// is a no-op, and <see cref="RawController"/> is <c>null</c>.
    /// </summary>
    private sealed class RawInputSession : IHumanizedInput
    {
        public bool IsHumanized => false;

        public object RawController => null;

        public bool KeyDown(Keys key)
        {
            Input.KeyDown(key); // Core/Input.cs:198
            return true;
        }

        public void Dispose()
        {
            // No lock held by the fallback.
        }
    }

    /// <summary>
    /// Immediately presses and releases a key using the synchronous <see cref="Input"/> API, bypassing the
    /// humanized path entirely. Convenience for the common "tap a key" case that works whether or not the
    /// service is loaded.
    /// </summary>
    /// <param name="key">The key to tap.</param>
    public static void TapKey(Keys key)
    {
        Input.KeyDown(key); // Core/Input.cs:198
        Input.KeyUp(key);   // Core/Input.cs:204
    }

    /// <summary>
    /// Immediately left-clicks at <paramref name="coordinate"/> using the synchronous <see cref="Input"/>
    /// API, bypassing the humanized path. Convenience that works whether or not the service is loaded.
    /// </summary>
    /// <param name="coordinate">The screen position to move to before clicking.</param>
    public static void ClickAt(Vector2 coordinate)
    {
        // Input.SetCursorPos takes a SharpDX.Vector2; see the README "Type note" for the conversion to add
        // when integrating into a real project.
        Input.SetCursorPos(coordinate.ToSharpDx()); // Core/Input.cs:149
        Input.Click(MouseButtons.Left);             // Core/Input.cs:156
    }
}

/// <summary>
/// Minimal conversion helpers between <see cref="System.Numerics.Vector2"/> (used by InputHumanizer and
/// by this helper's public surface) and the <c>SharpDX.Vector2</c> the fork's <see cref="Input"/> API
/// expects. Kept local so the proposal has no external dependency beyond the fork.
/// </summary>
internal static class VectorInterop
{
    /// <summary>Converts a numeric vector to the SharpDX vector the fork's input API consumes.</summary>
    public static SharpDX.Vector2 ToSharpDx(this Vector2 v) => new(v.X, v.Y);
}

/// <summary>
/// The fork-friendly surface of an acquired input session. Always supports the synchronous
/// <see cref="KeyDown"/>; the humanized async actions are reached through <see cref="RawController"/> so
/// that advanced consumers with a SyncTask pump can await them. Dispose to release the lock.
/// </summary>
public interface IHumanizedInput : IDisposable
{
    /// <summary>
    /// <c>true</c> when backed by the real InputHumanizer service (humanized timing/movement);
    /// <c>false</c> when using the immediate <see cref="Input"/> fallback.
    /// </summary>
    bool IsHumanized { get; }

    /// <summary>
    /// The raw <c>InputHumanizer.Input.IInputController</c> when <see cref="IsHumanized"/> is <c>true</c>,
    /// otherwise <c>null</c>. Cast it (or reflect over it) to await its <c>SyncTask&lt;bool&gt;</c>
    /// <c>Click</c>/<c>KeyUp</c>/<c>MoveMouse</c> methods from inside your own coroutine. This helper does
    /// not (and cannot, in this fork) drive those async methods to completion synchronously.
    /// </summary>
    object RawController { get; }

    /// <summary>
    /// Presses a key down. On the humanized path this is a synchronous call that also schedules a matching
    /// release delay inside the controller; on the fallback path it is an immediate
    /// <see cref="Input.KeyDown(Keys)"/>.
    /// </summary>
    /// <returns><c>true</c> on success; <c>false</c> if the humanized controller could not be invoked.</returns>
    bool KeyDown(Keys key);
}
