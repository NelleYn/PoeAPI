using System;

namespace ExileCore;
internal class DisposableAction : IDisposable
{
    private readonly Action _action;
    public DisposableAction(Action action)
    {
        _action = action;
    }

    public void Dispose()
    {
        _action?.Invoke();
    }
}