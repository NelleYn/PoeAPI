using System;
using System.Linq;
using SharpDX;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Math and vector helper methods used across the API.
/// </summary>
public static class MathHepler
{
    private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    static MathHepler()
    {
        Randomizer = new Random();
    }

    /// <summary>
    /// Gets the shared pseudo-random number generator used by the helpers.
    /// </summary>
    public static Random Randomizer { get; }

    /// <summary>
    /// Rotates a vector around the origin by the given angle.
    /// </summary>
    /// <param name="v">The vector to rotate.</param>
    /// <param name="angle">The rotation angle, in degrees.</param>
    /// <returns>The rotated vector.</returns>
    public static Vector2 RotateVector2(Vector2 v, float angle)
    {
        var theta = ConvertToRadians(angle);

        var cs = Math.Cos(theta);
        var sn = Math.Sin(theta);
        var px = v.X * cs - v.Y * sn;
        var py = v.X * sn + v.Y * cs;
        return new Vector2((float) px, (float) py);
    }

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="angle">The angle, in degrees.</param>
    /// <returns>The angle, in radians.</returns>
    public static double ConvertToRadians(double angle)
    {
        return Math.PI / 180 * angle;
    }

    /// <summary>
    /// Returns a unit-length copy of the given vector.
    /// </summary>
    /// <param name="vec">The vector to normalize.</param>
    /// <returns>The normalized vector.</returns>
    public static Vector2 NormalizeVector(Vector2 vec)
    {
        var length = VectorLength(vec);
        vec.X /= length;
        vec.Y /= length;
        return vec;
    }

    /// <summary>
    /// Computes the magnitude of the given vector.
    /// </summary>
    /// <param name="vec">The vector.</param>
    /// <returns>The vector length.</returns>
    public static float VectorLength(Vector2 vec)
    {
        return (float) Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
    }

    /// <summary>
    /// Converts a vector into polar coordinates.
    /// </summary>
    /// <param name="vector">The vector to convert.</param>
    /// <param name="phi">When this method returns, contains the polar angle.</param>
    /// <returns>The radial distance of the vector.</returns>
    public static double GetPolarCoordinates(this Vector2 vector, out double phi)
    {
        double distance = vector.Length();
        phi = Math.Acos(vector.X / distance);
        if (vector.Y < 0) phi = MathUtil.TwoPi - phi;
        return distance;
    }

    /// <summary>
    /// Generates a random alphanumeric string of the requested length.
    /// </summary>
    /// <param name="length">The number of characters to generate.</param>
    /// <returns>The random string.</returns>
    public static string GetRandomWord(int length)
    {
        var array = new char[length];

        for (var i = 0; i < length; i++)
        {
            array[i] = CHARS[Randomizer.Next(CHARS.Length)];
        }

        return new string(array);
    }

    /// <summary>
    /// Returns the largest of the supplied values.
    /// </summary>
    /// <param name="values">The values to compare.</param>
    /// <returns>The maximum value.</returns>
    public static float Max(params float[] values)
    {
        var max = values.First();

        for (var i = 1; i < values.Length; i++)
        {
            max = Math.Max(max, values[i]);
        }

        return max;
    }

    /// <summary>
    /// Returns a copy of the vector translated by the given offsets.
    /// </summary>
    /// <param name="vector">The vector to translate.</param>
    /// <param name="dx">The offset to add to the X component.</param>
    /// <param name="dy">The offset to add to the Y component.</param>
    /// <returns>The translated vector.</returns>
    public static Vector2 Translate(this Vector2 vector, float dx = 0f, float dy = 0f)
    {
        return new Vector2(vector.X + dx, vector.Y + dy);
    }

    /// <summary>
    /// Returns a copy of the vector translated by another vector.
    /// </summary>
    /// <param name="vector">The vector to translate.</param>
    /// <param name="vector2">The translation offset.</param>
    /// <returns>The translated vector.</returns>
    public static System.Numerics.Vector2 Translate(this System.Numerics.Vector2 vector, System.Numerics.Vector2 vector2)
    {
        return new System.Numerics.Vector2(vector.X + vector2.X, vector.Y + vector2.Y);
    }

