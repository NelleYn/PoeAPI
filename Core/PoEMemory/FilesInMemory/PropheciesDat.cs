using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the Prophecies.dat table, exposing each prophecy and a lookup by prophecy id.
/// </summary>
public class PropheciesDat : UniversalFileWrapper<ProphecyDat>
{
    private int IndexCounter;
    private bool loaded;
    private readonly Dictionary<int, ProphecyDat> ProphecyIndexDictionary = new Dictionary<int, ProphecyDat>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PropheciesDat"/> class.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public PropheciesDat(IMemory m, Func<long> address) : base(m, address)
    {
    }

    /// <summary>Gets all prophecies in the table.</summary>
    public IList<ProphecyDat> EntriesList => base.EntriesList.ToList();

    /// <summary>Gets the prophecy with the given prophecy id, or <c>null</c> if not found.</summary>
    /// <param name="index">The prophecy id.</param>
    public ProphecyDat GetProphecyById(int index)
    {
        CheckCache();

        if (!loaded)
        {
            foreach (var prophecyDat in EntriesList)
            {
                EntryAdded(prophecyDat.Address, prophecyDat);
            }

            loaded = true;
        }

        ProphecyIndexDictionary.TryGetValue(index, out var prophecy);
        return prophecy;
    }

    /// <summary>Indexes a prophecy by its prophecy id and assigns it a sequential index.</summary>
    /// <param name="addr">The record's memory address.</param>
    /// <param name="entry">The record that was added.</param>
    protected void EntryAdded(long addr, ProphecyDat entry)
    {
        entry.Index = IndexCounter++;
        ProphecyIndexDictionary.Add(entry.ProphecyId, entry);
    }

    /// <summary>Gets the prophecy located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public ProphecyDat GetByAddress(long address)
    {
        return base.GetByAddress(address);
    }
}
