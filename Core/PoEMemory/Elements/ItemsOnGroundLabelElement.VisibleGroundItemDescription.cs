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
        public Element Label { get; init; }
        public Entity Entity { get; init; }
        public RectangleF ClientRect { get; init; }

        public VisibleGroundItemDescription(Element Label, Entity Entity, RectangleF ClientRect)
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