using System;
using SharpDX;

namespace ExileCore;

/// <summary>
/// Extension methods for converting between the game's grid coordinates and world coordinates.
/// </summary>
public static class WorldPositionExtensions
{
    private const float MarsEllipticOrbit = 0.092f;
    private const float Offset = 5.434783f;

    /// <summary>Converts a 2D grid position to a 2D world position.</summary>
    public static Vector2 GridToWorld(this Vector2 v)
    {
        return new Vector2(v.X / MarsEllipticOrbit + Offset, v.Y / MarsEllipticOrbit + Offset);
    }

    /// <summary>Converts a 2D grid position to a 3D world position using the supplied height.</summary>
    public static Vector3 GridToWorld(this Vector2 v, float z)
    {
        return new Vector3(v.X / MarsEllipticOrbit + Offset, v.Y / MarsEllipticOrbit + Offset, z);
    }

    /// <summary>Converts a 3D world position to a 2D grid position.</summary>
    public static Vector2 WorldToGrid(this Vector3 v)
    {
        return new Vector2((float)Math.Floor(v.X * MarsEllipticOrbit), (float)Math.Floor(v.Y * MarsEllipticOrbit));
    }
}
