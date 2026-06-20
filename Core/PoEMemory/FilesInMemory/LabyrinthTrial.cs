using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore.PoEMemory.FilesInMemory;

/// <summary>
/// A single labyrinth trial record, lazily resolving its associated world area.
/// </summary>
public class LabyrinthTrial : RemoteMemoryObject
{
    /// <summary>Cached world area backing <see cref="Area"/>.</summary>
    public WorldArea area;

    private int id = -1;

    /// <summary>The trial's id, read lazily and cached.</summary>
    public int Id => id != -1 ? id : id = M.Read<int>(Address + 0x10);

    /// <summary>The world area this trial belongs to, resolved lazily and cached.</summary>
    public WorldArea Area
    {
        get
        {
            if (area == null)
            {
                var areaPtr = M.Read<long>(Address + 0x8);
                area = TheGame.Files.WorldAreas.GetByAddress(areaPtr);
            }

            return area;
        }
    }
}
