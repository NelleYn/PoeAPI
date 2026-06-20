using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ExileCore;

/// <summary>
/// Provides the application-wide Serilog logger, lazily configured to write a separate rolling
/// log file per log level under the <c>Logs</c> directory.
/// </summary>
public class Logger
{
    private static ILogger _instance;

    /// <summary>The shared logger instance, created on first access.</summary>
    public static ILogger Log =>
        _instance ?? (_instance = new LoggerConfiguration().MinimumLevel
            .ControlledBy(new LoggingLevelSwitch(LogEventLevel.Verbose)).WriteTo
            .Logger(l => l.Filter.ByIncludingOnly(
                    e => e.Level == LogEventLevel.Information).WriteTo
                .RollingFile(@"Logs\Info-{Date}.log")).WriteTo
            .Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Debug)
                .WriteTo.RollingFile(@"Logs\Debug-{Date}.log")).WriteTo
            .Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Warning)
                .WriteTo.RollingFile(@"Logs\Warning-{Date}.log")).WriteTo
            .Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
                .WriteTo.RollingFile(@"Logs\Error-{Date}.log")).WriteTo
            .Logger(l => l.Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Fatal)
                .WriteTo.RollingFile(@"Logs\Fatal-{Date}.log")).WriteTo
            .RollingFile(@"Logs\Verbose-{Date}.log").CreateLogger());
}
