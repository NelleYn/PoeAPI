using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class EffectEnvironment : StructuredRemoteMemoryObject<EnvironmentOffsets>
{
    public ushort Key => (ushort)(int)this;
    public ushort Value0 => (ushort)(int)this;
    public float Value1 => (float)this;
}