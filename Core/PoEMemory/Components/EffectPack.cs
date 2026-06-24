using System.Collections.Generic;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;
public class EffectPack : Component
{
    public class Effect : RemoteMemoryObject
    {
        public string Name
        {
            get
            {
                //IL_0004: Unknown result type (might be due to invalid IL or missing references)
                _ = this + 8;
                return (string)(object)this;
            }
        }
    }

    private StdVector EffectVector => (StdVector)(this + 24);
    private List<long> EffectPtrList => (List<long>)(object)this;
    private List<Effect> Effects => (List<Effect>)(object)this;
}