// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Helpers;
using SharpDX;

namespace ExileCore.IconsBuilder;

/// <summary>Icon for mission markers (e.g. master/league mission targets).</summary>
public class MissionMarkerIcon : BaseIcon
{
    /// <summary>Builds a mission marker icon shown while the marker is active or targetable.</summary>
    public MissionMarkerIcon(Entity entity, GameController gameController, IconsBuilderSettings settings) : base(entity, settings)
    {
        MainTexture = new HudTexture();
        MainTexture.FileName = "Icons.png";
        MainTexture.UV = SpriteHelper.GetUV(16, new Size2F(14, 14));

        Show = () =>
        {
            var switchState = entity.GetComponent<Transitionable>() != null
                ? entity.GetComponent<Transitionable>().Flag1
                : (byte?)null;

            var isTargetable = entity.IsTargetable;
            return switchState == 1 || isTargetable;
        };

        MainTexture.Size = settings.SizeMiscIcon;
    }
}
