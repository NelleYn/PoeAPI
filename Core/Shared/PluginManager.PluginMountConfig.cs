// Partial extension that restores a nested type missing from the modernized source.
using System.Runtime.CompilerServices;

namespace ExileCore.Shared;
partial class PluginManager
{
    private class PluginMountConfig
    {
        public string SourcePath { get; set; }
    }
}