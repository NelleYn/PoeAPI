using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;

namespace ExileCore;
[Submenu(CollapsedByDefault = true)]
public class CoreDebugSettings
{
    public ToggleNode ShowDemoWindow { get; set; } = new();
    public ToggleNode HideAllDebugging { get; set; } = new();
    public ToggleNode DebugFileLoads { get; set; } = new();
    public ToggleNode DebugDatLoads { get; set; } = new();
    public ToggleNode BypassLoginRequirement { get; set; } = new();
    public ToggleNode AttachToFirstFreeProcess { get; set; } = new();
    public ToggleNode DetectOtherLoaderProcesses { get; set; } = new();

    public CoreDebugSettings()
    {
        _ = 0;
        _ = 0;
        _ = 0;
        _ = 0;
        _ = 0;
        _ = 0;
        _ = 1;
    }
}