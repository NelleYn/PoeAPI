using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory.FilesInMemory.Atlas;

/// <summary>
/// Reads the AtlasNode.dat table, exposing each atlas node.
/// </summary>
public class AtlasNodes : UniversalFileWrapper<AtlasNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AtlasNodes"/> class.
    /// </summary>
    /// <param name="mem">The memory accessor used to read the game process.</param>
    /// <param name="address">A delegate returning the table's base address.</param>
    public AtlasNodes(IMemory mem, Func<long> address) : base(mem, address)
    {
    }

    /// <summary>Gets a snapshot of all atlas nodes in the table.</summary>
    public IList<AtlasNode> EntriesList
    {
        get
        {
            CheckCache();
            return CachedEntriesList.ToList();
        }
    }

    /// <summary>Gets the atlas node located at the given memory address, or <c>null</c> if not found.</summary>
    /// <param name="address">The record's memory address.</param>
    public AtlasNode GetByAddress(long address)
    {
        return base.GetByAddress(address);
    }
}
