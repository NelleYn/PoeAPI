using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExileCore.Shared.Interfaces;
using GameOffsets;
using GameOffsets.Native;
using SharpDX;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// Miscellaneous helper methods for strings, time formatting, memory strings and config loading.
/// </summary>
public static class MiscHelpers
{
    /// <summary>
    /// Inserts the given string before each uppercase character (after the first) that is not already preceded by a space.
    /// </summary>
    /// <param name="str">The source string.</param>
    /// <param name="append">The string to insert before uppercase characters.</param>
    /// <returns>The transformed string.</returns>
    public static string InsertBeforeUpperCase(this string str, string append)
    {
        var sb = new StringBuilder();

        var previousChar = char.MinValue; // Unicode '\0'

        foreach (var c in str)
        {
            if (char.IsUpper(c))
            {
                // If not the first character and previous character is not a space, insert a space before uppercase

                if (sb.Length != 0 && previousChar != ' ')
                    sb.Append(append);
            }

            sb.Append(c);

            previousChar = c;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a time span as a "h:mm:ss" (or "m:ss" when under an hour) string.
    /// </summary>
    /// <param name="timeSpent">The time span to format.</param>
    /// <returns>The formatted time string.</returns>
    public static string GetTimeString(TimeSpan timeSpent)
    {
        var allsec = (int) timeSpent.TotalSeconds;
        var secs = allsec % 60;
        var mins = allsec / 60;
        var hours = mins / 60;
        mins = mins % 60;
        return string.Format(hours > 0 ? "{0}:{1:00}:{2:00}" : "{1}:{2:00}", hours, mins, secs);
    }

    /// <summary>
    /// Reads the managed string represented by a native Unicode string, handling the small-string optimization.
    /// </summary>
    /// <param name="str">The native string offsets.</param>
    /// <param name="mem">The memory accessor used to read remote strings.</param>
    /// <returns>The decoded string.</returns>
    public static string ToString(this NativeStringU str, IMemory mem)
    {
        if (str.Capacity >= 8)
        {
            if (str.Size < 256)
                return mem.ReadStringU(str.buf, (int) str.Size * 2);

            return mem.ReadStringU(str.buf);
        }

        return Encoding.Unicode.GetString(BitConverter.GetBytes(str.buf).Concat(BitConverter.GetBytes(str.buf2))
            .Take((int) str.Size * 2).ToArray());
    }

    /// <summary>
    /// Reads the path string described by the given entity path offsets.
    /// </summary>
    /// <param name="str">The path entity offsets.</param>
    /// <param name="mem">The memory accessor used to read remote strings.</param>
    /// <returns>The decoded path string.</returns>
    public static string ToString(this PathEntityOffsets str, IMemory mem)
    {
        return mem.ReadStringU(str.Path.Ptr, (int) str.Length * 2);
    }

    /// <summary>
    /// Parses a string into a value of the given enum type, ignoring case.
    /// </summary>
    /// <typeparam name="T">The enum type to parse into.</typeparam>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed enum value.</returns>
    public static T ToEnum<T>(this string value)
    {
        return (T) Enum.Parse(typeof(T), value, true);
    }

    /// <summary>
    /// Returns a random point inside the given rectangle, inset by the supplied margins.
    /// </summary>
    /// <param name="clientRect">The bounding rectangle.</param>
    /// <param name="x">The horizontal inset.</param>
    /// <param name="y">The vertical inset.</param>
    /// <returns>A random point within the inset rectangle.</returns>
    public static Vector2 ClickRandom(this RectangleF clientRect, int x = 3, int y = 3)
    {
        var resX = MathHepler.Randomizer.Next((int) clientRect.TopLeft.X + x, (int) clientRect.TopRight.X - x);
        var resY = MathHepler.Randomizer.Next((int) clientRect.TopLeft.Y + y, (int) clientRect.BottomLeft.Y - x);
        return new Vector2(resX, resY);
    }

    /// <summary>
    /// Runs the given action under a performance timer that logs the elapsed time to the debug window.
    /// </summary>
    /// <param name="act">The action to time and run.</param>
    /// <param name="msg">The message describing the operation.</param>
    /// <param name="time">The debug-window message duration, in seconds.</param>
    /// <param name="log">Whether to also log via the timer's own logger.</param>
    public static void PerfTimerLogMsg(Action act, string msg, float time = 3f, bool log = false)
    {
        using (new PerformanceTimer(
            msg, 0, (s, span) => DebugWindow.LogMsg($"{s} -> {span.TotalMilliseconds} ms.", time, Color.Zero.GetRandomColor()), false))
        {
            act?.Invoke();
        }
    }

    /// <summary>
    /// Loads a semicolon-delimited config file, skipping blank lines and comments (lines starting with '#').
    /// </summary>
    /// <param name="path">The path to the config file.</param>
    /// <param name="columnsCount">The maximum number of columns to split each line into.</param>
    /// <returns>The parsed rows, each as a trimmed array of column values.</returns>
    public static IEnumerable<string[]> LoadConfigBase(string path, int columnsCount = 2)
    {
        return File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line) && line.IndexOf(';') >= 0 && !line.StartsWith("#"))
            .Select(line => line.Split(new[] {';'}, columnsCount).Select(parts => parts.Trim()).ToArray());
    }
}
