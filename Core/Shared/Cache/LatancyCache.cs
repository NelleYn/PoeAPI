using System;

namespace ExileCore.Shared.Cache;

/// <summary>
/// A cached value that is recomputed no more often than the current latency (or a configured minimum) allows.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public class LatancyCache<T> : CachedValue<T>
{
    private readonly int _minLatency;
    private long _checkTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatancyCache{T}"/> class.
    /// </summary>
    /// <param name="func">The function used to produce the cached value.</param>
    /// <param name="minLatency">The minimum interval, in milliseconds, between refreshes.</param>
    public LatancyCache(Func<T> func, int minLatency = 10) : base(func)
    {
        _minLatency = minLatency;
        _checkTime = long.MinValue;
    }

    protected override bool Update(bool force)
    {
        var curLatency = Latency;
        var time = sw.ElapsedMilliseconds;

        if (time >= _checkTime || force)
        {
            if (curLatency > _minLatency)
                _checkTime = (long) (time + curLatency);
            else
                _checkTime = time + _minLatency;

            return true;
        }

        return false;
    }
}
