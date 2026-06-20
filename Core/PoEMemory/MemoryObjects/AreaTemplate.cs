using System.Collections.Generic;

namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory area template record, exposing area metadata and possible corrupted variants.
/// </summary>
public class AreaTemplate : RemoteMemoryObject
{
    /// <summary>Gets the area's raw (internal) name.</summary>
    public string RawName => M.ReadStringU(M.Read<long>(Address));

    /// <summary>Gets the area's display name.</summary>
    public string Name => M.ReadStringU(M.Read<long>(Address + 8));

    /// <summary>Gets the act this area belongs to.</summary>
    public int Act => M.Read<int>(Address + 0x10);

    /// <summary>Gets a value indicating whether this area is a town.</summary>
    public bool IsTown => M.Read<byte>(Address + 0x14) == 1;

    /// <summary>Gets a value indicating whether this area has a waypoint.</summary>
    public bool HasWaypoint => M.Read<byte>(Address + 0x15) == 1;

    /// <summary>Gets the area's nominal level.</summary>
    public int NominalLevel => M.Read<int>(Address + 0x16); //Not sure

    /// <summary>Gets the monster level for this area.</summary>
    public int MonsterLevel => M.Read<int>(Address + 0x26);

    /// <summary>Gets the world area id for this template.</summary>
    public int WorldAreaId => M.Read<int>(Address + 0x2A);

    /// <summary>Gets the number of possible corrupted area variants.</summary>
    public int CorruptedAreasVariety => M.Read<int>(Address + 0xFB);

    /// <summary>Gets the possible corrupted areas this template can lead to.</summary>
    public List<WorldArea> PossibleCorruptedAreas => _PossibleCorruptedAreas(Address + 0x103, CorruptedAreasVariety);

    private List<WorldArea> _PossibleCorruptedAreas(long address, int count)
    {
        var list = new List<WorldArea>();

        for (var i = 0; i < count; i++)
        {
            list.Add(GetObject<WorldArea>(address + i * 8));
        }

        return list;
    }
}
