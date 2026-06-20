namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the in-game map, exposing both the large map and the minimap.
/// </summary>
public class Map : Element
{
    private Element _largeMap;
    private Element _smallMap;

    /// <summary>
    /// Gets the large map element.
    /// </summary>
    public Element LargeMap => _largeMap ?? (_largeMap = ReadObjectAt<Element>(0x230));

    /// <summary>
    /// Gets the horizontal shift of the large map.
    /// </summary>
    public float LargeMapShiftX => M.Read<float>(LargeMap.Address + 0x1C0);

    /// <summary>
    /// Gets the vertical shift of the large map.
    /// </summary>
    public float LargeMapShiftY => M.Read<float>(LargeMap.Address + 0x1C4);

    /// <summary>
    /// Gets the zoom level of the large map.
    /// </summary>
    public float LargeMapZoom => M.Read<float>(LargeMap.Address + 0x204);

    /// <summary>
    /// Gets the minimap element.
    /// </summary>
    public Element SmallMiniMap => _smallMap ?? (_smallMap = ReadObjectAt<Element>(0x238));

    /// <summary>
    /// Gets the horizontal shift of the minimap.
    /// </summary>
    public float SmallMinMapX => M.Read<float>(SmallMiniMap.Address + 0x1C0);

    /// <summary>
    /// Gets the vertical shift of the minimap.
    /// </summary>
    public float SmallMinMapY => M.Read<float>(SmallMiniMap.Address + 0x1C4);

    /// <summary>
    /// Gets the zoom level of the minimap.
    /// </summary>
    public float SmallMinMapZoom => M.Read<float>(SmallMiniMap.Address + 0x204);

    /// <summary>
    /// Gets the element holding the orange map text overlay.
    /// </summary>
    public Element OrangeWords => ReadObjectAt<Element>(0x250);

    /// <summary>
    /// Gets the element holding the blue map text overlay.
    /// </summary>
    public Element BlueWords => ReadObjectAt<Element>(0x2A8);
}
