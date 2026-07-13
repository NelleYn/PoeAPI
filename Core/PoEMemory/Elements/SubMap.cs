using SharpDX;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// A map sub-element — the full-screen large map or the corner minimap. Plugins obtain it via
/// <c>Map.LargeMap.AsObject&lt;SubMap&gt;()</c> / <c>Map.SmallMiniMap.AsObject&lt;SubMap&gt;()</c>,
/// so a <see cref="SubMap"/> shares the underlying map element's address.
///
/// The offsets below are the same ones <see cref="Map"/> already reads on this fork's build — the
/// shift/center at +0x1C0/+0x1C4 and the zoom at +0x204 back <c>Map.LargeMapShiftX/Y</c> /
/// <c>Map.LargeMapZoom</c> (and the identical minimap fields) — so <see cref="MapCenter"/> and
/// <see cref="MapScale"/> stay consistent with what <c>Map</c> exposes for both maps. If a future
/// game build moves these, update them here and in <see cref="Map"/> together.
/// </summary>
public class SubMap : Element
{
    /// <summary>Screen-space shift/translation of the map (its on-screen center offset).</summary>
    public Vector2 MapCenter => new Vector2(M.Read<float>(Address + 0x1C0), M.Read<float>(Address + 0x1C4));

    /// <summary>Grid-to-pixel scale (zoom) used to project grid/world points onto the map.</summary>
    public float MapScale => M.Read<float>(Address + 0x204);

    /// <summary>Alias of <see cref="MapScale"/>, kept for parity with the upstream SubMap API.</summary>
    public float Zoom => M.Read<float>(Address + 0x204);
}
