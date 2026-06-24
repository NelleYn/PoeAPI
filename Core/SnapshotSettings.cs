using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;

namespace ExileCore;
[Submenu(CollapsedByDefault = true)]
public class SnapshotSettings
{
    [Submenu(RenderMethod = "Render")]
    public class RenderList
    {
        public void Render()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    [JsonIgnore]
    public ButtonNode TakeSnapshot { get; set; } = new();

    [JsonIgnore]
    public ButtonNode ClearSnapshots { get; set; } = new();
    public HotkeyNodeV2 TakeSnapshotHotkey { get; set; }
    public ToggleNode FreezeProcessDuringSnapshot { get; set; } = new();

    [JsonIgnore]
    public RenderList SnapshotList { get; set; }

    public SnapshotSettings()
    {
        _ = 0;
        _ = 0;
    }
}