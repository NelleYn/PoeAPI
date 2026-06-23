// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;

namespace ExileCore.IconsBuilder;

/// <summary>Icon for the local player.</summary>
public class SelfIcon : BaseIcon
{
    /// <summary>Builds the self icon using the dedicated MyPlayer sprite.</summary>
    public SelfIcon(Entity entity, GameController gameController, IconsBuilderSettings settings) : base(entity, settings)
    {
        Show = () => entity.IsValid && !settings.HideSelf;
        MainTexture = new HudTexture("Icons.png") { UV = SpriteHelper.GetUV(MapIconsIndex.MyPlayer) };
        MainTexture.Size = settings.SizeSelf;
    }
}
