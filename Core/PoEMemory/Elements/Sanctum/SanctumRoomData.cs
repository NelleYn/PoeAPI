using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory.Sanctum;

namespace ExileCore.PoEMemory.Elements.Sanctum;
public class SanctumRoomData : RemoteMemoryObject
{
    public SanctumRoom FightRoom => (SanctumRoom)(object)this;
    public SanctumRoom RewardRoom => (SanctumRoom)(this + 16);
    public SanctumPersistentEffect RoomEffect => (SanctumPersistentEffect)(this + 32);
    public SanctumDeferredRewardCategory Reward1 => (SanctumDeferredRewardCategory)(this + 56);
    public SanctumDeferredRewardCategory Reward2 => (SanctumDeferredRewardCategory)(this + 72);
    public SanctumDeferredRewardCategory Reward3 => (SanctumDeferredRewardCategory)(this + 88);

    public List<SanctumDeferredRewardCategory> Rewards
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}