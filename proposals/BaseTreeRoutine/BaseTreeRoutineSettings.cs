// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace ExileCore.TreeRoutine;

/// <summary>
/// Settings contract required by <see cref="BaseTreeRoutinePlugin{TSettings}"/>: the engine-required
/// <see cref="ISettings.Enable"/> master switch plus a tick-rate slider that throttles the tree
/// coroutine. Concrete plugins add their own routine toggles/thresholds on top. Mirrors upstream
/// <c>BaseTreeSettings</c> (which also carried <c>Enable</c> + <c>TicksPerSecond</c>).
/// </summary>
public interface ITreeSettings : ISettings
{
    /// <summary>How many times per second the tree is ticked. The coroutine waits <c>1000 / value</c> ms.</summary>
    RangeNode<int> TicksPerSecond { get; set; }
}

/// <summary>
/// Ready-to-extend implementation of <see cref="ITreeSettings"/>. Derive your plugin's settings from
/// this to inherit the <see cref="Enable"/> switch and <see cref="TicksPerSecond"/> slider, or implement
/// <see cref="ITreeSettings"/> directly. Uses this fork's <c>ToggleNode</c>/<c>RangeNode&lt;T&gt;</c> nodes.
/// </summary>
public class BaseTreeRoutineSettings : ITreeSettings
{
    /// <summary>Master enable switch required by <see cref="ISettings"/>. Off by default so automation never auto-starts.</summary>
    public ToggleNode Enable { get; set; } = new ToggleNode(false);

    [Menu("Ticks per second")]
    public RangeNode<int> TicksPerSecond { get; set; } = new RangeNode<int>(10, 1, 20);
}
