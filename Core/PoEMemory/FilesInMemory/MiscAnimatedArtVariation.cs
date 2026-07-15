using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory;
public class MiscAnimatedArtVariation : RemoteMemoryObject
{
    public string Id => (string)1;

    public List<MiscAnimatedDat> Animated
    {
        get
        {
            //IL_0004: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 8;
            return (List<MiscAnimatedDat>)(object)this;
        }
    }

    public override string ToString()
    {
        return Id;
    }
}