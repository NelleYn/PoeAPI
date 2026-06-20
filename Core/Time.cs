using System.Diagnostics;

namespace ExileCore;

/// <summary>
/// Provides global timing information for the application, backed by a single process-wide stopwatch.
/// </summary>
public class Time
{
    private static Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

    /// <summary>Time elapsed since the previous frame.</summary>
    public static double DeltaTime { get; set; }

    /// <summary>Total milliseconds elapsed since the application started.</summary>
    public static double TotalMilliseconds => Stopwatch.Elapsed.TotalMilliseconds;

    /// <summary>Whole milliseconds elapsed since the application started.</summary>
    public static long ElapsedMilliseconds => Stopwatch.ElapsedMilliseconds;
}
