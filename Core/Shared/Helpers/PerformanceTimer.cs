using System;
using System.Diagnostics;
using System.Threading;
using Serilog;

namespace ExileCore.Shared.Helpers;

/// <summary>
/// A disposable stopwatch that logs the elapsed time when disposed, provided it exceeds a trigger threshold.
/// </summary>
public struct PerformanceTimer : IDisposable
{
    private readonly string DebugText;
    private readonly Action<string, TimeSpan> FinishedCallback;
    private readonly int TriggerMs;
    private readonly bool Log;

    /// <summary>
    /// When <see langword="true" />, suppresses all timing output regardless of the trigger threshold.
    /// </summary>
    public static bool IgnoreTimer = false;

    /// <summary>
    /// The logger used to report elapsed timings.
    /// </summary>
    public static ILogger Logger;

    private readonly Stopwatch sw;

    /// <summary>
    /// Initializes and starts a new performance timer.
    /// </summary>
    /// <param name="debugText">A label describing the timed operation.</param>
    /// <param name="triggerMs">The minimum elapsed time, in milliseconds, required for the timer to report.</param>
    /// <param name="callback">An optional callback invoked with the label and elapsed time when the timer reports.</param>
    /// <param name="log">Whether to log the elapsed time via <see cref="Logger" />.</param>
    public PerformanceTimer(string debugText, int triggerMs = 0, Action<string, TimeSpan> callback = null, bool log = true)
    {
        FinishedCallback = callback;
        DebugText = debugText;
        TriggerMs = triggerMs;
        Log = log;
        sw = Stopwatch.StartNew();
    }

    /// <summary>
    /// Stops the timer and reports the elapsed time.
    /// </summary>
    public void Dispose()
    {
        StopAndPrint();
    }

    /// <summary>
    /// Stops the timer and, if the elapsed time meets the trigger threshold, logs it and invokes the callback.
    /// </summary>
    public void StopAndPrint()
    {
        if (!sw.IsRunning) return;
        sw.Stop();

        if (sw.ElapsedMilliseconds >= TriggerMs && !IgnoreTimer)
        {
            var elapsed = sw.Elapsed;

            if (Log)
            {
                Logger.Information(
                    $"PerfTimer =-> {DebugText} ({elapsed.TotalMilliseconds} ms) Thread #[{Thread.CurrentThread.ManagedThreadId}]");
            }

            FinishedCallback?.Invoke(DebugText, elapsed);
        }
    }
}
