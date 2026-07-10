using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using NumVector2 = System.Numerics.Vector2;
using NumVector3 = System.Numerics.Vector3;

namespace ExileCore.Shared;

/// <summary>
/// <c>System.Numerics</c> accessors that emulate the ExileApi-Compiled members
/// <c>PosNum</c> / <c>GridPosNum</c> / <c>WorldPosNum</c> / <c>BoundsNum</c> / <c>RotationNum</c>.
/// <para>
/// This fork stores positions and bounds as <see cref="SharpDX.Vector2"/> / <see cref="SharpDX.Vector3"/>
/// (see the compatibility doc, "Entity &amp; EntityListWrapper" and "Components — combat &amp; character").
/// These extension methods convert at the call boundary using the fork's existing
/// <c>ToVector2Num</c> / <c>ToVector3Num</c> converters
/// (<c>Core/Shared/Helpers/Extensions.cs:140,150</c>). Upstream <c>Entity.PosNum</c> and
/// <c>Entity.GridPosNum</c> are not emulated here because Core already ships them as instance
/// properties (<c>Core/PoEMemory/MemoryObjects/Entity.cs:140,179</c>).
/// </para>
/// </summary>
public static class NumericsCompat
{
    // ---- Entity --------------------------------------------------------------

    /// <summary>
    /// Emulates upstream <c>Entity.BoundsNum</c>: the entity render bounds as <see cref="System.Numerics.Vector3"/>.
    /// </summary>
    /// <param name="e">The entity.</param>
    /// <returns>
    /// The render bounds, or <see cref="System.Numerics.Vector3.Zero"/> when the entity has no
    /// <see cref="Render"/> component. Builds on <c>Render.Bounds</c> (<c>Render.cs:49</c>).
    /// Upstream exposes this on <c>Entity</c> via the <c>Render</c> component; the fork has no
    /// <c>Bounds</c> on <c>Entity</c> (compatibility doc, "Components — combat &amp; character").
    /// </returns>
    public static NumVector3 BoundsNum(this Entity e)
    {
        var render = e.GetComponent<Render>();
        return render == null ? NumVector3.Zero : render.BoundsNum();
    }

    // ---- Positioned ----------------------------------------------------------

    /// <summary>
    /// Emulates upstream <c>Positioned.WorldPosNum</c>: the world position as <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    /// <param name="p">The positioned component.</param>
    /// <returns>The world position. Builds on <c>Positioned.WorldPos</c> (<c>Positioned.cs:40</c>).</returns>
    public static NumVector2 WorldPosNum(this Positioned p)
    {
        return p.WorldPos.ToVector2Num();
    }

    /// <summary>
    /// Emulates upstream <c>Positioned.GridPosNum</c>: the grid position as <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    /// <param name="p">The positioned component.</param>
    /// <returns>The grid position. Builds on <c>Positioned.GridPos</c> (<c>Positioned.cs:34</c>).</returns>
    public static NumVector2 GridPosNum(this Positioned p)
    {
        return p.GridPos.ToVector2Num();
    }

    // ---- Render --------------------------------------------------------------

    /// <summary>
    /// Emulates upstream <c>Render.PosNum</c>: the render position as <see cref="System.Numerics.Vector3"/>.
    /// </summary>
    /// <param name="r">The render component.</param>
    /// <returns>The render position. Builds on <c>Render.Pos</c> (<c>Render.cs:34</c>).</returns>
    public static NumVector3 PosNum(this Render r)
    {
        return r.Pos.ToVector3Num();
    }

    /// <summary>
    /// Emulates upstream <c>Render.BoundsNum</c>: the render bounds as <see cref="System.Numerics.Vector3"/>.
    /// </summary>
    /// <param name="r">The render component.</param>
    /// <returns>The render bounds. Builds on <c>Render.Bounds</c> (<c>Render.cs:49</c>).</returns>
    public static NumVector3 BoundsNum(this Render r)
    {
        return r.Bounds.ToVector3Num();
    }

    /// <summary>
    /// Emulates upstream <c>Render.RotationNum</c>: the model rotation as <see cref="System.Numerics.Vector3"/>.
    /// </summary>
    /// <param name="r">The render component.</param>
    /// <returns>The model rotation. Builds on <c>Render.Rotation</c> (<c>Render.cs:46</c>).</returns>
    public static NumVector3 RotationNum(this Render r)
    {
        return r.Rotation.ToVector3Num();
    }

    // ---- Element ---------------------------------------------------------------

    /// <summary>
    /// Emulates upstream <c>Element.PositionNum</c>: the UI element's position as <see cref="System.Numerics.Vector2"/>.
    /// </summary>
    /// <param name="e">The UI element.</param>
    /// <returns>
    /// The element position. Builds on the fork's <c>Element.Position</c>
    /// (<c>Core/PoEMemory/Element.cs:61</c>), which is a plain SharpDX <see cref="SharpDX.Vector2"/>
    /// (compatibility doc, "UI Elements").
    /// </returns>
    public static NumVector2 PositionNum(this Element e)
    {
        return e.Position.ToVector2Num();
    }
}
