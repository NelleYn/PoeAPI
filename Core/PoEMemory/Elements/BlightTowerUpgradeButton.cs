using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Elements;
public class BlightTowerUpgradeButton : Element
{
    public BlightTowerDat UpgradeResult => (BlightTowerDat)(this + (long)this);
}