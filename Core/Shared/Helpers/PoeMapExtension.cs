using System;
using SharpDX;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Extension methods for converting between Path of Exile grid and world coordinates.
/// </summary>
public static class PoeMapExtension
{
    private const float MarsEllipticOrbit = 0.092f;
    private const float Offset = 5.434783f;

    /// <summary>
    /// Converts grid coordinates into world coordinates.
    /// </summary>
    /// <param name="v">The grid coordinate.</param>
    /// <returns>The world coordinate.</returns>
    public static Vector2 GridToWorld(this Vector2 v)
    {
        return new Vector2(v.X / MarsEllipticOrbit + Offset, v.Y / MarsEllipticOrbit + Offset);
    }

    /// <summary>
    /// Converts world coordinates into grid coordinates.
    /// </summary>
    /// <param name="v">The world coordinate (the Z component is ignored).</param>
    /// <returns>The grid coordinate.</returns>
    public static Vector2 WorldToGrid(this Vector3 v)
    {
        return new Vector2((float) Math.Floor(v.X * MarsEllipticOrbit), (float) Math.Floor(v.Y * MarsEllipticOrbit));
    }

    /// <summary>
    /// Converts world coordinates into grid coordinates.
    /// </summary>
    /// <param name="v">The world coordinate.</param>
    /// <returns>The grid coordinate.</returns>
    public static Vector2 WorldToGrid(this Vector2 v)
    {
        return new Vector2((float) Math.Floor(v.X * MarsEllipticOrbit), (float) Math.Floor(v.Y * MarsEllipticOrbit));
    }
}
