using System;
using System.Threading.Tasks;

namespace ExileCore
{
    /// <summary>
    /// Holds host-supplied overlay callbacks and invokes them at the matching points of the
    /// render loop. Ported from the ExileApi-Compiled API surface (see the reconstruction
    /// branch, PR #18); upstream's constructor body is not recoverable from the protected DLL,
    /// so the delegates are supplied through this constructor.
    /// </summary>
    public class ActionOverlay
    {
        public ActionOverlay(Action renderAction = null, Action postFrameAction = null, Func<Task> postInitializedAction = null)
        {
            RenderAction = renderAction;
            PostFrameAction = postFrameAction;
            PostInitializedAction = postInitializedAction;
        }

        public Action RenderAction { get; }
        public Action PostFrameAction { get; }
        public Func<Task> PostInitializedAction { get; }

        public void Render() => RenderAction?.Invoke();
        public void PostFrame() => PostFrameAction?.Invoke();
        public Task PostInitialized() => PostInitializedAction?.Invoke() ?? Task.CompletedTask;
    }
}
