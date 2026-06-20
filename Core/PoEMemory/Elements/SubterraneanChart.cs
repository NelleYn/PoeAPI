namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the Subterranean Chart (Delve map), exposing its Delve grid element.
/// </summary>
public class SubterraneanChart : Element
{
    private DelveElement _grid;

    /// <summary>
    /// Gets the Delve grid element of the chart, or <c>null</c> when unavailable.
    /// </summary>
    public DelveElement GridElement =>
        Address != 0 ? _grid ?? (_grid = GetObject<DelveElement>(M.Read<long>(Address + 0x1C0, 0x690))) : null;
}
