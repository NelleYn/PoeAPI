using System;
using System.Numerics;

namespace ExileCore;
public class DefaultInputManager : IInputManager
{
    private class StatusDisposable : IStatusDisposable, IDisposable
    {
        public bool IsSuccess => false;

        public void Dispose()
        {
        }
    }

    public virtual IStatusDisposable BlockUserMouseInput()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public virtual IStatusDisposable BlockUserKeyboardInput()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public virtual bool MoveMouse(Vector2 coordinate)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public virtual System.Threading.Tasks.Task<bool> MoveMouseAsync(MouseMoveStroke stroke, System.Threading.CancellationToken cancellationToken)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public virtual ExileCore.Shared.SyncTask<bool> MoveMouseSyncTask(MouseMoveStroke stroke, System.Threading.CancellationToken cancellationToken)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}