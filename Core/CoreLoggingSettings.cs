using System.Runtime.CompilerServices;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;

namespace ExileCore;
[Submenu(CollapsedByDefault = true)]
public class CoreLoggingSettings
{
    public ListNode MinimumLogLevel { get; set; } = new();
    public ColorNode TimestampColor { get; set; } = new();
    public ColorNode VerboseLogColor { get; set; } = new();
    public ColorNode DebugLogColor { get; set; } = new();
    public ColorNode InfoLogColor { get; set; } = new();
    public ColorNode WarningLogColor { get; set; } = new();
    public ColorNode ErrorLogColor { get; set; } = new();
    public RangeNode<float> DefaultVerboseLogDisplayTime { get; set; } = new();
    public RangeNode<float> DefaultDebugLogDisplayTime { get; set; } = new();
    public RangeNode<float> DefaultInfoLogDisplayTime { get; set; } = new();
    public RangeNode<float> DefaultWarningLogDisplayTime { get; set; } = new();
    public RangeNode<float> DefaultErrorLogDisplayTime { get; set; } = new();

    public CoreLoggingSettings()
    {
        while (this != null)
        {
        }
    }
}