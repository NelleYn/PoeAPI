using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.Elements;
public class AltarEntity : Entity
{
    public class AltarMod : RemoteMemoryObject
    {
        public uint SpecialId => (uint)(int)this;

        public List<uint> ModIdAndStatValues
        {
            get
            {
                //IL_0006: Unknown result type (might be due to invalid IL or missing references)
                //IL_000a: Expected O, but got I4
                _ = this + 16;
                return (List<uint>)40;
            }
        }

        public List<int> StatValues
        {
            get
            {
                while (true)
                {
                }

                return (List<int>)(object)this;
            }
        }

        public ModsDat.ModRecord Mod => (ModsDat.ModRecord)0;

        public override string ToString()
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private const uint SpecialId1 = 3184128996u;
    private const uint SpecialId2 = 627210281u;
    private const uint SpecialId3 = 1874799358u;
    private const uint SpecialId4 = 459538583u;
    private const uint SpecialId5 = 167828180u;
    private const uint SpecialId6 = 4029295898u;
    private const uint SpecialId7 = 2683957264u;
    private const uint SpecialId8 = 2368693987u;
    private const uint SpecialId9 = 2857552602u;
    private const uint SpecialId10 = 3840675357u;
    private const uint SpecialId11 = 1328127073u;
    private const uint SpecialId12 = 200507954u;
    public List<AltarMod> Mods
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_000a: Expected O, but got I4
            _ = this + 48;
            return (List<AltarMod>)88;
        }
    }

    public AltarMod TopDownside1
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod TopDownside2
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod TopDownside3
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<AltarMod> TopDownsides
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod TopUpside1
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod TopUpside2
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod TopUpside3
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<AltarMod> TopUpsides
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod BottomDownside1
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod BottomDownside2
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod BottomDownside3
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<AltarMod> BottomDownsides
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod BottomUpside1
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod BottomUpside2
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public AltarMod BottomUpside3
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<AltarMod> BottomUpsides
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}