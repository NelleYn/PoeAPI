// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System;
using System.Collections.Generic;

namespace ExileCore.TreeRoutine.TreeSharp;

/// <summary>
/// Runs its single child only when a guard predicate is satisfied; otherwise reports
/// <see cref="RunStatus.Failure"/> without running the child. This is the workhorse "condition" node
/// (<c>new Decorator(ctx =&gt; hpPct &lt; threshold, action)</c>). Plugin's OWN type, copied from upstream
/// TreeSharp. Concrete here (predicate delegate) rather than abstract, matching how
/// <c>BasicFlaskRoutine.CreateTree()</c> constructs decorators inline.
/// </summary>
public class Decorator : Composite
{
    private readonly Func<object, bool> _canRun;

    /// <summary>The decorated child. Exposed to subclasses that want a custom <see cref="CanRun"/>.</summary>
    protected Composite Child { get; }

    /// <param name="canRun">Guard evaluated each traversal; the child runs only when it returns <c>true</c>.</param>
    /// <param name="child">The node to run when the guard passes.</param>
    public Decorator(Func<object, bool> canRun, Composite child)
    {
        _canRun = canRun;
        Child = child;
        if (child != null)
            child.Parent = this;
    }

    /// <summary>Always-run decorator (no guard); useful as a plain wrapper or subclass base.</summary>
    public Decorator(Composite child) : this(null, child)
    {
    }

    /// <summary>Whether the child may run this traversal. Override for a hard-typed condition.</summary>
    protected virtual bool CanRun(object context) => _canRun == null || _canRun(context);

    public override IEnumerable<RunStatus> Execute(object context)
    {
        if (Child == null || !CanRun(context))
        {
            LastStatus = RunStatus.Failure;
            yield return RunStatus.Failure;
            yield break;
        }

        Child.Start(context);
        var status = Child.Tick(context);
        while (status == RunStatus.Running)
        {
            yield return RunStatus.Running;
            status = Child.Tick(context);
        }

        LastStatus = status;
        yield return status;
    }
}
