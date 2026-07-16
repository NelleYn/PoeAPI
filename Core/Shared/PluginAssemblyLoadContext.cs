using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace ExileCore.Shared
{
    /// <summary>
    /// Collectible <see cref="AssemblyLoadContext"/> for plugin assemblies, following the
    /// canonical <see cref="AssemblyDependencyResolver"/> pattern: managed and unmanaged
    /// dependencies resolve against the plugin's own .deps.json, and the whole context can be
    /// unloaded once. Ported from the ExileApi-Compiled API surface (reconstruction branch,
    /// PR #18); upstream's constructor is not recoverable, so this one wires the standard
    /// pattern the recovered fields imply.
    /// </summary>
    public class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginAssemblyLocation;
        private readonly AssemblyDependencyResolver _resolver;
        private int _unloadState;

        public PluginAssemblyLoadContext(string pluginAssemblyLocation)
            : base(Path.GetFileNameWithoutExtension(pluginAssemblyLocation), isCollectible: true)
        {
            _pluginAssemblyLocation = pluginAssemblyLocation;
            _resolver = new AssemblyDependencyResolver(pluginAssemblyLocation);
            Resolving += ResolvingCallback;
            ResolvingUnmanagedDll += ResolvingUnmanagedDllCallback;
        }

        public string PluginAssemblyLocation => _pluginAssemblyLocation;

        public Assembly ResolvingCallback(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            try
            {
                var assemblyPath = _resolver?.ResolveAssemblyToPath(assemblyName);
                return assemblyPath != null ? context.LoadFromAssemblyPath(assemblyPath) : null;
            }
            catch
            {
                // A Resolving handler must not throw; returning null lets resolution fall through.
                return null;
            }
        }

        public nint ResolvingUnmanagedDllCallback(Assembly assembly, string dllName)
        {
            try
            {
                var libraryPath = _resolver?.ResolveUnmanagedDllToPath(dllName);
                return libraryPath != null ? NativeLibrary.Load(libraryPath) : IntPtr.Zero;
            }
            catch
            {
                // A ResolvingUnmanagedDll handler must not throw; IntPtr.Zero lets default probing continue.
                return IntPtr.Zero;
            }
        }

        public void UnloadOnce()
        {
            if (Interlocked.Exchange(ref _unloadState, 1) == 0)
            {
                Unload();
            }
        }
    }
}
