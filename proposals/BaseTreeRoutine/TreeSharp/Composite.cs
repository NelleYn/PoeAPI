// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System.Collections.Generic;

namespace ExileCore.TreeRoutine.TreeSharp;

/// <summary>
/// Base class for every behaviour-tree node. This is the plugin's OWN type (copied/adapted from
/// the bundled TreeSharp library that <c>BasicFlaskRoutine</c> vendors) — it is NOT an engine type.
/// </summary>
/// <remarks>
/// Execution is coroutine based, matching upstream TreeSharp: a node describes its work as an
/// iterator (<see cref="Execute"/>) that yields <see cref="RunStatus.Running"/> while it still has
/// work to do and finally yields a terminal <see cref="RunStatus.Success"/>/<see cref="RunStatus.Failure"/>.
/// <see cref="Tick"/> advances that iterator by exactly one step per call, so a single tree can span
/// multiple engine frames. The driver in <c>BaseTreeRoutinePlugin.TickTree</c> re-<see cref="Start"/>s
/// the tree only when the previous tick did not leave it <see cref="RunStatus.Running"/>.
/// </remarks>
public abstract class Composite
{
    /// <summary>Optional user tag; unused by the engine, provided for parity with upstream TreeSharp.</summary>
    public object Tag { get; set; }

    /// <summary>The node that owns this one, or <c>null</c> for the root.</summary>
    public Composite Parent { get; set; }

    /// <summary>The status reported by the most recent <see cref="Tick"/>, or <c>null</c> before the first tick.</summary>
    public RunStatus? LastStatus { get; protected set; }

    /// <summary>The in-flight iterator produced by <see cref="Execute"/>, or <c>null</c> when idle.</summary>
    protected IEnumerator<RunStatus> Coroutine { get; set; }

    /// <summary>
    /// (Re)initialise the node for a fresh traversal. Discards any in-flight iterator so the next
    /// <see cref="Tick"/> re-evaluates from the top.
    /// </summary>
    public virtual void Start(object context)
    {
        Coroutine = Execute(context).GetEnumerator();
        LastStatus = null;
    }

    /// <summary>Hook invoked once when a traversal completes (terminal status reached). Override to clean up.</summary>
    public virtual void Stop(object context)
    {
    }

    /// <summary>The node's work as a coroutine. Yield <see cref="RunStatus.Running"/> to be resumed next tick.</summary>
    public abstract IEnumerable<RunStatus> Execute(object context);

    /// <summary>
    /// Advance the node by one coroutine step and return the resulting status. Lazily calls
    /// <see cref="Start"/> if the node has not been started yet; drops the iterator once a terminal
    /// status is reached so the following traversal starts fresh.
    /// </summary>
    public virtual RunStatus Tick(object context)
    {
        if (Coroutine == null)
            Start(context);

        if (Coroutine.MoveNext())
        {
            LastStatus = Coroutine.Current;
        }
        else
        {
            // A well-formed Execute() always ends by yielding a terminal status (handled below),
            // so this branch only guards a malformed/empty iterator: fall back to the last known
            // status, or Failure if there never was one.
            LastStatus ??= RunStatus.Failure;
            Stop(context);
            Coroutine = null;
            return LastStatus.Value;
        }

        if (LastStatus != RunStatus.Running)
        {
            Stop(context);
            Coroutine = null;
        }

        return LastStatus.Value;
    }
}
