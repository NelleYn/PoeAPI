using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;

namespace ExileCore;
[Submenu]
public class CorePerformanceSettings
{
    public ToggleNode CoroutineMultiThreading
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

    public ToggleNode ParseEntitiesInMultiThread
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

    [Menu("Threads count", "How much threads to use for prepare work.")]
    public RangeNode<int> Threads
    {
        [CompilerGenerated]
        get
        {
            return (RangeNode<int>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [Menu("Target FPS")]
    public RangeNode<int> TargetFps
    {
        [CompilerGenerated]
        get
        {
            return (RangeNode<int>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public RangeNode<int> TargetParallelCoroutineFps
    {
        [CompilerGenerated]
        get
        {
            return (RangeNode<int>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [Menu(null, "How often to update entities. You can see time spent on this in DebugWindow->Coroutines.")]
    public RangeNode<int> EntitiesFps
    {
        [CompilerGenerated]
        get
        {
            return (RangeNode<int>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    [JsonProperty("ParseServerEntities_v2")]
    public ToggleNode ParseServerEntities
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

    [Menu("Limit draw plot in ms", "Don't put small value, because plot need a lot triangles and DebugWindow with a lot plot will be broke.")]
    public RangeNode<float> LimitDrawPlot
    {
        [CompilerGenerated]
        get
        {
            return (RangeNode<float>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public RangeNode<int> MaxGroundItemLabels
    {
        [CompilerGenerated]
        get
        {
            return (RangeNode<int>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public RangeNode<int> MaxEntities
    {
        [CompilerGenerated]
        get
        {
            return (RangeNode<int>)(object)this;
        }

        [CompilerGenerated]
        set
        {
        }
    }

    public CorePerformanceSettings()
    {
        _ = 0;
        _ = 0;
        _ = 2;
        _ = 0;
        _ = 4;
        _ = 60;
        _ = 5;
        _ = 60;
        _ = 30;
        _ = 60;
        _ = 5;
        _ = 1;
        _ = 0;
        _ = 0;
    }
}