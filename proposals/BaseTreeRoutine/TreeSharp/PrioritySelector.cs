// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System.Collections.Generic;

namespace ExileCore.TreeRoutine.TreeSharp;

/// <summary>
/// Runs its children in order and stops at the first one that reports <see cref="RunStatus.Success"/>
/// ("OR" semantics / priority list). If a child is <see cref="RunStatus.Running"/>, the selector stays
/// on it across ticks; if every child fails, the selector fails. Plugin's OWN type, copied from upstream
/// TreeSharp.
/// </summary>
public class PrioritySelector : GroupComposite
{
    public PrioritySelector(params Composite[] children) : base(children)
    {
    }

    public override IEnumerable<RunStatus> Execute(object context)
    {
        foreach (var child in Children)
        {
            if (child == null)
                continue;

            // Fresh evaluation of this child for this traversal.
            child.Start(context);
            var status = child.Tick(context);
            while (status == RunStatus.Running)
            {
                yield return RunStatus.Running;
                status = child.Tick(context);
            }

            if (status == RunStatus.Success)
            {
                LastStatus = RunStatus.Success;
                yield return RunStatus.Success;
                yield break;
            }
            // Failure -> fall through to the next child.
        }

        LastStatus = RunStatus.Failure;
        yield return RunStatus.Failure;
    }
}
