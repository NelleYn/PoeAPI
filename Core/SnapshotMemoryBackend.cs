using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ExileCore;
internal class SnapshotMemoryBackend : IMemoryBackend, IDisposable
{
    private SortedList<long, byte[]> _data;
    private readonly Task<SortedList<long, byte[]>> _completionTask;
    public DateTime CreationTime
    {
        [CompilerGenerated]
        get
        {
            return (DateTime)this;
        }
    }

    public long Size
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I8, but got O
            return (long)this;
        }

        [CompilerGenerated]
        private set
        {
        }
    }

    public IReadOnlyCollection<KeyValuePair<long, byte[]>> Data => (IReadOnlyCollection<KeyValuePair<long, byte[]>>)this;

    public bool IsCompleted
    {
        [CompilerGenerated]
        get
        {
            //IL_0002: Expected I4, but got O
            return (byte)(int)this != 0;
        }

        [CompilerGenerated]
        private set
        {
        }
    }

    public SnapshotMemoryBackend(Process process, bool freezeProcess)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public void Dispose()
    {
    }

    public bool TryReadMemory(nint address, Span<byte> target)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public void NotifyFrame()
    {
    }
}