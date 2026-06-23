// Starter plugin template for ExileCore — see proposals/PluginTemplate/README.md.

using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace MyPlugin;

/// <summary>
/// Strongly-typed settings for <see cref="MyPlugin"/>.
/// <para>
/// Every public property whose type is one of the node wrappers in
/// <c>ExileCore.Shared.Nodes</c> is reflected by <c>SettingsParser</c> into an ImGui
/// menu widget and persisted to JSON by <c>SettingsContainer</c>. You declare the
/// properties with sensible defaults; you do not build the drawers yourself.
/// </para>
/// </summary>
public class MyPluginSettings : ISettings
{
    /// <summary>
    /// Mandatory toggle required by <see cref="ISettings"/>. The core uses it to switch the
    /// owning plugin on and off. Defaulting to <c>true</c> means the plugin is active as soon
    /// as it is installed; ship it as <c>new(false)</c> if you prefer opt-in.
    /// </summary>
    public ToggleNode Enable { get; set; } = new(true);

    /// <summary>How far (in world units) to look for monsters before drawing their markers.</summary>
    [Menu("Draw distance", "Maximum distance from the player to highlight monsters.")]
    public RangeNode<int> DrawDistance { get; set; } = new(value: 100, min: 0, max: 1000);

    /// <summary>Color used to draw the monster markers and labels.</summary>
    [Menu("Marker color")]
    public ColorNode MarkerColor { get; set; } = new(Color.Red);

    /// <summary>Toggles the on-screen text rendered by <see cref="MyPlugin.Render"/>.</summary>
    [Menu("Show debug text")]
    public ToggleNode ShowDebugText { get; set; } = new(true);

    /// <summary>
    /// Hotkey checked every frame in <see cref="MyPlugin.Render"/>. <see cref="HotkeyNode"/>
    /// wraps a <see cref="Keys"/> value and also offers <c>PressedOnce()</c> / <c>GetKeyState</c>
    /// helpers for edge vs. level detection.
    /// </summary>
    [Menu("Highlight hotkey", "Hold this key to draw the monster markers.")]
    public HotkeyNode HighlightKey { get; set; } = new(Keys.F5);

    /// <summary>Free-text label drawn alongside the debug text. Demonstrates <see cref="TextNode"/>.</summary>
    [Menu("Label text")]
    public TextNode LabelText { get; set; } = new("MyPlugin");
}
