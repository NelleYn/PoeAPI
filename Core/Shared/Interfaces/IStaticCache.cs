using System;

namespace ExileCore.Shared.Interfaces;

/// <summary>
/// A keyed cache of values read from process memory, tracking hit/miss statistics.
/// </summary>
/// <typeparam name="T">The type of the cached values.</typeparam>
public interface IStaticCache<T>
{
    /// <summary>Gets the number of entries currently stored in the cache.</summary>
    int Count { get; }

    /// <summary>Gets the number of entries that have been evicted from the cache.</summary>
    int DeletedCache { get; }

    /// <summary>Gets the number of reads served from the cache (cache hits).</summary>
    int ReadCache { get; }

    /// <summary>Gets the number of reads that fell through to process memory (cache misses).</summary>
    int ReadMemory { get; }

    /// <summary>Gets the cache hit ratio formatted as a string.</summary>
    string CoeffString { get; }

    /// <summary>Gets the cache hit ratio.</summary>
    float Coeff { get; }

    /// <summary>
    /// Returns the cached value for <paramref name="addr"/>, invoking <paramref name="func"/> to produce and cache it on a miss.
    /// </summary>
    /// <param name="addr">The cache key (typically an address as a string).</param>
    /// <param name="func">Factory invoked to compute the value when it is not already cached.</param>
    T Read(string addr, Func<T> func);

    /// <summary>Performs cache maintenance, evicting stale entries.</summary>
    void UpdateCache();

    /// <summary>Removes the entry with the specified key.</summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><c>true</c> if an entry was removed; otherwise, <c>false</c>.</returns>
    bool Remove(string key);
}
