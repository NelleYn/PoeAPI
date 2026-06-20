using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using GameOffsets;
using GameOffsets.Native;

namespace ExileCore.PoEMemory.Components;

/// <summary>
/// Component exposing the visibility state and name of an entity's minimap icon.
/// </summary>
public class MinimapIcon : Component
{
    private FrameCache<MinimapIconOffsets> cachedValue;
    private MinimapIconOffsets MinimapIconOffsets => cachedValue.Value;

    /// <summary>Gets a value indicating whether the minimap icon is visible.</summary>
    public bool IsVisible => MinimapIconOffsets.IsVisible == 0; //M.Read<byte>(Address + 0x30) == 0;

    /// <summary>Gets a value indicating whether the minimap icon is hidden.</summary>
    public bool IsHide => MinimapIconOffsets.IsHide == 1; //M.Read<byte>(Address + 0x34) == 1;

    private NativeStringU Test => M.Read<NativeStringU>(MinimapIconOffsets.NamePtr); //M.Read<NativeStringU>(Address + 0x28, 0);

    /// <summary>Gets the raw minimap icon name string read from memory.</summary>
    public string TestString => Test.ToString(M);

    /// <summary>Gets the cached minimap icon name.</summary>
    public string Name => Cache.StringCache.Read($"{MinimapIconOffsets.NamePtr}{Address}", () => TestString);

    /// <inheritdoc />
    protected override void OnAddressChange()
    {
        cachedValue = new FrameCache<MinimapIconOffsets>(() => M.Read<MinimapIconOffsets>(Address));
    }
}
