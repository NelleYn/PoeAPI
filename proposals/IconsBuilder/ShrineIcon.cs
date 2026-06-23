// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;

namespace ExileCore.IconsBuilder;

/// <summary>Icon for shrines; only shown while the shrine is still available.</summary>
public class ShrineIcon : BaseIcon
{
    /// <summary>Builds a shrine icon labelled with the shrine name.</summary>
    public ShrineIcon(Entity entity, GameController gameController, IconsBuilderSettings settings) : base(entity, settings)
    {
        MainTexture = new HudTexture("Icons.png");
        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.Shrine);
        Text = entity.GetComponent<Render>()?.Name;
        MainTexture.Size = settings.SizeShrineIcon;
        Show = () => entity.IsValid && (entity.GetComponent<Shrine>()?.IsAvailable ?? false);
    }
}
