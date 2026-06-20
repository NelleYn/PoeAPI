// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Runtime.CompilerServices;

namespace ExileCore.RenderQ;
partial class ImGuiRender
{
    private record PopFont : IDisposable
    {
        [CompilerGenerated]
        protected virtual Type EqualityContract
        {
            [CompilerGenerated]
            get
            {
                return (Type)typeof(PopFont).TypeHandle;
            }
        }

        public void Dispose()
        {
        }

        [CompilerGenerated]
        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public override int GetHashCode()
        {
            //IL_0002: Expected I4, but got O
            return (int)this;
        }

        [CompilerGenerated]
        public virtual bool Equals(PopFont? other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected PopFont(PopFont original)
        {
        }
    }
}