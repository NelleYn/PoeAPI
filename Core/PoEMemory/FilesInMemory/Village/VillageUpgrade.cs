using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory.Village;
public class VillageUpgrade : RemoteMemoryObject
{
    private VillageUpgradeCategory _category;
    private string _description;
    private int? _tier;
    private int? _playerFlagId;
    private int? _requiredGold;
    private List<VillageResource> _requiredTypes;
    private List<int> _requiredResourceAmounts;
    public VillageUpgradeCategory Category
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int Tier
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string Description
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int PlayerFlagId
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int RequiredGold
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<VillageResource> RequiredResourceTypes
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<int> RequiredResourceAmounts
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<(VillageResource, int)> RequiredResources => (List<(VillageResource, int)>)(object)this;

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}