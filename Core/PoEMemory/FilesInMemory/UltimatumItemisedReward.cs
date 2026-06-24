using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.FilesInMemory;
public class UltimatumItemisedReward : RemoteMemoryObject
{
    public string Id => (string)1;
    public int Hash => this + 8;

    public string RewardText
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 12;
            return (string)1;
        }
    }

    public UltimatumItemisedRewardType RewardType => (UltimatumItemisedRewardType)(this + 36);
    public BaseItemType SacrificeItem => (BaseItemType)(this + 40);
    public int SacrificeAmount => this + 56;

    public string SacrificeText
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 60;
            return (string)1;
        }
    }
}