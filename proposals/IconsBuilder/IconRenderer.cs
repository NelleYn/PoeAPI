// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Small reusable drawing helper for the icons produced by <see cref="IconsBuilder"/>. The original
/// library kept the icon factory and the renderer in separate plugins (IconsBuilder built the icons,
/// MinimapIcons/HeistIcons drew them). This helper shows how the icons map onto this fork's
/// <see cref="Graphics"/> facade so a host plugin only needs the factory plus a few lines of
/// rendering. Both a world-space and a minimap-space drawing path are provided.
/// </summary>
public static class IconRenderer
{
    /// <summary>
    /// Draws an icon over its entity in the world, using <c>Camera.WorldToScreen</c> on the entity's
    /// <see cref="Render"/> position. Respects the icon's <see cref="BaseIcon.Show"/> predicate.
    /// </summary>
    /// <param name="graphics">The host plugin's drawing facade.</param>
    /// <param name="gameController">The host game controller (for the camera).</param>
    /// <param name="icon">The icon to draw.</param>
    public static void DrawInWorld(Graphics graphics, GameController gameController, BaseIcon icon)
    {
        if (icon?.Entity == null || !icon.Show()) return;

        var texture = icon.MainTexture;
        if (texture?.FileName == null) return;

        var render = icon.Entity.GetComponent<Render>();
        if (render == null) return;

        // Camera.WorldToScreen returns a SharpDX Vector2 (screen pixels) for a SharpDX Vector3 world pos.
        var screen = gameController.Game.IngameState.Camera.WorldToScreen(render.Pos);

        var size = texture.Size;
        var half = size / 2f;
        var rect = new RectangleF(screen.X - half, screen.Y - half, size, size);

        graphics.DrawImage(texture.FileName, rect, texture.UV, texture.Color);

        if (!string.IsNullOrEmpty(icon.Text))
            graphics.DrawText(icon.Text, new Vector2(screen.X, screen.Y + half), Color.White, FontAlign.Center);
    }

    /// <summary>
    /// Draws an icon on the minimap at a pre-computed screen position. The host is responsible for
    /// projecting the icon's <see cref="BaseIcon.GridPosition"/> into minimap pixels (the projection
    /// depends on the active map element); this method just blits the sprite and label. The computed
    /// rectangle is also written back to <see cref="BaseIcon.DrawRect"/> for hit-testing.
    /// </summary>
    /// <param name="graphics">The host plugin's drawing facade.</param>
    /// <param name="icon">The icon to draw.</param>
    /// <param name="minimapPosition">The icon centre, in screen pixels, on the minimap.</param>
    /// <param name="zForText">Vertical text offset, in pixels, below the icon centre.</param>
    public static void DrawOnMinimap(Graphics graphics, BaseIcon icon, Vector2 minimapPosition, float zForText = 0f)
    {
        if (icon?.Entity == null || !icon.Show()) return;

        var texture = icon.MainTexture;
        if (texture?.FileName == null) return;

        var size = texture.Size;
        var half = size / 2f;
        icon.DrawRect = new RectangleF(minimapPosition.X - half, minimapPosition.Y - half, size, size);

        graphics.DrawImage(texture.FileName, icon.DrawRect, texture.UV, texture.Color);

        if (!string.IsNullOrEmpty(icon.Text))
            graphics.DrawText(icon.Text, new Vector2(minimapPosition.X, minimapPosition.Y + zForText), Color.White, FontAlign.Center);
    }

    /// <summary>
    /// Alternative blit path for hosts that ship their own named-texture atlas (the approach used by
    /// the HeistIcons plugin) instead of the shared <c>Icons.png</c>/<c>sprites.png</c> sheets. The
    /// atlas texture is obtained once by the host via <c>BaseSettingsPlugin.GetAtlasTexture(name)</c>
    /// and drawn here with the <see cref="AtlasTexture"/> overload of
    /// <c>Graphics.DrawImage</c>, which already carries its own UV.
    /// </summary>
    /// <param name="graphics">The host plugin's drawing facade.</param>
    /// <param name="atlasTexture">A texture resolved from the host's atlas (may be null).</param>
    /// <param name="position">The icon centre, in screen pixels.</param>
    /// <param name="size">The icon size, in pixels.</param>
    /// <param name="color">The tint colour.</param>
    public static void DrawAtlasTexture(Graphics graphics, AtlasTexture atlasTexture, Vector2 position, float size, Color color)
    {
        if (atlasTexture == null) return;

        var half = size / 2f;
        var rect = new RectangleF(position.X - half, position.Y - half, size, size);
        graphics.DrawImage(atlasTexture, rect, color);
    }
}
