using System;
using ExileCore.Shared.Enums;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.MemoryObjects;
public class ServerPlayerData : StructuredRemoteMemoryObject<ServerPlayerDataOffsets>
{
    public CharacterClass Class => (CharacterClass)(this & 0xF);
    public int Level => (int)this;
    public int PassiveRefundPointsLeft => (int)this;
    public int QuestPassiveSkillPoints => (int)this;

    [Obsolete("Might be gone?")]
    public int FreePassiveSkillPointsLeft => (int)this;
    public int TotalAscendencyPoints => (int)this;
    public int SpentAscendencyPoints => (int)this;
    public NativePtrArray AllocatedPassivesIds => (NativePtrArray)this;
}