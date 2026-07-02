// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System;
using ExileCore.Shared;
using ExileCore.TreeRoutine.TreeSharp;

namespace ExileCore.TreeRoutine;

/// <summary>
/// Base plugin for behaviour-tree automation routines, ported from upstream
/// <c>TreeRoutine/BaseTreeRoutinePlugin.cs</c> and rewritten against this fork's engine API.
/// Builds the tree once in <see cref="Initialise"/> and ticks it on a fixed-rate engine
/// <see cref="Coroutine"/> registered with <c>Core.ParallelRunner</c> — so the tree runs off the
/// render thread and its rate is throttled by a settings slider rather than the frame rate.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Composite"/>/<see cref="Decorator"/>/<see cref="PrioritySelector"/>/<see cref="Sequence"/>/
/// <see cref="ExileCore.TreeRoutine.TreeSharp.Action"/>/<see cref="RunStatus"/> are the plugin's OWN
/// bundled TreeSharp types (see <c>TreeSharp/</c>). <see cref="Coroutine"/>, <see cref="WaitTime"/> and
/// <c>Core.ParallelRunner</c> ARE engine types.
/// </para>
/// <para>
/// Subclasses implement <see cref="CreateTree"/>. The conventional shape (see the cookbook) is a root
/// <see cref="Decorator"/> whose guard is <c>TreeHelper.CanTick(GameController) &amp;&amp; ReadState()</c>
/// wrapping a <see cref="PrioritySelector"/> of per-routine <see cref="Decorator"/> → action branches.
/// </para>
/// </remarks>
/// <typeparam name="TSettings">The plugin's settings, providing <c>Enable</c> and <c>TicksPerSecond</c>.</typeparam>
public abstract class BaseTreeRoutinePlugin<TSettings> : BaseSettingsPlugin<TSettings>
    where TSettings : ITreeSettings, new()
{
    /// <summary>The root of the behaviour tree, built once by <see cref="CreateTree"/>.</summary>
    protected Composite Tree { get; set; }

    private Coroutine _treeCoroutine;
    private int _lastTicksPerSecond;

    /// <summary>Build the behaviour tree. Called once from <see cref="Initialise"/>.</summary>
    protected abstract Composite CreateTree();

    /// <summary>Effective tick rate (clamped to at least 1). Reads the settings slider by default.</summary>
    protected virtual int TicksPerSecond => Math.Max(1, Settings.TicksPerSecond.Value);

    /// <summary>Name given to the tree coroutine; must be unique per plugin instance.</summary>
    protected virtual string TreeCoroutineName => $"{Name}##Tree";

    public override bool Initialise()
    {
        Tree = CreateTree();
        _lastTicksPerSecond = TicksPerSecond;
        _treeCoroutine = new Coroutine(() => TickTree(Tree), new WaitTime(1000 / _lastTicksPerSecond), this, TreeCoroutineName);
        Core.ParallelRunner.Run(_treeCoroutine);
        return true;
    }

    /// <summary>
    /// One tick of the tree. Bails when disabled or unbuilt; otherwise re-<see cref="Composite.Start"/>s
    /// the root unless the previous tick left it <see cref="RunStatus.Running"/> (a multi-tick action in
    /// flight), then advances it one step with <see cref="Composite.Tick"/>. Broad game-state gating
    /// (in game / focused / not in town / player alive) belongs in the tree's root
    /// <see cref="Decorator"/> via <c>TreeHelper.CanTick</c>, not here.
    /// </summary>
    protected virtual void TickTree(Composite tree)
    {
        if (!Settings.Enable.Value || tree == null)
            return;

        // Cheaply keep the coroutine's wait in sync with the slider if the user changed it.
        if (_treeCoroutine != null && TicksPerSecond != _lastTicksPerSecond)
        {
            _lastTicksPerSecond = TicksPerSecond;
            _treeCoroutine.UpdateCondtion(new WaitTime(1000 / _lastTicksPerSecond));
        }

        if (tree.LastStatus != RunStatus.Running)
            tree.Start(null);

        tree.Tick(null);
    }

    public override void Dispose()
    {
        _treeCoroutine?.Done();
        _treeCoroutine = null;
        base.Dispose();
    }
}
