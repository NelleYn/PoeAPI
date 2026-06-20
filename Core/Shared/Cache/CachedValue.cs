using System;
using System.Diagnostics;
using System.Threading;

namespace ExileCore.Shared.Cache;

/// <summary>
/// Non-generic base for <see cref="CachedValue{T}"/> exposing shared instance counters and latency.
/// </summary>
public abstract class CachedValue
{
    /// <summary>Total number of cached values ever created.</summary>
    public static int TotalCount;

    /// <summary>Number of cached values currently alive (not yet finalized).</summary>
    public static int LifeCount;

    /// <summary>The latency (in milliseconds) used by latency-based caches.</summary>
    public static float Latency { get; set; } = 25;
}

/// <summary>
/// A lazily-evaluated value that is refreshed according to a derived cache policy.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public abstract partial class CachedValue<T> : CachedValue
{
    /// <summary>
    /// Delegate invoked whenever the cached value is refreshed.
    /// </summary>
    /// <param name="t">The newly computed value.</param>
    public delegate void CacheUpdateEvent(T t);

    protected static Stopwatch sw = Stopwatch.StartNew();
    private readonly Func<T> _func;
    private bool _force;
    private T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedValue{T}"/> class.
    /// </summary>
    /// <param name="func">The function used to produce the cached value.</param>
    protected CachedValue(Func<T> func)
    {
        _func = func ?? throw new ArgumentNullException(nameof(func), "Cached Value ctor null function");
        Interlocked.Increment(ref TotalCount);
        Interlocked.Increment(ref LifeCount);
    }

    /// <summary>
    /// Gets the cached value, refreshing it via the underlying function if the cache policy demands an update.
    /// </summary>
    public T Value
    {
        get
        {
            if (Update(_force))
            {
                {
                    _force = false;
                    _value = _func();
                }
                OnUpdate?.Invoke(_value);
                _updated = true;
                return _value;
            }
            else
            {
                if (!_updated)
                {
                    return _func();
                }

            }
            return _value;
        }
    }

    private bool _updated = false;

    /// <summary>Gets the freshly computed value, bypassing the cache.</summary>
    public T RealValue => _func();

    /// <summary>Raised whenever the cached value is refreshed.</summary>
    public event CacheUpdateEvent OnUpdate;

    /// <summary>
    /// Forces the next read of <see cref="Value"/> to recompute the value.
    /// </summary>
    public void ForceUpdate()
    {
        _force = true;
    }

    /// <summary>
    /// Determines whether the cached value should be recomputed.
    /// </summary>
    /// <param name="force">Whether an update has been explicitly forced.</param>
    /// <returns><c>true</c> if the value should be recomputed; otherwise <c>false</c>.</returns>
    protected abstract bool Update(bool force);

    ~CachedValue()
    {
        Interlocked.Decrement(ref LifeCount);
    }
}
