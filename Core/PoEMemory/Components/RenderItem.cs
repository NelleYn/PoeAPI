namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the resource path of a rendered item.
/// </summary>
public class RenderItem : Component
{
    /// <summary>Gets the resource path of the rendered item (cached).</summary>
    public string ResourcePath =>
        Cache.StringCache.Read($"{nameof(RenderItem)}{Address + 0x20}", () => M.ReadStringU(M.Read<long>(Address + 0x20)));
}
