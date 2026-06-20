using System;
using System.Drawing;
using System.Globalization;
using ExileCore.Shared.Nodes;
using SharpDX;
using Color = SharpDX.Color;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Helper methods for converting between colors, vectors and string representations.
/// </summary>
public static class ConvertHelper
{
    /// <summary>
    /// Formats a number using a compact "K"/"M" suffix for large magnitudes.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The numeric format string used when no suffix is applied.</param>
    /// <returns>The shortened string representation of the value.</returns>
    public static string ToShorten(double value, string format = "0")
    {
        var abs = Math.Abs(value);
        if (abs >= 1000000) return string.Concat((value / 1000000).ToString("F2"), "M");

        if (abs >= 1000) return string.Concat((value / 1000).ToString("F1"), "K");

        return value.ToString(format);
    }

    /// <summary>
    /// Parses a hexadecimal BGRA string into a <see cref="Color" />, returning <see cref="Color.Black" /> on failure.
    /// </summary>
    /// <param name="value">The hexadecimal BGRA string.</param>
    /// <returns>The parsed color, or <see cref="Color.Black" /> if parsing fails.</returns>
    public static Color ToBGRAColor(this string value)
    {
        return uint.TryParse(value, NumberStyles.HexNumber, null, out var bgra) ? Color.FromBgra(bgra) : Color.Black;
    }

    /// <summary>
    /// Extracts a color value from a config line at the given index, or <see langword="null" /> if absent.
    /// </summary>
    /// <param name="line">The split config line.</param>
    /// <param name="index">The column index to read.</param>
    /// <returns>The parsed color, or <see langword="null" /> if the value is missing or empty.</returns>
    public static Color? ConfigColorValueExtractor(this string[] line, int index)
    {
        return IsNotNull(line, index) ? (Color?) line[index].ToBGRAColor() : null;
    }

    /// <summary>
    /// Extracts a string value from a config line at the given index, or <see langword="null" /> if absent.
    /// </summary>
    /// <param name="line">The split config line.</param>
    /// <param name="index">The column index to read.</param>
    /// <returns>The value, or <see langword="null" /> if the value is missing or empty.</returns>
    public static string ConfigValueExtractor(this string[] line, int index)
    {
        return IsNotNull(line, index) ? line[index] : null;
    }

    private static bool IsNotNull(string[] line, int index)
    {
        return line.Length > index && !string.IsNullOrEmpty(line[index]);
    }

    /// <summary>
    /// Converts the value of a <see cref="ColorNode" /> into a numeric <see cref="Vector3" />.
    /// </summary>
    /// <param name="color">The color node to convert.</param>
    /// <returns>A vector with the color's RGB components.</returns>
    public static Vector3 ColorNodeToVector3(this ColorNode color)
    {
        var vector3 = color.Value.ToVector3();
        return new Vector3(vector3.X, vector3.Y, vector3.Z);
    }

    /// <summary>
    /// Converts a SharpDX vector to a numeric vector, optionally translated by the given offsets.
    /// </summary>
    /// <param name="vector">The vector to convert.</param>
    /// <param name="dx">The offset to add to the X component.</param>
    /// <param name="dy">The offset to add to the Y component.</param>
    /// <returns>The translated numeric vector.</returns>
    public static Vector2 TranslateToNum(this SharpDX.Vector2 vector, float dx = 0, float dy = 0)
    {
        return new Vector2(vector.X + dx, vector.Y + dy);
    }

    /// <summary>
    /// Converts a SharpDX vector to a numeric vector, optionally translated by the given offsets.
    /// </summary>
    /// <param name="vector">The vector to convert.</param>
    /// <param name="dx">The offset to add to the X component.</param>
    /// <param name="dy">The offset to add to the Y component.</param>
    /// <param name="dz">The offset to add to the Z component.</param>
    /// <param name="dw">The offset to add to the W component.</param>
    /// <returns>The translated numeric vector.</returns>
    public static Vector4 TranslateToNum(this SharpDX.Vector4 vector, float dx = 0, float dy = 0, float dz = 0, float dw = 0)
    {
        return new Vector4(vector.X + dx, vector.Y + dy, vector.Z + dz, vector.W + dw);
    }

    /// <summary>
    /// Returns a copy of the numeric vector translated by the given offsets.
    /// </summary>
    /// <param name="vector">The vector to translate.</param>
    /// <param name="dx">The offset to add to the X component.</param>
    /// <param name="dy">The offset to add to the Y component.</param>
    /// <param name="dz">The offset to add to the Z component.</param>
    /// <returns>The translated numeric vector.</returns>
    public static Vector3 TranslateToNum(this Vector3 vector, float dx = 0, float dy = 0, float dz = 0)
    {
        return new Vector3(vector.X + dx, vector.Y + dy, vector.Z + dz);
    }

    /// <summary>
    /// Converts a color into its HTML hex string representation.
    /// </summary>
    /// <param name="value">The color to convert.</param>
    /// <returns>The HTML hex string.</returns>
    public static string ToHex(this Color value)
    {
        return ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(value.A, value.R, value.G, value.B));
    }

    /// <summary>
    /// Creates a color from hue, saturation and value (HSV) components.
    /// </summary>
    /// <param name="hue">The hue, in degrees.</param>
    /// <param name="saturation">The saturation, from 0 to 1.</param>
    /// <param name="value">The value (brightness), from 0 to 1.</param>
    /// <returns>The resulting color.</returns>
    public static Color ColorFromHsv(double hue, double saturation, double value)
    {
        var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        var f = hue / 60 - Math.Floor(hue / 60);

        value = value * 255;
        var v = Convert.ToByte(value);
        var p = Convert.ToByte(value * (1 - saturation));
        var q = Convert.ToByte(value * (1 - f * saturation));
        var t = Convert.ToByte(value * (1 - (1 - f) * saturation));

        switch (hi)
        {
            case 0:
                return new ColorBGRA(v, t, p, 255);

            case 1:
                return new ColorBGRA(q, v, p, 255);

            case 2:
                return new ColorBGRA(p, v, t, 255);

            case 3:
                return new ColorBGRA(p, q, v, 255);

            case 4:
                return new ColorBGRA(t, p, v, 255);

            default:
                return new ColorBGRA(v, p, q, 255);
        }
    }
}
