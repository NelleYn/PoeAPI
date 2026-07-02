// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

using System;
using System.Collections.Generic;

namespace ExileCore.TreeRoutine.TreeSharp;

/// <summary>
/// Base class for composites that own an ordered list of children (<see cref="PrioritySelector"/>,
/// <see cref="Sequence"/>). Plugin's OWN type, copied from upstream TreeSharp. Kept as a shared base
/// so the selector/sequence traversal logic is not duplicated.
/// </summary>
public abstract class GroupComposite : Composite
{
    /// <summary>The child nodes, in evaluation order.</summary>
    protected List<Composite> Children { get; }

    /// <param name="children">Children in priority / sequence order.</param>
    protected GroupComposite(params Composite[] children)
    {
        Children = new List<Composite>(children ?? Array.Empty<Composite>());
        foreach (var child in Children)
            if (child != null)
                child.Parent = this;
    }

    /// <summary>Append a child after construction (parity with upstream TreeSharp's mutable groups).</summary>
    public void AddChild(Composite child)
    {
        if (child == null)
            return;
        child.Parent = this;
        Children.Add(child);
    }
}
