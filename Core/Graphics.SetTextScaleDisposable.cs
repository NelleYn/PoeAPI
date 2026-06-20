// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.RenderQ;

namespace ExileCore;
partial class Graphics
{
    private record SetTextScaleDisposable : IDisposable
    {
        [CompilerGenerated]
        protected virtual Type EqualityContract
        {
            [CompilerGenerated]
            get
            {
                return (Type)typeof(SetTextScaleDisposable).TypeHandle;
            }
        }

        public ImGuiRender Render
        {
            [CompilerGenerated]
            get
            {
                return (ImGuiRender)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public float OldScale
        {
            [CompilerGenerated]
            get
            {
                //IL_0002: Expected F4, but got O
                return (float)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public SetTextScaleDisposable(ImGuiRender Render, float OldScale)
        {
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
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public override int GetHashCode()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        public virtual bool Equals(SetTextScaleDisposable? other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected SetTextScaleDisposable(SetTextScaleDisposable original)
        {
        }

        [CompilerGenerated]
        public void Deconstruct(out ImGuiRender Render, out float OldScale)
        {
            //IL_0006: Expected F4, but got O
            Render = (ImGuiRender)(object)this;
            OldScale = (float)this;
        }
    }
}