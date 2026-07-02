// EXPERIMENTAL candidate ported from exApiTools/BasicFlaskRoutine — see proposals/BaseTreeRoutine/README.md. Not part of the build.

namespace ExileCore.TreeRoutine.TreeSharp;

/// <summary>
/// The result a <see cref="Composite"/> reports for a single tick. This is the plugin's OWN type
/// (copied from the bundled TreeSharp library), not an engine type.
/// </summary>
public enum RunStatus
{
    /// <summary>The node finished and its work succeeded.</summary>
    Success,

    /// <summary>The node finished and its work failed / its guard was not satisfied.</summary>
    Failure,

    /// <summary>The node is still working and wants to be ticked again next frame.</summary>
    Running,
}
