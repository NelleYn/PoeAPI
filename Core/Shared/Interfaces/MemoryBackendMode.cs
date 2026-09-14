namespace ExileCore.Shared.Interfaces;

/// <summary>
/// Read strategy used by a memory backend.
/// </summary>
public enum MemoryBackendMode
{
    /// <summary>Every request reads process memory directly.</summary>
    AlwaysRead = 0,

    /// <summary>Reads are served from a preloaded cache of process memory.</summary>
    CacheAndPreload = 1
}
