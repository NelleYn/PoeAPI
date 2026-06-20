using System;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the BestiaryCapturableMonsters.dat table, assigning each entry a sequential id.
/// </summary>
public class BestiaryCapturableMonsters : UniversalFileWrapper<BestiaryCapturableMonster>
{
    private int IdCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="BestiaryCapturableMonsters"/> class.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public BestiaryCapturableMonsters(IMemory m, Func<long> address) : base(m, address)
    {
    }

    /// <summary>Assigns a sequential id to a capturable monster as it is added to the cache.</summary>
    /// <param name="addr">The record's memory address.</param>
    /// <param name="entry">The record that was added.</param>
    protected void EntryAdded(long addr, BestiaryCapturableMonster entry)
    {
        entry.Id = IdCounter++;
    }
}
