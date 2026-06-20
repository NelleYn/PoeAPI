using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the MonsterVarieties.dat table, exposing each monster variety and a lookup by metadata path.
/// </summary>
public class MonsterVarieties : UniversalFileWrapper<MonsterVariety>
{
    private readonly Dictionary<string, MonsterVariety> MonsterVarietyMetadataDictionary = new Dictionary<string, MonsterVariety>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MonsterVarieties"/> class.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public MonsterVarieties(IMemory m, Func<long> address) : base(m, address)
    {
    }

    /// <summary>Gets all monster varieties in the table.</summary>
    public IList<MonsterVariety> EntriesList => base.EntriesList.ToList();

    /// <summary>Resolves a monster variety from its metadata path, or <c>null</c> if unknown.</summary>
    /// <param name="path">The monster variety's metadata path.</param>
    public MonsterVariety TranslateFromMetadata(string path)
    {
        CheckCache();
        MonsterVarietyMetadataDictionary.TryGetValue(path, out var result);
        return result;
    }

    /// <summary>Indexes a monster variety by its metadata path as it is added to the cache.</summary>
    /// <param name="addr">The record's memory address.</param>
    /// <param name="entry">The record that was added.</param>
    protected void EntryAdded(long addr, MonsterVariety entry)
    {
        MonsterVarietyMetadataDictionary.Add(entry.VarietyId, entry);
    }

    /// <summary>Gets the monster variety located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public MonsterVariety GetByAddress(long address)
    {
        return base.GetByAddress(address);
    }
}
