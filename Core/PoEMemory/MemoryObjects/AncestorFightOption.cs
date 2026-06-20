using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory.Ancestor;

namespace ExileCore.PoEMemory.MemoryObjects;
public class AncestorFightOption : RemoteMemoryObject
{
    private AncestralTrialTribe _tribeToFight;
    private List<AncestorFightOptionReward> _rewards;
    public AncestralTrialTribe TribeToFight
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<AncestorFightOptionReward> Rewards
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}