// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore;
public partial class ControllerInput
{
    public ExileCore.NamedPipeHandler _handler;
    public static System.Lazy<ExileCore.ControllerInput> _instance;
    public static ExileCore.ControllerInput Instance
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public static System.Boolean SendInputCommand(ExileCore.NamedPipeHandler.InputCommand inputCommand)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean SendControllerKeyDown(ExileCore.Shared.Nodes.HotkeyNodeV2.ControllerKey controllerKey)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean SendControllerKeyUp(ExileCore.Shared.Nodes.HotkeyNodeV2.ControllerKey controllerKey)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean SendControllerKeyPress(ExileCore.Shared.Nodes.HotkeyNodeV2.ControllerKey controllerKey, System.Int32 msDelay)
    {
        throw new global::System.NotImplementedException();
    }

    public static void SetInputCommandValue(ExileCore.NamedPipeHandler.InputCommand command, ExileCore.Shared.Nodes.HotkeyNodeV2.ControllerKey controllerKey, System.Boolean isDown)
    {
        throw new global::System.NotImplementedException();
    }

    public void Dispose()
    {
        throw new global::System.NotImplementedException();
    }
}