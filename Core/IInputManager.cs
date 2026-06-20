// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore;
public partial interface IInputManager
{
    public ExileCore.IStatusDisposable BlockUserMouseInput()
    {
        throw new global::System.NotImplementedException();
    }

    public ExileCore.IStatusDisposable BlockUserKeyboardInput()
    {
        throw new global::System.NotImplementedException();
    }

    public System.Boolean MoveMouse(System.Numerics.Vector2 coordinate)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Threading.Tasks.Task<System.Boolean> MoveMouseAsync(ExileCore.MouseMoveStroke stroke, System.Threading.CancellationToken cancellationToken)
    {
        throw new global::System.NotImplementedException();
    }

    public ExileCore.Shared.SyncTask<System.Boolean> MoveMouseSyncTask(ExileCore.MouseMoveStroke stroke, System.Threading.CancellationToken cancellationToken)
    {
        throw new global::System.NotImplementedException();
    }
}