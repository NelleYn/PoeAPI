using System.Collections.Generic;
using ExileCore.Shared.Cache;
using GameOffsets;

namespace ExileCore.PoEMemory.Elements.Sanctum;
public class SanctumFloorWindow : Element
{
    private readonly CachedValue<SanctumFloorWindowOffsets> _cachedValue;
    public List<List<SanctumRoomElement>> RoomsByLayer
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<SanctumRoomElement> Rooms
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    private SanctumFloorWindowDataSelector DataSelector
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public SanctumFloorData FloorData => (SanctumFloorData)(object)this;
}