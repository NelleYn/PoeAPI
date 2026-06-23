// Starter plugin template for ExileCore — see proposals/PluginTemplate/README.md.

using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;

namespace MyPlugin;

/// <summary>
/// A minimal, fully-commented example plugin. It derives from
/// <see cref="BaseSettingsPlugin{TSettings}"/>, which implements <c>IPlugin</c>, wires the
/// settings load/save to disk, builds the settings menu and injects the core APIs
/// (<see cref="BaseSettingsPlugin{TSettings}.GameController"/> and
/// <see cref="BaseSettingsPlugin{TSettings}.Graphics"/>). Override only the hooks you need.
/// </summary>
public class MyPlugin : BaseSettingsPlugin<MyPluginSettings>
{
    /// <summary>
    /// One-time initialization. Runs once after the plugin is loaded and before the first
    /// <see cref="Tick"/>/<see cref="Render"/>. Return <c>false</c> to abort loading.
    /// </summary>
    public override bool Initialise()
    {
        Name = "MyPlugin";
        return true;
    }

    /// <summary>
    /// Called whenever the player changes area (zone). A good place for per-zone setup,
    /// e.g. clearing caches you built for the previous area.
    /// </summary>
    /// <param name="area">The area the player just entered.</param>
    public override void AreaChange(AreaInstance area)
    {
        // Example: nothing to do per-zone in this template.
    }

    /// <summary>
    /// Per-frame logic hook, called before <see cref="Render"/>. Do non-drawing work here
    /// (calculations, caching). Returning <c>null</c> runs the work inline on the main thread;
    /// returning a <see cref="Job"/> schedules it on the engine's worker pool instead.
    /// </summary>
    public override Job Tick()
    {
        // Example: no background work in this template.
        return null;
    }

    /// <summary>
    /// Per-frame rendering hook. All drawing through <see cref="BaseSettingsPlugin{TSettings}.Graphics"/>
    /// belongs here. Reads the player and the valid monster list, projects each monster's world
    /// position to screen space and draws a frame plus a label.
    /// </summary>
    public override void Render()
    {
        var player = GameController.Player;
        if (player == null)
            return;

        // Optional debug readout, toggled from settings.
        if (Settings.ShowDebugText)
        {
            Graphics.DrawText(
                $"{Settings.LabelText.Value}: tracking monsters within {Settings.DrawDistance.Value}",
                new Vector2(100, 100),
                Settings.MarkerColor);
        }

        // Hold the configured hotkey to draw the markers (level-triggered, true while held).
        if (!Input.GetKeyState(Settings.HighlightKey))
            return;

        var camera = GameController.IngameState.Camera;

        // ValidEntitiesByType is pre-grouped per EntityType and is always populated for every
        // enum value, so indexing EntityType.Monster never throws.
        foreach (var monster in GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
        {
            if (monster == null || !monster.IsAlive)
                continue;

            if (monster.DistancePlayer > Settings.DrawDistance.Value)
                continue;

            // World -> screen. WorldToScreen returns Vector2.Zero on failure / off-screen,
            // so treat the origin as "not drawable".
            var screenPos = camera.WorldToScreen(monster.Pos);
            if (screenPos == Vector2.Zero)
                continue;

            var topLeft = new Vector2(screenPos.X - 15, screenPos.Y - 15);
            var bottomRight = new Vector2(screenPos.X + 15, screenPos.Y + 15);
            Graphics.DrawFrame(topLeft, bottomRight, Settings.MarkerColor, 2);

            Graphics.DrawText(monster.RenderName, screenPos, Settings.MarkerColor);
        }
    }

    /// <summary>
    /// Called once for each gameplay-relevant entity as it appears. Override when you need to
    /// process every entity exactly once instead of scanning the full list every frame.
    /// </summary>
    /// <param name="entity">The entity that was just added.</param>
    public override void EntityAdded(Entity entity)
    {
        // Example: no per-entity work in this template.
    }
}
