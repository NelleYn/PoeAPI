using System;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using GameOffsets;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing map item information such as the area, tier, and map series.
/// </summary>
public class Map : Component
{
    private readonly Lazy<MapComponentBase> mapBase;
    private readonly Lazy<MapComponentInner> mapInner;
    private readonly CachedValue<WorldArea> _area;

    /// <summary>Initializes a new instance of the <see cref="Map"/> class.</summary>
    public Map()
    {
        mapBase = new Lazy<MapComponentBase>(() => M.Read<MapComponentBase>(Address));
        mapInner = new Lazy<MapComponentInner>(() => M.Read<MapComponentInner>(mapBase.Value.Base));
        _area = new StaticValueCache<WorldArea>(() => TheGame.Files.WorldAreas.GetByAddress(MapInformation.Area));
    }

    /// <summary>Gets the inner map information struct.</summary>
    public MapComponentInner MapInformation => mapInner.Value;

    /// <summary>Gets the world area associated with this map.</summary>
    public WorldArea Area => _area.Value;

    /// <summary>Gets the map tier.</summary>
    public byte Tier => mapBase.Value.Tier;

    /// <summary>Gets the map series the map belongs to.</summary>
    public InventoryTabMapSeries MapSeries => (InventoryTabMapSeries) MapInformation.MapSeries;
}
