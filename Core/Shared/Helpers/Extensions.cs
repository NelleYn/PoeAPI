using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using JM.LinqFaster;
using SharpDX;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// General-purpose extension methods for colors, vectors, enums and reflection helpers.
/// </summary>
public static class Extensions
{
    private static readonly Color[] Colors;
    private static readonly Dictionary<string, MapIconsIndex> Icons;

    static Extensions()
    {
        var fieldInfos = typeof(Color).GetFields(BindingFlags.Public | BindingFlags.Static);

        Colors = new Color[fieldInfos.Length];
        ColorName = new Dictionary<string, Color>(fieldInfos.Length);
        ColorHex = new Dictionary<Color, string>(fieldInfos.Length);

        for (var index = 0; index < fieldInfos.Length; index++)
        {
            var fieldInfo = fieldInfos[index];
            var clr = (Color) fieldInfo.GetValue(typeof(Color));
            ColorName[fieldInfo.Name] = clr;
            ColorName[fieldInfo.Name.ToLower()] = clr;
            ColorHex[clr] = clr.ToRgba().ToString("X");
            if (clr != Color.Transparent) Colors[index] = clr;
        }

        Icons = new Dictionary<string, MapIconsIndex>(200);

        foreach (var icon in Enum.GetValues(typeof(MapIconsIndex)))
        {
            Icons[icon.ToString()] = (MapIconsIndex) icon;
        }
    }

    private static Dictionary<string, Color> ColorName { get; } = new Dictionary<string, Color>();
    private static Dictionary<Color, string> ColorHex { get; } = new Dictionary<Color, string>();

    /// <summary>
    /// Returns a random color from the set of named SharpDX colors.
    /// </summary>
    /// <param name="c">The receiver color (unused; provided for fluent syntax).</param>
    /// <returns>A randomly chosen color.</returns>
    public static Color GetRandomColor(this Color c)
    {
        return Colors[MathHepler.Randomizer.Next(0, Colors.Length - 1)];
    }

    /// <summary>
    /// Resolves a <see cref="MapIconsIndex" /> from its name.
    /// </summary>
    /// <param name="name">The icon name.</param>
    /// <returns>The matching icon index, or the default value if not found.</returns>
    public static MapIconsIndex IconIndexByName(string name)
    {
        Icons.TryGetValue(name, out var result);
        return result;
    }

    /// <summary>
    /// Resolves a color from its name, returning <see cref="Color.Zero" /> when unknown.
    /// </summary>
    /// <param name="name">The color name.</param>
    /// <returns>The matching color, or <see cref="Color.Zero" /> if not found.</returns>
    public static Color GetColorByName(string name)
    {
        return ColorName.TryGetValue(name, out var result) ? result : Color.Zero;
    }

    /// <summary>
    /// Returns the cached hexadecimal RGBA string for the given color.
    /// </summary>
    /// <param name="clr">The color to format.</param>
    /// <returns>The hexadecimal string, or the transparent color's hex when not cached.</returns>
    public static string Hex(this Color clr)
    {
        return ColorHex.TryGetValue(clr, out var result) ? result : ColorHex[Color.Transparent];
    }

    /// <summary>
    /// Converts a color into a packed RGBA value suitable for ImGui.
    /// </summary>
    /// <param name="c">The color to convert.</param>
    /// <returns>The packed RGBA value.</returns>
    public static uint ToImgui(this Color c)
    {
        return (uint) c.ToRgba();
    }

    /// <summary>
    /// Converts a color into a normalized RGBA vector for ImGui.
    /// </summary>
    /// <param name="c">The color to convert.</param>
    /// <returns>A vector with components in the range 0..1.</returns>
    public static Vector4 ToImguiVec4(this Color c)
    {
        return new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
    }

    /// <summary>
    /// Converts a color into an RGBA vector, overriding the alpha component.
    /// </summary>
    /// <param name="c">The color to convert.</param>
    /// <param name="alpha">The alpha value to use.</param>
    /// <returns>A vector with the color's RGB components and the supplied alpha.</returns>
    public static Vector4 ToImguiVec4(this Color c, byte alpha)
    {
        return new Vector4(c.R, c.G, c.B, alpha);
    }

    /// <summary>
    /// Converts a SharpDX vector into a numeric vector.
    /// </summary>
    /// <param name="v">The vector to convert.</param>
    /// <returns>The numeric vector.</returns>
    public static Vector4 ToVector4Num(this SharpDX.Vector4 v)
    {
        return new Vector4(v.X, v.Y, v.Z, v.W);
    }

    /// <summary>
    /// Converts a SharpDX vector into a numeric vector.
    /// </summary>
    /// <param name="v">The vector to convert.</param>
    /// <returns>The numeric vector.</returns>
    public static Vector2 ToVector2Num(this SharpDX.Vector2 v)
    {
        return new Vector2(v.X, v.Y);
    }

    /// <summary>
    /// Converts a numeric vector into a SharpDX color.
    /// </summary>
    /// <param name="v">The vector whose components map to RGBA.</param>
    /// <returns>The resulting color.</returns>
    public static Color ToSharpColor(this Vector4 v)
    {
        return new Color(v.X, v.Y, v.Z, v.W);
    }

    /// <summary>
    /// Reads the <c>FieldOffset</c> attribute of a named field on a struct type via reflection.
    /// </summary>
    /// <typeparam name="T">The struct type to inspect.</typeparam>
    /// <param name="name">The name of the field.</param>
    /// <returns>The field's offset.</returns>
    public static int GetOffset<T>(string name) where T : struct
    {
        try
        {
            var type = typeof(T);

            var offset = (int) type.GetFields().FirstF(x => x.Name == name).GetCustomAttributesData()
                .First(x => x.AttributeType.Name.Equals("FieldOffsetAttribute", StringComparison.Ordinal))
                .ConstructorArguments.First().Value;

            return offset;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Creates a <see cref="ValidCache{T}" /> bound to the given entity and value factory.
    /// </summary>
    /// <typeparam name="T">The cached value type.</typeparam>
    /// <param name="entity">The entity whose validity controls the cache.</param>
    /// <param name="func">The factory producing the cached value.</param>
    /// <returns>The new cache instance.</returns>
    public static ValidCache<T> ValidCache<T>(this Entity entity, Func<T> func)
    {
        return new ValidCache<T>(entity, func);
    }

    /// <summary>
    /// Parses a hexadecimal character span into an unsigned integer.
    /// </summary>
    /// <param name="span">The span of hexadecimal characters.</param>
    /// <returns>The parsed unsigned integer.</returns>
    public static uint HexToUInt(this ReadOnlySpan<char> span)
    {
        uint num1 = 0;

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];

            if (num1 > 268435455U)
                return num1;

            num1 *= 16U;
            if (c == char.MinValue) continue;
            var num2 = num1;

            if (c >= '0' && c <= '9')
                num2 += c - 48U;
            else if (c >= 'A' && c <= 'F')
                num2 += (uint) c - 65 + 10;
            else
                num2 += (uint) c - 97 + 10;

            if (num2 < num1)
                return num1;

            num1 = num2;
        }

        return num1;
    }
}
