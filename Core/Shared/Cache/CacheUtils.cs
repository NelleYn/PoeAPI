using System;

namespace ExileCore.Shared.Cache;

/// <summary>
/// Small caching helpers that ExileApi-Compiled exposes on <c>ExileCore.Shared.Cache.CacheUtils</c>.
/// </summary>
public static class CacheUtils
{
    /// <summary>
    /// Wraps a fold-style producer into a parameterless function that feeds each call the value the
    /// previous call returned, seeded with <paramref name="initialValue"/>.
    /// </summary>
    /// <typeparam name="T">The value type carried between calls.</typeparam>
    /// <param name="valueProducer">
    /// Receives the previous value and returns the next one. A producer that returns its argument
    /// unchanged makes the resulting function a plain "last value" accessor.
    /// </param>
    /// <param name="initialValue">The value handed to the first call. Defaults to <c>default(T)</c>.</param>
    /// <returns>
    /// A function that, on each invocation, calls <paramref name="valueProducer"/> with the running
    /// value and returns (and stores) the result.
    /// </returns>
    /// <remarks>
    /// The running value lives in the returned closure, so each call to
    /// <see cref="RememberLastValue{T}"/> gets its own independent state. The closure is
    /// <b>not</b> thread-safe: concurrent invocations of the returned function race on the captured
    /// value. Call it from a single thread (e.g. the render thread), or guard it yourself.
    /// </remarks>
    public static Func<T> RememberLastValue<T>(Func<T, T> valueProducer, T initialValue = default)
    {
        if (valueProducer == null)
            throw new ArgumentNullException(nameof(valueProducer));

        return () => initialValue = valueProducer(initialValue);
    }
}
