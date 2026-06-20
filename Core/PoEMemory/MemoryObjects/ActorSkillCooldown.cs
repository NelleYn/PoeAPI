using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.MemoryObjects;
public class ActorSkillCooldown : RemoteMemoryObject
{
    private readonly CachedValue<ActorSkillCooldownOffsets> _cache;
    public ushort Id => (ushort)(int)this;
    public int SkillSubId => (int)this;
    private StdVector CooldownUses => (StdVector)this;
    public int MaxUses => (int)this;

    public List<SkillCooldown> SkillCooldowns
    {
        get
        {
            _ = 16;
            return null;
        }
    }

    public override string ToString()
    {
        _ = 3;
        _ = 4;
        return "X";
    }
}