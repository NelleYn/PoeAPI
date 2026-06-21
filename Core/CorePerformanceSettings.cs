using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using Newtonsoft.Json;

namespace ExileCore;
[Submenu]
public class CorePerformanceSettings
{
    public ToggleNode CoroutineMultiThreading { get; set; } = new();
    public ToggleNode ParseEntitiesInMultiThread { get; set; } = new();

    [Menu("Threads count", "How much threads to use for prepare work.")]
    public RangeNode<int> Threads { get; set; } = new();

    [Menu("Target FPS")]
    public RangeNode<int> TargetFps { get; set; } = new();
    public RangeNode<int> TargetParallelCoroutineFps { get; set; } = new();

    [Menu(null, "How often to update entities. You can see time spent on this in DebugWindow->Coroutines.")]
    public RangeNode<int> EntitiesFps { get; set; } = new();

    [JsonProperty("ParseServerEntities_v2")]
    public ToggleNode ParseServerEntities { get; set; } = new();

    [Menu("Limit draw plot in ms", "Don't put small value, because plot need a lot triangles and DebugWindow with a lot plot will be broke.")]
    public RangeNode<float> LimitDrawPlot { get; set; } = new();
    public RangeNode<int> MaxGroundItemLabels { get; set; } = new();
    public RangeNode<int> MaxEntities { get; set; } = new();

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