namespace ExileCore.PoEMemory.MemoryObjects;
public class SkillCooldown : RemoteMemoryObject
{
    public float Remaining => (float)this;
    public float TotalCooldown => this + 8;
}