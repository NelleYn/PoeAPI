// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore.Shared;
public partial class SyncAwaiter
{
    public System.Collections.Generic.Queue<System.Action> _methodExecutionQueue;
    public System.Collections.Concurrent.ConcurrentDictionary<ExileCore.Shared.SyncAwaiter, System.Boolean> _childAwaiters;
    public void OnCompleted(System.Action completion)
    {
        throw new global::System.NotImplementedException();
    }

    public System.IDisposable RedirectExecutionQueue(ExileCore.Shared.SyncAwaiter target)
    {
        throw new global::System.NotImplementedException();
    }

    public void EnqueueItem(System.Action item)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Boolean PumpEvents()
    {
        throw new global::System.NotImplementedException();
    }
}