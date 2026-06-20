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
    public ButtonNode TakeSnapshot
    {
        [CompilerGenerated]
        get
        {
            return (ButtonNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [JsonIgnore]
    public ButtonNode ClearSnapshots
    {
        [CompilerGenerated]
        get
        {
            return (ButtonNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public HotkeyNodeV2 TakeSnapshotHotkey
    {
        [CompilerGenerated]
        get
        {
            return (HotkeyNodeV2)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public ToggleNode FreezeProcessDuringSnapshot
    {
        [CompilerGenerated]
        get
        {
            return (ToggleNode)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [JsonIgnore]
    public RenderList SnapshotList
    {
        [CompilerGenerated]
        get
        {
            return (RenderList)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public SnapshotSettings()
    {
        _ = 0;
        _ = 0;
    }
}