// Partial extension that restores a nested type missing from the modernized source.
using System.Reflection;

namespace ExileCore.Shared;

partial class PluginManager
{
    /// <summary>A loaded plugin assembly together with its on-disk path and load context.</summary>
    private record LoadedAssembly(Assembly Assembly, string PathOnDisk, PluginAssemblyLoadContext LoadContext);
}
