namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the in-game world map, exposing its panel element.
/// </summary>
public class WorldMapElement : Element
{
    /// <summary>
    /// Gets the world map panel element.
    /// </summary>
    public Element Panel => GetObject<Element>(M.Read<long>(Address + 0xAB8, 0xC10));
}
