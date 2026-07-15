using System.Collections.Generic;

namespace ExileCore.PoEMemory.FilesInMemory;
public class SkillArtVariation : RemoteMemoryObject
{
    public string Id => (string)1;

    public List<MiscAnimatedArtVariation> AnimatedArtVariations
    {
        get
        {
            //IL_0004: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 8;
            return (List<MiscAnimatedArtVariation>)(object)this;
        }
    }

    public List<string> Strings
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 72;
            _ = 8;
            return (List<string>)(object)this;
        }
    }

    public StatsDat.StatRecord Stat => (StatsDat.StatRecord)(this + (long)this);

    public override string ToString()
    {
        return Id;
    }
}