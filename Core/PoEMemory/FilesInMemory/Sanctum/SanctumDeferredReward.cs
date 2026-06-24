namespace ExileCore.PoEMemory.FilesInMemory.Sanctum;
public class SanctumDeferredReward : RemoteMemoryObject
{
    private string _id;
    private int? _count;
    public string Id
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public SanctumPersistentEffectCategory DeferralCategory => (SanctumPersistentEffectCategory)(this + 16);
    public SanctumDeferredRewardCategory RewardCategory => (SanctumDeferredRewardCategory)(this + 32);

    public int Count
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}