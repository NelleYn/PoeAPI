using System;

namespace ExileCore.Shared.Cache;

/// <summary>
/// A cached value that is recomputed at most once every <c>waitFrames</c> rendered frames.
/// </summary>
/// <typeparam name="T">The type of the cached value.</typeparam>
public class FramesCache<T> : FrameCache<T>
{
    private readonly uint _waitFrames;
    private uint _frame;

    /// <summary>
    /// Initializes a new instance of the <see cref="FramesCache{T}"/> class.
    /// </summary>
    /// <param name="func">The function used to produce the cached value.</param>
    /// <param name="waitFrames">The number of frames to wait between refreshes.</param>
    public FramesCache(Func<T> func, uint waitFrames = 1) : base(func)
    {
        _waitFrames = waitFrames;
        _frame = uint.MinValue;
    }

    protected override bool Update(bool force)
    {
        if (Core.FramesCount >= _frame || force)
        {
            _frame += _waitFrames;
            return true;
        }

        return false;
    }
}
