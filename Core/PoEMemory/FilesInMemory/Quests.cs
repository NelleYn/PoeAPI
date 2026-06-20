using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the Quests.dat table, exposing each quest record.
/// </summary>
public class Quests : UniversalFileWrapper<Quest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Quests"/> class.
    /// </summary>
    /// <param name="game">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public Quests(IMemory game, Func<long> address) : base(game, address)
    {
    }

    /// <summary>Gets a snapshot of all quests in the table.</summary>
    public IList<Quest> EntriesList
    {
        get
        {
            CheckCache();
            return CachedEntriesList.ToList();
        }
    }

    /// <summary>Gets the quest located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public Quest GetByAddress(long address)
    {
        return base.GetByAddress(address);
    }
}