    /// <summary>
    /// Returns a copy of the vector translated by the given offsets.
    /// </summary>
    /// <param name="vector">The vector to translate.</param>
    /// <param name="dx">The offset to add to the X component.</param>
    /// <param name="dy">The offset to add to the Y component.</param>
    /// <returns>The translated vector.</returns>
    public static System.Numerics.Vector2 Translate(this System.Numerics.Vector2 vector, float dx = 0f, float dy = 0f)
    {
        return new System.Numerics.Vector2(vector.X + dx, vector.Y + dy);
    }

    /// <summary>
    /// Translates a numeric vector and returns it as a SharpDX vector.
    /// </summary>
    /// <param name="vector">The vector to translate.</param>
    /// <param name="dx">The offset to add to the X component.</param>
    /// <param name="dy">The offset to add to the Y component.</param>
    /// <returns>The translated SharpDX vector.</returns>
    public static Vector2 TranslateToNum(this System.Numerics.Vector2 vector, float dx = 0f, float dy = 0f)
    {
        return new Vector2(vector.X + dx, vector.Y + dy);
    }

    /// <summary>
    /// Returns a copy of the vector with each component multiplied by the given factors.
    /// </summary>
    /// <param name="vector">The vector to scale.</param>
    /// <param name="dx">The factor for the X component.</param>
    /// <param name="dy">The factor for the Y component.</param>
    /// <returns>The scaled vector.</returns>
    public static System.Numerics.Vector2 Mult(this System.Numerics.Vector2 vector, float dx = 1f, float dy = 1f)
    {
        return new System.Numerics.Vector2(vector.X * dx, vector.Y * dy);
    }

    /// <summary>
    /// Returns a copy of the vector translated by the given offsets.
    /// </summary>
    /// <param name="vector">The vector to translate.</param>
    /// <param name="dx">The offset to add to the X component.</param>
    /// <param name="dy">The offset to add to the Y component.</param>
    /// <param name="dz">The offset to add to the Z component.</param>
    /// <returns>The translated vector.</returns>
    public static Vector3 Translate(this Vector3 vector, float dx, float dy, float dz)
    {
        return new Vector3(vector.X + dx, vector.Y + dy, vector.Z + dz);
    }

    /// <summary>
    /// Computes the distance between two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The Euclidean distance.</returns>
    public static float Distance(this Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b);
    }

    /// <summary>
    /// Computes the squared distance between two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The squared Euclidean distance.</returns>
    public static float DistanceSquared(this Vector2 a, Vector2 b)
    {
        return Vector2.DistanceSquared(a, b);
    }

    /// <summary>
    /// Determines whether the point lies within the given rectangle.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <param name="rect">The rectangle.</param>
    /// <returns><see langword="true" /> if the point is inside the rectangle; otherwise, <see langword="false" />.</returns>
    public static bool PointInRectangle(this Vector2 point, RectangleF rect)
    {
        return point.X >= rect.X && point.Y >= rect.Y && point.X <= rect.Width && point.Y <= rect.Height;
    }

    /// <summary>
    /// Computes the UV rectangle for a directional sprite given a polar angle and distance.
    /// </summary>
    /// <param name="phi">The polar angle, in radians.</param>
    /// <param name="distance">The distance used to select the sprite row.</param>
    /// <returns>The UV rectangle for the sprite.</returns>
    public static RectangleF GetDirectionsUV(double phi, double distance)
    {
        // could not find a better place yet
        phi += Math.PI * 0.25; // fix rotation due to projection
        if (phi > 2 * Math.PI) phi -= 2 * Math.PI;

        var xSprite = (float) Math.Round(phi / Math.PI * 4);
        if (xSprite >= 8) xSprite = 0;

        float ySprite = distance > 60 ? distance > 120 ? 2 : 1 : 0;
        var x = xSprite / 8;
        var y = ySprite / 3;
        return new RectangleF(x, y, (xSprite + 1) / 8 - x, (ySprite + 1) / 3 - y);
    }
}
