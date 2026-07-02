// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System.Collections.Generic;

namespace ExileCore.TreeRoutine.TreeSharp;

/// <summary>
/// Runs its children in order and stops at the first one that reports <see cref="RunStatus.Failure"/>
/// ("AND" semantics). Succeeds only when every child succeeds; stays on a <see cref="RunStatus.Running"/>
/// child across ticks. Plugin's OWN type, copied from upstream TreeSharp.
/// </summary>
public class Sequence : GroupComposite
{
    public Sequence(params Composite[] children) : base(children)
    {
    }

    public override IEnumerable<RunStatus> Execute(object context)
    {
        foreach (var child in Children)
        {
            if (child == null)
                continue;

            child.Start(context);
            var status = child.Tick(context);
            while (status == RunStatus.Running)
            {
                yield return RunStatus.Running;
                status = child.Tick(context);
            }

            if (status == RunStatus.Failure)
            {
                LastStatus = RunStatus.Failure;
                yield return RunStatus.Failure;
                yield break;
            }
            // Success -> continue with the next child.
        }

        LastStatus = RunStatus.Success;
        yield return RunStatus.Success;
    }
}
