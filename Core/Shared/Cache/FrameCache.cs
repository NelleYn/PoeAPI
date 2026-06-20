using System;

namespace ExileCore.Shared.Cache;

/// <summary>
/// A cached value that is recomputed once per rendered frame.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public class FrameCache<T> : CachedValue<T>
{
    private uint _frame;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameCache{T}"/> class.
    /// </summary>
    /// <param name="func">The function used to produce the cached value.</param>
    public FrameCache(Func<T> func) : base(func)
    {
        _frame = uint.MaxValue;
    }

    protected override bool Update(bool force)
    {
        if (_frame != Core.FramesCount || force)
        {
            _frame = Core.FramesCount;
            return true;
        }

        return false;
    }
}
