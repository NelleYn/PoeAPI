namespace ExileCore.PoEMemory.MemoryObjects;
public class SentinelData : RemoteMemoryObject
{
    public SentinelState State => (SentinelState)(this + 24);
}