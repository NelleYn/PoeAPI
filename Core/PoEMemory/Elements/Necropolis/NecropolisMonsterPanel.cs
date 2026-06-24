using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.Elements.Necropolis;
public class NecropolisMonsterPanel : Element
{
    public List<NecropolisMonsterPanelMonsterAssociation> Associations
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<ModsDat.ModRecord> Mods
    {
        get
        {
            //IL_0004: Unknown result type (might be due to invalid IL or missing references)
            _ = this + (long)this;
            _ = 16;
            return (List<ModsDat.ModRecord>)(object)this;
        }
    }

    public List<(NecropolisMonsterPanelMonsterAssociation Association, ModsDat.ModRecord Mod)> AssociationsWithMods
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }
}