// EXPERIMENTAL candidate ported from exApiTools/IconsBuilder — see proposals/IconsBuilder/README.md. Not part of the build.

using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Abstract;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;

namespace ExileCore.IconsBuilder;

/// <summary>
/// Icon for Delirium-league monsters: the Delirium-fog "doodad daemon" spawners get their own fixed
/// sprites, everything else falls back to the same rarity/mod-icon logic as <see cref="MonsterIcon"/>.
/// </summary>
/// <remarks>
/// The original library dispatches to this icon from <c>entity.League == LeagueType.Delirium</c>.
/// This fork's <see cref="LeagueType"/> has no <c>Delirium</c> member (see README), so the dispatch
/// site (<see cref="IconsBuilder.EntityAddedLogic"/>) instead recognises Delirium monsters via the
/// <see cref="GameStat.AffectedByDelirium"/> stat and the doodad-daemon path prefix used below — both
/// are read the same way <see cref="LegionIcon"/> already reads <see cref="GameStat.MonsterMinimapIcon"/>,
/// so no new memory offset or enum member is required.
/// <para>
/// One deliberate deviation from the original: the original <c>DeliriumIcon</c> never wired the
/// <c>ShowWhiteMonsterName</c>/<c>ShowMagicMonsterName</c>/<c>ShowRareMonsterName</c>/<c>ShowUniqueMonsterName</c>
/// toggles into its rarity switch (unlike its own <c>MonsterIcon</c> sibling), even though it reads the
/// same settings type. Because Delirium-affected monsters are now diverted away from
/// <see cref="MonsterIcon"/> to this class, that omission would have silently dropped name labels for
/// them. This port adds the same toggle checks <see cref="MonsterIcon"/> already has, so a
/// Delirium-affected monster behaves the same as a non-Delirium one with respect to name display.
/// </para>
/// </remarks>
public class DeliriumIcon : BaseIcon
{
    /// <summary>Builds a Delirium icon for the given entity.</summary>
    public DeliriumIcon(Entity entity, GameController gameController, IconsBuilderSettings settings, Dictionary<string, Size2> modIcons)
        : base(entity, settings)
    {
        Update(entity, settings, modIcons);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Entity.Path} : {Entity.Type} ({Entity.Address}) T: {Text}";

    private void Update(Entity entity, IconsBuilderSettings settings, Dictionary<string, Size2> modIcons)
    {
        Show = () => entity.IsAlive;

        if (!_HasIngameIcon) MainTexture = new HudTexture("Icons.png");

        switch (Rarity)
        {
            case MonsterRarity.White:
                MainTexture.Size = settings.SizeEntityWhiteIcon;
                break;
            case MonsterRarity.Magic:
                MainTexture.Size = settings.SizeEntityMagicIcon;
                break;
            case MonsterRarity.Rare:
                MainTexture.Size = settings.SizeEntityRareIcon;
                break;
            case MonsterRarity.Unique:
            default:
                // Unknown/out-of-range rarity reads fall back to unique sizing rather than throwing,
                // matching the graceful default used in MonsterIcon/LegionIcon. (Original threw here.)
                MainTexture.Size = settings.SizeEntityUniqueIcon;
                Text = entity.RenderName;
                break;
        }

        if (_HasIngameIcon && entity.HasComponent<MinimapIcon>())
        {
            var minimapName = entity.GetComponent<MinimapIcon>()?.Name;
            if (!string.IsNullOrEmpty(minimapName) && !minimapName.Equals("NPC", StringComparison.Ordinal))
                return;
        }

        if (entity.Path.StartsWith("Metadata/Monsters/LeagueAffliction/DoodadDaemons", StringComparison.Ordinal))
        {
            const string pathPrefix = "Metadata/Monsters/LeagueAffliction/DoodadDaemons/DoodadDaemon";

            // Proximity-spawning volatile monster -> warn the player away from it.
            if (entity.Path.StartsWith(pathPrefix + "BloodBag", StringComparison.Ordinal))
            {
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.RedFlag);
                Text = settings.DeliriumText.Value ? "Avoid" : "";
            }
            else if (entity.Path.StartsWith(pathPrefix + "EggFodder", StringComparison.Ordinal))
            {
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.NPC);
                // Clear any RenderName the rarity switch above may have assigned (Unique/default
                // branch) — these spawner icons never show a text label.
                Text = "";
            }
            else if (entity.Path.StartsWith(pathPrefix + "GlobSpawn", StringComparison.Ordinal))
            {
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.MyPlayer);
                Text = "";
            }
            else
            {
                Show = () => false;
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.QuestObject);
                return;
            }

            MainTexture.Size = settings.SizeEntityProximityMonsterIcon;
            Hidden = () => false;
            Priority = IconPriority.Medium;
            return;
        }

        if (!entity.IsHostile)
        {
            if (!_HasIngameIcon)
            {
                MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterSmallGreenCircle);
                Priority = IconPriority.Low;
                Show = () => !settings.HideMinions && entity.IsAlive;
            }
        }
        else if (Rarity == MonsterRarity.Unique && entity.Path.Contains("Metadata/Monsters/Spirit/"))
        {
            MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeGreenHexagon);
        }
        else
        {
            string modName = null;

            if (entity.HasComponent<ObjectMagicProperties>())
            {
                var objectMagicProperties = entity.GetComponent<ObjectMagicProperties>();
                var mods = objectMagicProperties?.Mods;

                if (mods != null)
                {
                    if (mods.Contains("MonsterConvertsOnDeath_")) Show = () => entity.IsAlive && entity.IsHostile;

                    modName = mods.FirstOrDefault(modIcons.ContainsKey);
                }
            }

            if (modName != null)
            {
                MainTexture = new HudTexture("sprites.png");
                MainTexture.UV = SpriteHelper.GetUV(modIcons[modName], new Size2F(7, 8));
                Priority = IconPriority.VeryHigh;
            }
            else
            {
                switch (Rarity)
                {
                    case MonsterRarity.White:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeRedCircle);
                        if (settings.ShowWhiteMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                    case MonsterRarity.Magic:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeBlueCircle);
                        if (settings.ShowMagicMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                    case MonsterRarity.Rare:
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowCircle);
                        if (settings.ShowRareMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                    case MonsterRarity.Unique:
                    default:
                        // Original used LootFilterLargeWhiteHexagon (absent in this fork); use the
                        // yellow hexagon tinted dark orange instead, matching MonsterIcon/LegionIcon.
                        // Unknown rarity also lands here rather than throwing.
                        MainTexture.UV = SpriteHelper.GetUV(MapIconsIndex.LootFilterLargeYellowHexagon);
                        MainTexture.Color = Color.DarkOrange;
                        if (settings.ShowUniqueMonsterName)
                            Text = RenderName?.Split(',').FirstOrDefault();
                        break;
                }
            }
        }
    }
}
