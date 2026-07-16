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
        try
        {
            // When wired to the plugin ALC's Resolving event, `context` is that plugin ALC,
            // so the dependency is loaded into the plugin's own (collectible) context.
            var assemblyPath = _resolver?.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? context.LoadFromAssemblyPath(assemblyPath) : null;
        }
        catch
        {
            // A Resolving handler must not throw; returning null lets resolution fall through.
            return null;
        }
    }

    public nint ResolvingUnmanagedDllCallback(System.Reflection.Assembly assembly, System.String dllName)
    {
        try
        {
            var libraryPath = _resolver?.ResolveUnmanagedDllToPath(dllName);
            return libraryPath != null ? System.Runtime.InteropServices.NativeLibrary.Load(libraryPath) : System.IntPtr.Zero;
        }
        catch
        {
            // A ResolvingUnmanagedDll handler must not throw; IntPtr.Zero lets default probing continue.
            return System.IntPtr.Zero;
        }
    }

    public void UnloadOnce()
    {
        throw new global::System.NotImplementedException();
    }
}