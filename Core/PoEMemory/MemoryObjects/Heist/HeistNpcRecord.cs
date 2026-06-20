using System.Collections.Generic;
using ExileCore.PoEMemory.FilesInMemory;

namespace ExileCore.PoEMemory.MemoryObjects.Heist;
public class HeistNpcRecord : RemoteMemoryObject
{
    private long _JobCount
    {
        get
        {
            //IL_000a: Unknown result type (might be due to invalid IL or missing references)
            //IL_000c: Expected I8, but got Unknown
            _ = 0;
            _ = 10;
            return this + 32;
        }
    }

    public List<HeistJobRecord> Jobs => (List<HeistJobRecord>)(this + 40);

    public string PortraitFile
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 48;
            return (string)1;
        }
    }

    private int _StatCount
    {
        get
        {
            //IL_0005: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 56;
            _ = 0;
            return 32;
        }
    }

    public List<StatsDat.StatRecord> Stats
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            _ = this + 64;
            return (List<StatsDat.StatRecord>)(object)this;
        }
    }

    public string Name
    {
        get
        {
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0009: Expected O, but got I4
            _ = this + 108;
            return (string)1;
        }
    }

    private List<StatsDat.StatRecord> GetStats(long start, int count)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    private List<HeistJobRecord> GetJobs(long source)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public override string ToString()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }
}