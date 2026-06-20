using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the Tags.dat table, exposing each tag record indexed by its key.
/// </summary>
public class TagsDat : FileInMemory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagsDat"/> class and loads all tag records.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public TagsDat(IMemory m, Func<long> address) : base(m, address)
    {
        loadItems();
    }

    /// <summary>Tag records keyed by their (case-insensitive) key.</summary>
    public Dictionary<string, TagRecord> Records { get; } = new Dictionary<string, TagRecord>(StringComparer.OrdinalIgnoreCase);

    private void loadItems()
    {
        foreach (var addr in RecordAddresses())
        {
            var r = new TagRecord(M, addr);

            if (!Records.ContainsKey(r.Key))
                Records.Add(r.Key, r);
        }
    }

    /// <summary>A single tag parsed from the Tags.dat table.</summary>
    public class TagRecord
    {
        // more fields can be added (see in visualGGPK)

        /// <summary>
        /// Reads a single tag record from memory at the given address.
        /// </summary>
        /// <param name="m">The memory accessor used to read the game process.</param>
        /// <param name="addr">The record's memory address.</param>
        public TagRecord(IMemory m, long addr)
        {
            Key = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(TagsDat)}{addr + 0}",
                () => m.ReadStringU(m.Read<long>(addr + 0), 255));

            Hash = m.Read<int>(addr + 0x8);
        }

        /// <summary>The tag's unique key.</summary>
        public string Key { get; }

        /// <summary>The tag's hash value.</summary>
        public int Hash { get; }
    }
}
