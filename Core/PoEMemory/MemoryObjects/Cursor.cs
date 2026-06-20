using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;

namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// UI element wrapping the in-game cursor, exposing its current action and click state.
/// </summary>
public class Cursor : Element
{
    private readonly CachedValue<CursorOffsets> _cachevalue;

    /// <summary>Initializes a new instance of the <see cref="Cursor"/> class.</summary>
    public Cursor()
    {
        _cachevalue = new FrameCache<CursorOffsets>(() => M.Read<CursorOffsets>(Address));
    }

    /// <summary>Gets the current mouse action read directly from memory.</summary>
    public MouseActionType Action => (MouseActionType) M.Read<int>(Address + 0x238);

    /// <summary>Gets the current mouse action from the per-frame cache.</summary>
    public MouseActionType ActionCached => (MouseActionType) _cachevalue.Value.Action;

    /// <summary>Gets the click count from the per-frame cache.</summary>
    public int ClicksCached => _cachevalue.Value.Clicks;

    /// <summary>Gets the click count read directly from memory.</summary>
    public int Clicks => M.Read<int>(Address + 0x24C);

    /// <summary>Gets the action string read directly from memory.</summary>
    public string ActionString => M.ReadNativeString(Address + 0x2A0);

    /// <summary>Gets the action string from the per-frame cache.</summary>
    public string ActionStringCached => _cachevalue.Value.ActionString.ToString(M);
}
