// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using System;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Catch-all icon for miscellaneous interactive world objects: area transitions, Abyss nodes,
/// Incursion portals, Delve sulphite/azurite controllers, etc. The visibility predicate inspects
/// <see cref="MinimapIcon"/>/<see cref="Transitionable"/>/<see cref="Targetable"/> state per path.
/// </summary>
public class MiscIcon : BaseIcon
{
    /// <summary>Builds a misc icon for the given entity.</summary>
    public MiscIcon(Entity entity, GameController gameController, IconsBuilderSettings settings) : base(entity, settings)
    {
        Update(entity, settings);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Entity.Path} : ({Entity.Type}) :{Text}";

    private void Update(Entity entity, IconsBuilderSettings settings)
    {
        if (entity == null) return;

        if (!_HasIngameIcon)
        {
            MainTexture = new HudTexture { FileName = "Icons.png", Size = settings.SizeMiscIcon };
        }
        else
        {
            MainTexture.Size = settings.SizeDefaultIcon;
            Text = RenderName;
            Priority = IconPriority.VeryHigh;
            if (entity.GetComponent<MinimapIcon>()?.Name.Equals("DelveRobot", StringComparison.Ordinal) ?? false) Text = "Follow Me";
            return;
        }

        if (entity.HasComponent<Targetable>())
        {
            if (entity.Path.Equals("Metadata/Terrain/Leagues/Synthesis/Objects/SynthesisPortal", StringComparison.Ordinal))
            {
                Show = () => entity.IsValid;
            }
            else
            {
                Show = () =>
                {
                    var isVisible = false;

                    if (entity.HasComponent<MinimapIcon>())
                    {
                        var minimapIcon = entity.GetComponent<MinimapIcon>();
                        isVisible = (minimapIcon?.IsVisible ?? false) && !minimapIcon.IsHide;
                    }

                    return entity.IsValid && isVisible && entity.IsTargetable;
                };
            }
        }
        else
        {
            Show = () => entity.IsValid && (entity.GetComponent<MinimapIcon>()?.IsVisible ?? false);
        }

        if (entity.HasComponent<Transitionable>() && entity.HasComponent<MinimapIcon>())
        {
            if (entity.Path.Equals("Metadata/MiscellaneousObjects/Abyss/AbyssCrackSpawners/AbyssCrackSkeletonSpawner"))
            {
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.AbyssCrack);
                Show = () => entity.IsValid && ((!(entity.GetComponent<MinimapIcon>()?.IsHide ?? true)) ||
                                                (entity.GetComponent<Transitionable>()?.Flag1 ?? 0) == 1);
            }
            else if (entity.Path.Equals("Metadata/MiscellaneousObjects/Abyss/AbyssStartNode"))
            {
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.Abyss);
                Show = () => entity.IsValid && ((!(entity.GetComponent<MinimapIcon>()?.IsHide ?? true)) ||
                                                (entity.GetComponent<Transitionable>()?.Flag1 ?? 0) == 1);
            }
            else if (entity.Path.Equals("Metadata/MiscellaneousObjects/Abyss/AbyssNodeSmall", StringComparison.Ordinal) ||
                     entity.Path.Equals("Metadata/MiscellaneousObjects/Abyss/AbyssNodeLarge", StringComparison.Ordinal) ||
                     entity.Path.StartsWith("Metadata/MiscellaneousObjects/Abyss/AbyssFinalNodeChest"))
            {
                Show = () => entity.IsValid && ((!(entity.GetComponent<MinimapIcon>()?.IsHide ?? true)) ||
                                                (entity.GetComponent<Transitionable>()?.Flag1 ?? 0) == 1);
            }
            else if (entity.Path.StartsWith("Metadata/Terrain/Leagues/Incursion/Objects/IncursionPortal", StringComparison.Ordinal))
            {
                Show = () => entity.IsValid && (entity.GetComponent<Transitionable>()?.Flag1 ?? 4) < 3;
            }
            else
            {
                Priority = IconPriority.Critical;
                Show = () => false;
            }
        }
        else if (entity.HasComponent<Targetable>())
        {
            if (entity.Path.Contains("Metadata/Terrain/Leagues/Delve/Objects/DelveMineral"))
            {
                Priority = IconPriority.High;
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.DelveMineralVein);
                Text = "Sulphite";
                Show = () => entity.IsValid && entity.IsTargetable;
            }
            else if (entity.Path.Contains("Metadata/Terrain/Leagues/Delve/Objects/EncounterControlObjects/AzuriteEncounterController"))
            {
                Priority = IconPriority.High;
                Text = "Start";
                Show = () => entity.IsValid && (entity.GetComponent<Transitionable>()?.Flag1 ?? 4) < 3;
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.PartyLeader);
            }
        }
    }
}
