// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExileCore.Shared;
partial class PluginManager
{
    private record LoadedAssembly
    {
        [CompilerGenerated]
        protected virtual Type EqualityContract
        {
            [CompilerGenerated]
            get
            {
                return (Type)typeof(LoadedAssembly).TypeHandle;
            }
        }

        public Assembly Assembly
        {
            [CompilerGenerated]
            get
            {
                return (Assembly)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public string PathOnDisk
        {
            [CompilerGenerated]
            get
            {
                return (string)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public PluginAssemblyLoadContext LoadContext
        {
            [CompilerGenerated]
            get
            {
                return (PluginAssemblyLoadContext)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public LoadedAssembly(Assembly Assembly, string PathOnDisk, PluginAssemblyLoadContext LoadContext)
        {
        }

        [CompilerGenerated]
        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            return true;
        }

        [CompilerGenerated]
        public override int GetHashCode()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public virtual bool Equals(LoadedAssembly? other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected LoadedAssembly(LoadedAssembly original)
        {
        }

        [CompilerGenerated]
        public void Deconstruct(out Assembly Assembly, out string PathOnDisk, out PluginAssemblyLoadContext LoadContext)
        {
            Assembly = (Assembly)(object)this;
            PathOnDisk = (string)(object)this;
            LoadContext = (PluginAssemblyLoadContext)(object)this;
        }
    }
}