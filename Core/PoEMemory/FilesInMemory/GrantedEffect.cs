using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory;
public class GrantedEffect : RemoteMemoryObject
{
    private string _name;
    private bool? _isSupport;
    private string _supportGemLetter;
    private bool? _supportsGemsOnly;
    private bool? _cannotBeSupported;
    private int? _castTime;
    private List<GrantedEffectPerLevel> _perLevelEffects;
    public string Name
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public bool IsSupport
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public string SupportGemLetter
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public bool SupportsGemsOnly
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public bool CannotBeSupported
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int CastTime
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<GrantedEffectPerLevel> PerLevelEffects
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}