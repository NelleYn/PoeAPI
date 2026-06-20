// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore.Shared;
public static partial class TaskUtils
{
    public static ExileCore.Shared.SyncTask<System.Boolean> CheckEveryFrame(System.Func<System.Boolean> condition, System.Threading.CancellationToken cancellationToken)
    {
        throw new global::System.NotImplementedException();
    }

    public static ExileCore.Shared.SyncTask<System.Boolean> CheckEveryFrameWithThrow(System.Func<System.Boolean> condition, System.Threading.CancellationToken cancellationToken)
    {
        throw new global::System.NotImplementedException();
    }

    public static ExileCore.Shared.SyncTask<T> RunOrRestart<T>(ref ExileCore.Shared.SyncTask<T> oldTask, System.Func<ExileCore.Shared.SyncTask<T>> taskProvider)
    {
        throw new global::System.NotImplementedException();
    }

    public static void ClearIfCompleted<T>(ref ExileCore.Shared.SyncTask<T> oldTask, System.Func<ExileCore.Shared.SyncTask<T>> taskProvider)
    {
        throw new global::System.NotImplementedException();
    }

    public static ExileCore.Shared.NextFrameTask NextFrame()
    {
        throw new global::System.NotImplementedException();
    }
}