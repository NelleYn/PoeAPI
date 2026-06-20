using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.Components;
public class UltimatumTrial : Component
{
    public WordEntry SacrificedItemWord => (WordEntry)(this + 72);
    public BaseItemType SacrificedItemType => (BaseItemType)(this + 72);
    public WordEntry RewardItemWord => (WordEntry)(this + 96);
    public UltimatumItemisedReward Reward => (UltimatumItemisedReward)(this + 40);
}