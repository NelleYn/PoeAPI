using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.Shared.Enums;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.MemoryObjects;
public class ServerDataMinimapIcon : StructuredRemoteMemoryObject<ServerDataMinimapIconOffsets>
{
    public Vector2i GridPosition => (Vector2i)this;
    public uint EntityId => (uint)(this + 112);
    public Entity Entity => (Entity)(object)this;
    public Entity EntityDirect => (Entity)96;
    public MinimapIconDat MinimapIcon => (MinimapIconDat)(object)this;
    public MapIconsIndex MinimapIconIndex => (MapIconsIndex)(this + 16);

    public override string ToString()
    {
        _ = 11;
        _ = 4;
        return (string)(object)this;
    }
}