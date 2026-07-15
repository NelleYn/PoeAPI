namespace ExileCore;
public partial interface IInputManager
{
    ExileCore.IStatusDisposable BlockUserMouseInput();

    ExileCore.IStatusDisposable BlockUserKeyboardInput();

    System.Boolean MoveMouse(System.Numerics.Vector2 coordinate);

    System.Threading.Tasks.Task<System.Boolean> MoveMouseAsync(ExileCore.MouseMoveStroke stroke, System.Threading.CancellationToken cancellationToken);

    ExileCore.Shared.SyncTask<System.Boolean> MoveMouseSyncTask(ExileCore.MouseMoveStroke stroke, System.Threading.CancellationToken cancellationToken);
}
