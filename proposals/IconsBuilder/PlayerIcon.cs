// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;

namespace ExileCore.IconsBuilder;

/// <summary>Icon for other players in the instance.</summary>
public class PlayerIcon : BaseIcon
{
    /// <summary>Builds an "other player" icon labelled with the player name.</summary>
    public PlayerIcon(Entity entity, GameController gameController, IconsBuilderSettings settings) : base(entity, settings)
    {
        Show = () => entity.IsValid && !settings.HideOtherPlayers;
        if (_HasIngameIcon) return;
        MainTexture = new HudTexture("Icons.png") { UV = SpriteHelper.GetUV(MapIconsIndex.OtherPlayer) };
        Text = entity.GetComponent<Player>()?.PlayerName;
        Priority = IconPriority.VeryHigh;
    }
}
