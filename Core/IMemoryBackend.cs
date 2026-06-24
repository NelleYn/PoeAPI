using System;

namespace ExileCore;
public interface IMemoryBackend : IDisposable
{
    bool TryReadMemory(nint address, Span<byte> target);
    void NotifyFrame();
}