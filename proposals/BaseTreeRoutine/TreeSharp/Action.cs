// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System;
using System.Collections.Generic;

namespace ExileCore.TreeRoutine.TreeSharp;

/// <summary>
/// A leaf that does the actual work and reports a <see cref="RunStatus"/>. Either pass a delegate
/// (<c>new Action(ctx =&gt; { ...; return RunStatus.Success; })</c>) or subclass and override
/// <see cref="Run"/> (as <c>UseHotkeyAction</c> does). Plugin's OWN type, copied from upstream TreeSharp.
/// </summary>
public class Action : Composite
{
    private readonly Func<object, RunStatus> _run;

    /// <summary>For subclasses that override <see cref="Run"/>.</summary>
    public Action()
    {
    }

    /// <summary>Delegate-based leaf.</summary>
    public Action(Func<object, RunStatus> run)
    {
        _run = run;
    }

    /// <summary>Convenience leaf for side-effect-only work that always succeeds.</summary>
    public Action(System.Action<object> run)
    {
        _run = ctx =>
        {
            run?.Invoke(ctx);
            return RunStatus.Success;
        };
    }

    /// <summary>The work. Return <see cref="RunStatus.Running"/> to be re-invoked next tick.</summary>
    protected virtual RunStatus Run(object context) => _run != null ? _run(context) : RunStatus.Failure;

    public override IEnumerable<RunStatus> Execute(object context)
    {
        RunStatus status;
        do
        {
            status = Run(context);
            if (status == RunStatus.Running)
                yield return RunStatus.Running;
        }
        while (status == RunStatus.Running);

        LastStatus = status;
        yield return status;
    }
}
