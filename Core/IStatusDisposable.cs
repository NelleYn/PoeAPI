using System;

namespace ExileCore;
public interface IStatusDisposable : IDisposable
{
    bool IsSuccess { get; }
}