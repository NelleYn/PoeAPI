using System;
using ExileCore.Shared;

namespace ExileCore;
public class DefaultMemoryBackend : IMemoryBackend, IDisposable
{
    private static readonly DebugInformation PerFrameStats;
    private long _currentFrameUsedTime;
    private readonly nint _openProcessHandle;
    public DefaultMemoryBackend(nint processHandle)
    {
    }

    public bool TryReadMemory(nint address, Span<byte> target)
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public void NotifyFrame()
    {
        throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
    }

    public void Dispose()
    {
    }

    static DefaultMemoryBackend()
    {
        _ = 1;
    }
}