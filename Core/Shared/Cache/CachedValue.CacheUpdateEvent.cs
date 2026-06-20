// Partial extension that restores a nested type missing from the modernized source.
namespace ExileCore.Shared.Cache;
partial class CachedValue<T>
{
    public delegate void CacheUpdateEvent(T t);
}