using System;
using System.Runtime.CompilerServices;

namespace ExileCore;
internal class FuncAttachment : IDisposable
{
    private readonly Action<Action> _detachAction;
    private Action _attachedAction;
    public FuncAttachment(Action action, Action<Action> detachAction = null)
    {
    }

    private void Act()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public void Dispose()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public unsafe static FuncAttachment Attach(ref Action baseAction, Action attachedAction, Action<Action> detachAction = null)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}