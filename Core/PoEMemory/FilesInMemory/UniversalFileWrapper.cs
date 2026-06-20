using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Generic reader for an in-memory .dat file table whose records map to
/// <see cref="RemoteMemoryObject"/> instances. Records are lazily read and cached on first access.
/// </summary>
/// <typeparam name="RecordType">The record type produced for each table entry.</typeparam>
public class UniversalFileWrapper<RecordType> : FileInMemory where RecordType : RemoteMemoryObject, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniversalFileWrapper{RecordType}"/> class.
    /// </summary>
    /// <param name="mem">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public UniversalFileWrapper(IMemory mem, Func<long> address) : base(mem, address)
    {
    }

    //We mark this fields as private coz we don't allow to read them directly dut to optimisation. Use EntriesList and methods instead.

    /// <summary>Maps each record address to its cached <typeparamref name="RecordType"/> instance.</summary>
    protected Dictionary<long, RecordType> EntriesAddressDictionary { get; set; } = new Dictionary<long, RecordType>();

    /// <summary>Backing list of cached records in table order.</summary>
    protected List<RecordType> CachedEntriesList { get; set; } = new List<RecordType>();

    /// <summary>Gets all records in the table, populating the cache on first access.</summary>
    public List<RecordType> EntriesList
    {
        get
        {
            CheckCache();
            return CachedEntriesList;
        }
    }

    /// <summary>Gets the record located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public RecordType GetByAddress(long address)
    {
        CheckCache();
        EntriesAddressDictionary.TryGetValue(address, out var result);
        return result;
    }

    /// <summary>Reads and caches all records if the cache has not been populated yet.</summary>
    public void CheckCache()
    {
        if (EntriesAddressDictionary.Count != 0)
            return;

        foreach (var addr in RecordAddresses())
        {
            if (!EntriesAddressDictionary.ContainsKey(addr))
            {
                var r = RemoteMemoryObject.pTheGame.GetObject<RecordType>(addr);
                EntriesAddressDictionary.Add(addr, r);
                EntriesList.Add(r);
                EntryAdded(addr, r);
            }
        }
    }

    /// <summary>Called for each record as it is added to the cache, allowing derived types to build extra indexes.</summary>
    /// <param name="addr">The record's memory address.</param>
    /// <param name="entry">The record that was added.</param>
    protected virtual void EntryAdded(long addr, RecordType entry)
    {
    }
}
