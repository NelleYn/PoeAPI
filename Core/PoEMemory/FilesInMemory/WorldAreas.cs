using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// Reads the WorldAreas.dat table, exposing each world area and an index keyed by area index.
/// </summary>
public class WorldAreas : UniversalFileWrapper<WorldArea>
{
    private int _indexCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldAreas"/> class.
    /// </summary>
    /// <param name="m">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public WorldAreas(IMemory m, Func<long> address) : base(m, address)
    {
    }

    /// <summary>World areas keyed by their sequential index.</summary>
    public Dictionary<int, WorldArea> AreasIndexDictionary { get; } = new Dictionary<int, WorldArea>();

    /// <summary>Gets the world area with the given index, or <c>null</c> if not found.</summary>
    /// <param name="index">The area's sequential index.</param>
    public WorldArea GetAreaByAreaId(int index)
    {
        CheckCache();

        AreasIndexDictionary.TryGetValue(index, out var area);
        return area;
    }

    /// <summary>Gets the first world area whose id matches <paramref name="id"/>.</summary>
    /// <param name="id">The area's string id.</param>
    public WorldArea GetAreaByAreaId(string id)
    {
        CheckCache();
        return AreasIndexDictionary.First(area => area.Value.Id == id).Value;
    }

    /// <inheritdoc />
    protected override void EntryAdded(long addr, WorldArea entry)
    {
        entry.Index = _indexCounter++;
        AreasIndexDictionary.Add(entry.Index, entry);
    }
}
