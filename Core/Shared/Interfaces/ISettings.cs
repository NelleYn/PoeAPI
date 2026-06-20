using ExileCore.Shared.Nodes;

namespace ExileCore.Shared.Interfaces;

/// <summary>
/// Base contract for plugin settings, exposing the toggle that enables or disables the plugin.
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Gets or sets the toggle that controls whether the owning plugin is enabled.
    /// </summary>
    ToggleNode Enable { get; set; }
}
