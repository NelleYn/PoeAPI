using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;
public class GameConfigKeyValue : StructuredRemoteMemoryObject<GameConfigKeyValueOffsets>
{
    public string Key => (string)(object)this;
    public string Value => (string)(object)this;
}