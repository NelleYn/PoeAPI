using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.MemoryObjects.Heist;
public class HeistJobRecord : RemoteMemoryObject
{
    public string Id => (string)1;

    public string Name
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            //IL_0008: Expected O, but got I4
            _ = this + 8;
            return (string)1;
        }
    }

    public string RequiredSkillIcon
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 16;
            return (string)1;
        }
    }

    public string SkillIcon
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 24;
            return (string)1;
        }
    }

    public string MapIcon
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 40;
            return (string)1;
        }
    }

    public StatsDat.StatRecord LevelStat => (StatsDat.StatRecord)(this + 48);
    public StatsDat.StatRecord AlertStat => (StatsDat.StatRecord)(this + 64);
    public StatsDat.StatRecord AlarmStat => (StatsDat.StatRecord)(this + 80);
    public StatsDat.StatRecord CostStat => (StatsDat.StatRecord)(this + 96);
    public StatsDat.StatRecord ExperienceGainStat => (StatsDat.StatRecord)(this + 112);

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}