using System;

namespace ExileCore.Shared.Cache;

/// <summary>
/// A cached value that is recomputed whenever the current area instance changes.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public class AreaCache<T> : CachedValue<T>
{
    private uint _areaHash;

    /// <summary>
    /// Initializes a new instance of the <see cref="AreaCache{T}"/> class.
    /// </summary>
    /// <param name="func">The function used to produce the cached value.</param>
    public AreaCache(Func<T> func) : base(func)
    {
        _areaHash = uint.MaxValue;
    }

    protected override bool Update(bool force)
    {
        if (_areaHash != AreaInstance.CurrentHash || force)
        {
            _areaHash = AreaInstance.CurrentHash;
            return true;
        }

        return false;
    }
}
