// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore.Shared;
public partial class PluginAssemblyLoadContext
{
    public System.String _pluginAssemblyLocation;
    public System.Boolean _loadFromStream;
    public System.Runtime.Loader.AssemblyDependencyResolver _resolver;
    public System.Int32 _unloadState;
    public System.Reflection.Assembly ResolvingCallback(System.Runtime.Loader.AssemblyLoadContext context, System.Reflection.AssemblyName assemblyName)
    {
        throw new global::System.NotImplementedException();
    }

    public nint ResolvingUnmanagedDllCallback(System.Reflection.Assembly assembly, System.String dllName)
    {
        throw new global::System.NotImplementedException();
    }

    public void UnloadOnce()
    {
        throw new global::System.NotImplementedException();
    }
}