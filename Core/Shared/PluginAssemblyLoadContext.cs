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
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? context.LoadFromAssemblyPath(assemblyPath) : null;
    }

    public nint ResolvingUnmanagedDllCallback(System.Reflection.Assembly assembly, System.String dllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(dllName);
        return libraryPath != null ? System.Runtime.InteropServices.NativeLibrary.Load(libraryPath) : System.IntPtr.Zero;
    }

    public void UnloadOnce()
    {
        throw new global::System.NotImplementedException();
    }
}