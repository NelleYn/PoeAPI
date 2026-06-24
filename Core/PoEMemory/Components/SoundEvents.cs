using System.Collections.Generic;

namespace ExileCore.PoEMemory.Components;
public class SoundEvents : Component
{
    public class SoundEvent : RemoteMemoryObject
    {
        public string Name
        {
            get
            {
                //IL_0005: Unknown result type (might be due to invalid IL or missing references)
                _ = this + 64;
                return (string)(object)this;
            }
        }

        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<SoundEvent> Events
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 88;
            _ = 32;
            return (List<SoundEvent>)(object)this;
        }
    }
}