// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore.Shared;
public partial class SyncTaskMethodBuilder<T>
{
    public System.Runtime.CompilerServices.IAsyncStateMachine _stateMachine;
    public ExileCore.Shared.SyncTask<T> Task
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public static ExileCore.Shared.SyncTaskMethodBuilder<T> Create()
    {
        throw new global::System.NotImplementedException();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
    {
        throw new global::System.NotImplementedException();
    }

    public void SetStateMachine(System.Runtime.CompilerServices.IAsyncStateMachine stateMachine)
    {
        throw new global::System.NotImplementedException();
    }

    public void SetException(System.Exception exception)
    {
        throw new global::System.NotImplementedException();
    }

    public void SetResult(T result)
    {
        throw new global::System.NotImplementedException();
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
    {
        throw new global::System.NotImplementedException();
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
    {
        throw new global::System.NotImplementedException();
    }
}