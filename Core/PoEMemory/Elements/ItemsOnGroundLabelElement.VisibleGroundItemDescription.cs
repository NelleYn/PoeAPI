// Partial extension that restores a nested type missing from the modernized source.
using System;
using System.Runtime.CompilerServices;
using System.Text;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;

namespace ExileCore.PoEMemory.Elements;
partial class ItemsOnGroundLabelElement
{
    public record VisibleGroundItemDescription
    {
        [CompilerGenerated]
        protected virtual Type EqualityContract
        {
            [CompilerGenerated]
            get
            {
                return (Type)typeof(VisibleGroundItemDescription).TypeHandle;
            }
        }

        public Element Label
        {
            [CompilerGenerated]
            get
            {
                return (Element)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public Entity Entity
        {
            [CompilerGenerated]
            get
            {
                return (Entity)(object)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public RectangleF ClientRect
        {
            [CompilerGenerated]
            get
            {
                return (RectangleF)this;
            }

            [CompilerGenerated]
            init
            {
            }
        }

        public VisibleGroundItemDescription(Element Label, Entity Entity, RectangleF ClientRect)
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
        public virtual bool Equals(VisibleGroundItemDescription? other)
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }

        [CompilerGenerated]
        protected VisibleGroundItemDescription(VisibleGroundItemDescription original)
        {
        }

        [CompilerGenerated]
        public void Deconstruct(out Element Label, out Entity Entity, out RectangleF ClientRect)
        {
            Label = (Element)(object)this;
            Entity = (Entity)(object)this;
            ClientRect = (RectangleF)this;
        }
    }
}