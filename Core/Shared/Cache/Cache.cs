using ExileCore.PoEMemory;
using ExileCore.Shared.Interfaces;

namespace ExileCore.Shared.Cache;

/// <summary>
/// Holds the set of static memory-read caches used throughout the API.
/// </summary>
public class Cache
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Cache"/> class and creates the underlying caches.
    /// </summary>
    public Cache()
    {
        CreateCache();
    }

    /// <summary>Cache for UI element memory objects.</summary>
    public IStaticCache<RemoteMemoryObject> StaticCacheElements { get; private set; }

    /// <summary>Cache for component memory objects.</summary>
    public IStaticCache<RemoteMemoryObject> StaticCacheComponents { get; private set; }

    /// <summary>Cache for entity memory objects.</summary>
    public IStaticCache<RemoteMemoryObject> StaticEntityCache { get; private set; }

    /// <summary>Cache for parsed entity-list memory objects.</summary>
    public IStaticCache<RemoteMemoryObject> StaticEntityListCache { get; private set; }

    /// <summary>Cache for parsed server-entity memory objects.</summary>
    public IStaticCache<RemoteMemoryObject> StaticServerEntityCache { get; private set; }

    /// <summary>Cache for strings read from memory.</summary>
    public IStaticCache<string> StringCache { get; private set; }

    /// <summary>
    /// Creates (or recreates) all of the underlying caches.
    /// </summary>
    public void CreateCache()
    {
        StaticCacheElements = new StaticCache<RemoteMemoryObject>(300, 60, "Elements");
        StaticCacheComponents = new StaticCache<RemoteMemoryObject>(90, 29, "Components");
        StaticEntityCache = new StaticCache<RemoteMemoryObject>(60, 30, "Entity");
        StaticEntityListCache = new StaticCache<RemoteMemoryObject>(60, 30, "Entities parse");
        StaticServerEntityCache = new StaticCache<RemoteMemoryObject>(90, 30, "Server entities parse");
        StringCache = new StaticCache<string>(300);
    }

    /// <summary>
    /// Attempts to trim/clear all of the underlying caches.
    /// </summary>
    public void TryClearCache()
    {
        StaticCacheElements.UpdateCache();
        StaticCacheComponents.UpdateCache();
        StaticEntityCache.UpdateCache();
        StaticEntityListCache.UpdateCache();
        StaticServerEntityCache.UpdateCache();
        StringCache.UpdateCache();
    }
}
