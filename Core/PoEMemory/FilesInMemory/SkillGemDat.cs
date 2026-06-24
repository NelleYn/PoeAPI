using System.Collections.Generic;
using ExileCore.PoEMemory.Models;

namespace ExileCore.PoEMemory.FilesInMemory;
public class SkillGemDat : RemoteMemoryObject
{
    public BaseItemType ItemType
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<GemEffect> GemEffects
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int StrengthRequirementPercent => this + 16;
    public int DexterityRequirementPercent => this + 20;
    public int IntelligenceRequirementPercent => this + 24;
    public bool IsVaalGem => this + 44 > 0;
    public bool IsSupportGem => this + 61 > 0;
    public SkillGemDatSocketType SocketType => (SkillGemDatSocketType)(this + 83);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}