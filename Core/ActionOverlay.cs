namespace ExileCore;
public partial class ActionOverlay
{
    public System.Action RenderAction { get; }

    public System.Action PostFrameAction { get; }

    public System.Func<System.Threading.Tasks.Task> PostInitializedAction { get; }

    public void Render()
    {
        RenderAction?.Invoke();
    }

    public System.Threading.Tasks.Task PostInitialized()
    {
        return PostInitializedAction?.Invoke() ?? System.Threading.Tasks.Task.CompletedTask;
    }

    public void PostFrame()
    {
        PostFrameAction?.Invoke();
    }
}
