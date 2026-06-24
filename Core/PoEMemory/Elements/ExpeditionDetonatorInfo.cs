using ExileCore.Shared.Cache;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Elements;
public class ExpeditionDetonatorInfo : RemoteMemoryObject
{
    private readonly FrameCache<ExpeditionDetonatorInfoOffsets> _cache;
    public bool IsExplosivePlacementActive => (long)this != 0;
    public Vector2i[] PlacedExplosiveGridPositions => (Vector2i[])(object)this;
    public int TotalExplosiveCount => (int)this;

    public int PlacedExplosiveCount
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public int RemainingExplosiveCount => (object)this - (object)this;
    public Vector2i DetonatorGridPosition => (Vector2i)this;
    public Vector2i PlacementIndicatorGridPosition => (Vector2i)this;
}