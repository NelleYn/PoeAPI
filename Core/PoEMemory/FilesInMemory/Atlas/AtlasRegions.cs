using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory.Atlas;

/// <summary>
/// Reads the AtlasRegions.dat table, exposing each region and an index keyed by region index.
/// </summary>
public class AtlasRegions : UniversalFileWrapper<AtlasRegion>
{
    /// <summary>The running counter used to assign sequential region indexes.</summary>
    public int IndexCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtlasRegions"/> class.
    /// </summary>
    /// <param name="mem">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public AtlasRegions(IMemory mem, Func<long> address) : base(mem, address)
    {
    }

    /// <summary>Atlas regions keyed by their sequential index.</summary>
    public Dictionary<int, AtlasRegion> RegionIndexDictionary { get; } = new Dictionary<int, AtlasRegion>();

    /// <inheritdoc />
    protected override void EntryAdded(long addr, AtlasRegion entry)
    {
        entry.Index = IndexCounter++;
        RegionIndexDictionary.Add(entry.Index, entry);
    }
}
