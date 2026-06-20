using System.Collections.Generic;

namespace ExileCore.PoEMemory.Components;
public class AttachedAnimatedObject : Component
{
    public List<AttachedAnimatedObjectAttachment> Attachments => (List<AttachedAnimatedObjectAttachment>)(this + 32);
}