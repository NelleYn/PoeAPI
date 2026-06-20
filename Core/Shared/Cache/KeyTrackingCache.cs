using System;

namespace ExileCore.Shared.Cache;
public static class KeyTrackingCache
{
    public static KeyTrackingCache<T, TKey> Create<T, TKey>(Func<T> func, Func<TKey> keyFunc)
    {
        return new KeyTrackingCache<T, TKey>(func, keyFunc);
    }
}