using System;

namespace ExileCore.Shared.Cache;

/// <summary>
/// A cached value that is recomputed whenever a supplied condition evaluates to <c>true</c>.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public class ConditionalCache<T> : CachedValue<T>
{
    private readonly Func<bool> _cond;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalCache{T}"/> class.
    /// </summary>
    /// <param name="func">The function used to produce the cached value.</param>
    /// <param name="cond">The condition that triggers a refresh when it returns <c>true</c>.</param>
    public ConditionalCache(Func<T> func, Func<bool> cond) : base(func)
    {
        _cond = cond;
    }

    protected override bool Update(bool force)
    {
        if (_cond() || force) return true;

        return false;
    }
}
